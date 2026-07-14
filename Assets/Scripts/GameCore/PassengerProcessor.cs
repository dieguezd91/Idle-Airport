using System;
using System.Collections.Generic;
using IdleAirport.GameCore.Prestige;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerProcessor : MonoBehaviour, IPrestigeResettable
    {
        public enum PassengerProcessingType
        {
            Manual,
            Auto
        }

        public enum ManualScanFailureReason
        {
            None,
            MissingQueue,
            MissingWaitingRoom,
            NoPassengers,
            NoReservableCapacity,
            ReserveFailed,
            DequeueFailed,
            WaitingRoomReceiveFailed
        }

        public readonly struct PassengerProcessedData
        {
            public PassengerProcessedData(
                PassengerProcessingType processingType,
                double totalReward,
                double baseReward,
                double shopBonus)
            {
                ProcessingType = processingType;
                TotalReward = totalReward;
                BaseReward = baseReward;
                ShopBonus = shopBonus;
            }

            public PassengerProcessingType ProcessingType { get; }
            public double TotalReward { get; }
            public double BaseReward { get; }
            public double ShopBonus { get; }
        }

        public readonly struct PassengerProcessFailedFeedbackData
        {
            public PassengerProcessFailedFeedbackData(
                PassengerProcessingType processingType,
                ManualScanFailureReason manualFailureReason,
                Vector3 feedbackWorldPosition)
            {
                ProcessingType = processingType;
                ManualFailureReason = manualFailureReason;
                FeedbackWorldPosition = feedbackWorldPosition;
            }

            public PassengerProcessingType ProcessingType { get; }
            public ManualScanFailureReason ManualFailureReason { get; }
            public Vector3 FeedbackWorldPosition { get; }
        }

        private sealed class ManualScannerLane
        {
            public ManualScannerLane(ScannerStationUIController scanner)
            {
                Scanner = scanner;
            }

            public ScannerStationUIController Scanner { get; }
            public bool IsUnlocked { get; set; }
            public bool HasReadyPassenger => IsUnlocked && Scanner != null && Scanner.HasManualPassengerReady;
            public bool CanAcceptPassenger => IsUnlocked && Scanner != null && Scanner.CanAcceptPassenger;
        }

        public event Action<PassengerProcessedData> OnPassengerProcessed;
        public event Action<PassengerProcessFailedFeedbackData> OnPassengerProcessFailed;

        [Header("References")]
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private StoresManager _storesManager;
        [SerializeField] private PassengerQueueUIController _queue;
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private AITSAScannerUpgrade _aiTSAScannerUpgrade;
        [SerializeField] private ScannerStationUIController _manualScanner;
        [SerializeField] private List<ScannerStationUIController> _manualScanners = new();
        [SerializeField] private ScannerStationUIController _aiScanner;

        [Header("Settings")]
        [SerializeField] private int _manualClickValue = 1;
        [SerializeField] private bool _debugManualScanState;

        private readonly List<ManualScannerLane> _manualLanes = new();
        private bool _lastCanManualScan;
        private string _lastManualScanBlockReason;
        private ManualScanFailureReason _lastManualScanFailureReason;
        private int _currentPrestigeCount;

        public int ManualClickValue => _manualClickValue;
        public float CurrentProcessingDuration => _aiTSAScannerUpgrade != null
            ? _aiTSAScannerUpgrade.CurrentProcessingDuration
            : _aiScanner != null
                ? _aiScanner.ProcessingDuration
                : 0f;
        public float CurrentPassengersPerSecond => _aiTSAScannerUpgrade != null
            ? _aiTSAScannerUpgrade.CurrentPassengersPerSecond
            : HasActiveAIScanner && CurrentProcessingDuration > 0f
                ? 1f / CurrentProcessingDuration
                : 0f;
        public bool HasActiveAIScanner => _aiScanner != null && _aiScanner.IsOperational;
        public bool CanAutoProcessAIScanner => HasActiveAIScanner && (_aiTSAScannerUpgrade == null || _aiTSAScannerUpgrade.CanAutoProcess);
        public bool IsPassengerFlowBlocked => _waitingRoom != null && !_waitingRoom.HasReservableCapacity;
        public bool IsGameplayActive => isActiveAndEnabled;
        public bool CanManualScan => EvaluateCanManualScan(out _);
        public bool CanProcessManualClick => CanManualScan;
        public ManualScanFailureReason LastManualScanFailureReason => _lastManualScanFailureReason;
        public WaitingRoomUIController WaitingRoom => _waitingRoom;

        private void Awake()
        {
            AutoWireScanners();
            RebuildManualLanes();
            RefreshManualScannerUnlocks(_currentPrestigeCount);
        }

        private void Update()
        {
            LogManualScanStateIfNeeded();
            FillAvailableManualScanners();
            if (CanAutoProcessAIScanner)
                TryProcessOnePassenger(_aiScanner);
        }

        private void OnDisable()
        {
            ReleaseManualReservations();
            CancelAutoProcessingReservation();
        }

        public void RefreshManualScannerUnlocks(int prestigeCount)
        {
            _currentPrestigeCount = Mathf.Max(0, prestigeCount);
            EnsureManualLanesReady();

            int unlockedCount = _manualScanners.Count > 0
                ? Mathf.Clamp(1 + _currentPrestigeCount, 1, _manualScanners.Count)
                : 0;

            for (int i = 0; i < _manualLanes.Count; i++)
            {
                ManualScannerLane lane = _manualLanes[i];
                bool isUnlocked = i < unlockedCount;
                lane.IsUnlocked = isUnlocked;

                if (lane.Scanner == null)
                    continue;

                if (!isUnlocked)
                {
                    ReleaseReservations(lane.Scanner.RecycleHeldPassengers());
                }

                lane.Scanner.gameObject.SetActive(isUnlocked);
            }

            FillAvailableManualScanners();
        }

        public void FillAvailableManualScanners()
        {
            EnsureManualLanesReady();

            if (_queue == null || _waitingRoom == null)
                return;

            for (int i = 0; i < _manualLanes.Count; i++)
            {
                ManualScannerLane lane = _manualLanes[i];
                if (!lane.CanAcceptPassenger)
                    continue;

                if (!_queue.HasPassengerReady)
                    return;

                if (!_waitingRoom.TryReserveSlot())
                    return;

                if (!_queue.TryDequeuePassenger(out PassengerUIVisual passenger))
                {
                    _waitingRoom.ReleaseReservedSlot();
                    return;
                }

                if (!lane.Scanner.TryHoldPassenger(passenger))
                {
                    _waitingRoom.ReleaseReservedSlot();
                    passenger.Recycle();
                    continue;
                }

                _queue.RefillBackSlotIfPossible();
            }
        }

        private bool TryProcessOnePassenger(ScannerStationUIController scanner)
        {
            if (scanner == null || !scanner.IsOperational || scanner.IsBusy) return false;
            if (_aiTSAScannerUpgrade != null && !_aiTSAScannerUpgrade.CanAutoProcess) return false;
            if (_queue == null || !_queue.HasPassengerReady) return false;
            if (_waitingRoom == null || !_waitingRoom.TryReserveSlot()) return false;

            PassengerUIVisual passenger;
            if (!_queue.TryDequeuePassenger(out passenger))
            {
                _waitingRoom.ReleaseReservedSlot();
                return false;
            }

            if (!scanner.TryStartAutoProcessing(passenger, OnAutoScannerCompleted))
            {
                _waitingRoom.ReleaseReservedSlot();
                return false;
            }

            _queue.RefillBackSlotIfPossible();
            return true;
        }

        public bool TryManualScan()
        {
            _lastManualScanFailureReason = ManualScanFailureReason.None;
            EnsureManualLanesReady();
            FillAvailableManualScanners();

            if (_waitingRoom == null)
            {
                SetManualScanFailure(ManualScanFailureReason.MissingWaitingRoom);
                return false;
            }

            bool processedAny = false;
            bool foundReadyPassenger = false;

            for (int i = 0; i < _manualLanes.Count; i++)
            {
                ManualScannerLane lane = _manualLanes[i];
                ScannerStationUIController scanner = lane.Scanner;
                if (!lane.HasReadyPassenger || scanner == null || scanner.IsBusy)
                    continue;

                foundReadyPassenger = true;

                PassengerUIVisual passenger;
                if (!scanner.TryReleaseOneHeldPassenger(out passenger))
                {
                    SetManualScanFailure(ManualScanFailureReason.DequeueFailed, scanner);
                    continue;
                }

                bool processed = CompleteProcessedPassenger(
                    passenger,
                    consumeReservation: true,
                    out PassengerRewardBreakdown reward,
                    instantWaitingRoomPlacement: true);

                if (!processed)
                {
                    SetManualScanFailure(ManualScanFailureReason.WaitingRoomReceiveFailed, scanner);
                    continue;
                }

                processedAny = true;
                OnPassengerProcessed?.Invoke(new PassengerProcessedData(
                    PassengerProcessingType.Manual,
                    reward.TotalReward,
                    reward.BaseReward,
                    reward.ShopBonus));
            }

            FillAvailableManualScanners();

            if (!processedAny && !foundReadyPassenger)
                SetManualScanFailure(ResolveNoReadyManualPassengerReason(), GetFirstUnlockedManualScanner());

            return processedAny;
        }

        public void ProcessManualClick()
        {
            TryManualScan();
        }

        public void SetEconomyController(EconomyController controller)
        {
            _economyController = controller;
        }

        public void EnableAIScanner()
        {
            if (_aiScanner != null)
                _aiScanner.gameObject.SetActive(true);
        }

        public void DisableAIScanner()
        {
            if (_aiScanner == null)
                return;

            CancelAutoProcessingReservation();
            _aiScanner.ResetForPrestige();
            _aiScanner.gameObject.SetActive(false);
        }

        public void ResetForPrestige()
        {
            ReleaseManualReservations();
            CancelAutoProcessingReservation();

            for (int i = 0; i < _manualLanes.Count; i++)
            {
                if (_manualLanes[i].Scanner != null)
                    _manualLanes[i].Scanner.ResetForPrestige();
            }

            if (_aiScanner != null)
            {
                _aiScanner.ResetForPrestige();
                _aiScanner.gameObject.SetActive(false);
            }

            if (_waitingRoom != null)
                _waitingRoom.ResetForPrestige();

            if (_queue != null)
                _queue.ResetForPrestige();

            _lastManualScanFailureReason = ManualScanFailureReason.None;
            RefreshManualScannerUnlocks(_currentPrestigeCount);
        }

        public void SetAIAutoProcessingDuration(float duration)
        {
            if (_aiScanner == null) return;

            _aiScanner.SetAutoProcessingDuration(duration);
        }

        private void OnAutoScannerCompleted(PassengerUIVisual passenger)
        {
            bool processed = CompleteProcessedPassenger(
                passenger,
                consumeReservation: true,
                out PassengerRewardBreakdown reward,
                instantWaitingRoomPlacement: true);
            if (!processed)
                return;

            OnPassengerProcessed?.Invoke(new PassengerProcessedData(
                PassengerProcessingType.Auto,
                reward.TotalReward,
                reward.BaseReward,
                reward.ShopBonus));

            if (_aiTSAScannerUpgrade != null && !_aiTSAScannerUpgrade.TryConsumeTokenAfterAutoScan())
            {
                Debug.LogWarning("PassengerProcessor: Auto scan completed but AI TSA tokens could not be consumed.", this);
            }
        }

        private bool CompleteProcessedPassenger(
            PassengerUIVisual passenger,
            bool consumeReservation,
            out PassengerRewardBreakdown reward,
            bool instantWaitingRoomPlacement = false)
        {
            reward = default;

            if (passenger == null)
            {
                if (consumeReservation && _waitingRoom != null)
                    _waitingRoom.ReleaseReservedSlot();
                return false;
            }

            if (_waitingRoom == null)
            {
                if (consumeReservation)
                    passenger.Recycle();
                return false;
            }

            bool enteredWaitingRoom = consumeReservation
                ? instantWaitingRoomPlacement
                    ? _waitingRoom.TryReceivePassengerWithReservationImmediate(passenger)
                    : _waitingRoom.TryReceivePassengerWithReservation(passenger)
                : instantWaitingRoomPlacement
                    ? _waitingRoom.TryReceivePassengerImmediate(passenger)
                    : _waitingRoom.TryReceivePassenger(passenger);

            if (!enteredWaitingRoom)
            {
                if (consumeReservation)
                    _waitingRoom.ReleaseReservedSlot();

                passenger.Recycle();
                return false;
            }

            reward = RewardProcessedPassenger();
            return true;
        }

        private PassengerRewardBreakdown RewardProcessedPassenger()
        {
            PassengerRewardBreakdown reward = GetPassengerRewardBreakdown();
            if (_economyController == null) return reward;

            _economyController.RewardProcessedPassenger(reward.TotalReward);
            return reward;
        }

        public double GetTotalPassengerReward()
        {
            PassengerRewardBreakdown reward = GetPassengerRewardBreakdown();
            return reward.TotalReward;
        }

        public double GetShopsPassengerIncomeBonus()
        {
            return _storesManager != null ? _storesManager.GetPassengerIncomeBonus() : 0.0;
        }

        public string GetManualScanBlockReason()
        {
            EvaluateCanManualScan(out string reason);
            return reason;
        }

        public string GetLastManualScanFailureReason()
        {
            return _lastManualScanFailureReason.ToString();
        }

        private PassengerRewardBreakdown GetPassengerRewardBreakdown()
        {
            double baseIncome = _economyController != null
                ? _economyController.GetBasePassengerIncome()
                : 0.0;
            double shopsBonus = _storesManager != null
                ? _storesManager.GetPassengerIncomeBonus()
                : 0.0;

            return new PassengerRewardBreakdown(baseIncome, shopsBonus);
        }

        private bool EvaluateCanManualScan(out string blockReason)
        {
            if (!isActiveAndEnabled)
            {
                blockReason = "PassengerProcessor is inactive.";
                return false;
            }

            if (_queue == null)
            {
                blockReason = "Passenger queue reference is missing.";
                return false;
            }

            if (_waitingRoom == null)
            {
                blockReason = "Waiting room reference is missing.";
                return false;
            }

            EnsureManualLanesReady();

            if (HasReadyManualPassenger())
            {
                blockReason = string.Empty;
                return true;
            }

            if (!_queue.HasPassengerReady)
            {
                blockReason = $"Queue has no passengers. Active count: {_queue.ActivePassengerCount}.";
                return false;
            }

            if (!_waitingRoom.HasReservableCapacity)
            {
                blockReason =
                    $"Gate has no reservable capacity. Passengers: {_waitingRoom.CurrentCount}, Reserved: {_waitingRoom.ReservedCount}, Capacity: {_waitingRoom.Capacity}.";
                return false;
            }

            if (!HasUnlockedManualScanner())
            {
                blockReason = "No manual scanners are unlocked.";
                return false;
            }

            blockReason = string.Empty;
            return true;
        }

        private void LogManualScanStateIfNeeded()
        {
            if (!_debugManualScanState) return;

            bool canManualScan = EvaluateCanManualScan(out string blockReason);
            if (canManualScan == _lastCanManualScan && blockReason == _lastManualScanBlockReason) return;

            _lastCanManualScan = canManualScan;
            _lastManualScanBlockReason = blockReason;

            if (canManualScan)
            {
                Debug.Log(
                    $"PassengerProcessor manual scan ready. Queue: {_queue.ActivePassengerCount}, Gate: {_waitingRoom.CurrentCount}, Reserved: {_waitingRoom.ReservedCount}, Manual lanes: {GetUnlockedManualLaneCount()}.",
                    this);
                return;
            }

            Debug.Log(
                $"PassengerProcessor manual scan blocked. Reason: {blockReason}",
                this);
        }

        private void ReleaseManualReservations()
        {
            if (_waitingRoom == null) return;

            EnsureManualLanesReady();
            for (int i = 0; i < _manualLanes.Count; i++)
            {
                ScannerStationUIController scanner = _manualLanes[i].Scanner;
                if (scanner == null) continue;

                int releasedPassengers = scanner.RecycleHeldPassengers();
                ReleaseReservations(releasedPassengers);
            }
        }

        private void CancelAutoProcessingReservation()
        {
            if (_aiScanner == null || _waitingRoom == null) return;

            if (_aiScanner.CancelAutoProcessing())
                ReleaseReservations(1);
        }

        private void ReleaseReservations(int count)
        {
            if (_waitingRoom == null) return;

            for (int i = 0; i < count; i++)
                _waitingRoom.ReleaseReservedSlot();
        }

        private void SetManualScanFailure(ManualScanFailureReason reason)
        {
            SetManualScanFailure(reason, GetFirstUnlockedManualScanner());
        }

        private void SetManualScanFailure(ManualScanFailureReason reason, ScannerStationUIController scanner)
        {
            _lastManualScanFailureReason = reason;
            OnPassengerProcessFailed?.Invoke(new PassengerProcessFailedFeedbackData(
                PassengerProcessingType.Manual,
                reason,
                GetFeedbackWorldPosition(scanner, null)));
        }

        private void AutoWireScanners()
        {
            if (_manualScanners == null)
                _manualScanners = new List<ScannerStationUIController>();

            if (_manualScanner != null && !_manualScanners.Contains(_manualScanner))
                _manualScanners.Insert(0, _manualScanner);

            if (_aiScanner != null && _manualScanners.Count > 0)
                return;

            var scanners = FindObjectsByType<ScannerStationUIController>(FindObjectsSortMode.None);
            Array.Sort(scanners, CompareScannerPriority);

            for (int i = 0; i < scanners.Length; i++)
            {
                ScannerStationUIController scanner = scanners[i];
                if (scanner == null)
                    continue;

                if (scanner.IsAutoScanner)
                {
                    if (_aiScanner == null)
                        _aiScanner = scanner;
                    continue;
                }

                if (_manualScanner == null)
                    _manualScanner = scanner;

                if (!_manualScanners.Contains(scanner))
                    _manualScanners.Add(scanner);
            }
        }

        private static int CompareScannerPriority(ScannerStationUIController left, ScannerStationUIController right)
        {
            if (left == right) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int siblingCompare = left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
            if (siblingCompare != 0)
                return siblingCompare;

            return string.Compare(left.name, right.name, StringComparison.Ordinal);
        }

        private void EnsureManualLanesReady()
        {
            if (_manualLanes.Count == _manualScanners.Count)
                return;

            RebuildManualLanes();
        }

        private void RebuildManualLanes()
        {
            if (_manualScanners == null)
                _manualScanners = new List<ScannerStationUIController>();

            _manualLanes.Clear();
            for (int i = 0; i < _manualScanners.Count; i++)
            {
                ScannerStationUIController scanner = _manualScanners[i];
                if (scanner == null || scanner.IsAutoScanner)
                    continue;

                _manualLanes.Add(new ManualScannerLane(scanner));
            }
        }

        private ManualScanFailureReason ResolveNoReadyManualPassengerReason()
        {
            if (_queue == null)
                return ManualScanFailureReason.MissingQueue;

            if (_waitingRoom == null)
                return ManualScanFailureReason.MissingWaitingRoom;

            if (!_queue.HasPassengerReady)
                return ManualScanFailureReason.NoPassengers;

            if (!_waitingRoom.HasReservableCapacity)
                return ManualScanFailureReason.NoReservableCapacity;

            return ManualScanFailureReason.NoPassengers;
        }

        private bool HasReadyManualPassenger()
        {
            for (int i = 0; i < _manualLanes.Count; i++)
            {
                if (_manualLanes[i].HasReadyPassenger)
                    return true;
            }

            return false;
        }

        private bool HasUnlockedManualScanner()
        {
            return GetFirstUnlockedManualScanner() != null;
        }

        private int GetUnlockedManualLaneCount()
        {
            int count = 0;
            for (int i = 0; i < _manualLanes.Count; i++)
            {
                if (_manualLanes[i].IsUnlocked && _manualLanes[i].Scanner != null)
                    count++;
            }

            return count;
        }

        private ScannerStationUIController GetFirstUnlockedManualScanner()
        {
            EnsureManualLanesReady();
            for (int i = 0; i < _manualLanes.Count; i++)
            {
                if (_manualLanes[i].IsUnlocked && _manualLanes[i].Scanner != null)
                    return _manualLanes[i].Scanner;
            }

            return _manualScanner;
        }

        private static Vector3 GetFeedbackWorldPosition(ScannerStationUIController scanner, PassengerUIVisual passenger)
        {
            if (passenger != null)
                return passenger.transform.position;

            return scanner != null ? scanner.FeedbackWorldPosition : Vector3.zero;
        }

        private readonly struct PassengerRewardBreakdown
        {
            public PassengerRewardBreakdown(double baseReward, double shopBonus)
            {
                BaseReward = baseReward;
                ShopBonus = shopBonus;
            }

            public double BaseReward { get; }
            public double ShopBonus { get; }
            public double TotalReward => BaseReward + ShopBonus;
        }
    }
}