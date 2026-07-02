using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerProcessor : MonoBehaviour
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

        public readonly struct PassengerProcessFeedbackData
        {
            public PassengerProcessFeedbackData(
                PassengerProcessingType processingType,
                double totalReward,
                double baseReward,
                double shopBonus,
                Vector3 feedbackWorldPosition)
            {
                ProcessingType = processingType;
                TotalReward = totalReward;
                BaseReward = baseReward;
                ShopBonus = shopBonus;
                FeedbackWorldPosition = feedbackWorldPosition;
            }

            public PassengerProcessingType ProcessingType { get; }
            public double TotalReward { get; }
            public double BaseReward { get; }
            public double ShopBonus { get; }
            public Vector3 FeedbackWorldPosition { get; }
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

        public event Action<PassengerProcessFeedbackData> OnPassengerManuallyProcessed;
        public event Action<PassengerProcessFeedbackData> OnPassengerAutoProcessed;
        public event Action<PassengerProcessFailedFeedbackData> OnPassengerProcessFailed;

        [Header("References")]
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private StoresManager _storesManager;
        [SerializeField] private PassengerQueueUIController _queue;
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private AITSAScannerUpgrade _aiTSAScannerUpgrade;
        [SerializeField] private ScannerStationUIController _manualScanner;
        [SerializeField] private ScannerStationUIController _aiScanner;

        [Header("Settings")]
        [SerializeField] private int _manualClickValue = 1;
        [SerializeField] private bool _debugManualScanState;

        private bool _lastCanManualScan;
        private string _lastManualScanBlockReason;
        private ManualScanFailureReason _lastManualScanFailureReason;

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

        private void Update()
        {
            LogManualScanStateIfNeeded();
            TryRefillManualScanner();
            if (CanAutoProcessAIScanner)
                TryProcessOnePassenger(_aiScanner);
        }

        private void OnDisable()
        {
            ReleaseManualReservations();
            CancelAutoProcessingReservation();
        }

        private void TryRefillManualScanner()
        {
            if (_manualScanner == null || !_manualScanner.IsOperational) return;
            if (_manualScanner.HasManualPassengerReady) return;
            if (_queue == null || !_queue.HasPassengerReady) return;
            if (_waitingRoom == null || !_waitingRoom.TryReserveSlot()) return;

            PassengerUIVisual passenger;
            if (!_queue.TryDequeuePassenger(out passenger))
            {
                _waitingRoom.ReleaseReservedSlot();
                return;
            }

            if (!_manualScanner.TryHoldPassenger(passenger))
            {
                _waitingRoom.ReleaseReservedSlot();
                passenger.Recycle();
                return;
            }

            _queue.RefillBackSlotIfPossible();
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

            if (_waitingRoom == null)
            {
                SetManualScanFailure(ManualScanFailureReason.MissingWaitingRoom);
                return false;
            }

            if (!_waitingRoom.HasReservableCapacity)
            {
                SetManualScanFailure(ManualScanFailureReason.NoReservableCapacity);
                return false;
            }

            if (_manualScanner == null || !_manualScanner.HasManualPassengerReady)
            {
                SetManualScanFailure(ManualScanFailureReason.NoPassengers);
                return false;
            }

            PassengerUIVisual passenger;
            if (!_manualScanner.TryReleaseOneHeldPassenger(out passenger))
            {
                SetManualScanFailure(ManualScanFailureReason.DequeueFailed);
                return false;
            }

            Vector3 feedbackPosition = GetFeedbackWorldPosition(_manualScanner, passenger);
            bool processed = CompleteProcessedPassenger(
                passenger,
                consumeReservation: true,
                out PassengerRewardBreakdown reward,
                instantWaitingRoomPlacement: true);

            if (!processed)
            {
                SetManualScanFailure(ManualScanFailureReason.WaitingRoomReceiveFailed);
            }
            else
            {
                OnPassengerManuallyProcessed?.Invoke(new PassengerProcessFeedbackData(
                    PassengerProcessingType.Manual,
                    reward.TotalReward,
                    reward.BaseReward,
                    reward.ShopBonus,
                    feedbackPosition));
            }

            TryRefillManualScanner();
            return processed;
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

        public void SetAIAutoProcessingDuration(float duration)
        {
            if (_aiScanner == null) return;

            _aiScanner.SetAutoProcessingDuration(duration);
        }

        private void OnAutoScannerCompleted(PassengerUIVisual passenger)
        {
            Vector3 feedbackPosition = GetFeedbackWorldPosition(_aiScanner, passenger);
            bool processed = CompleteProcessedPassenger(
                passenger,
                consumeReservation: true,
                out PassengerRewardBreakdown reward,
                instantWaitingRoomPlacement: true);
            if (!processed)
                return;

            OnPassengerAutoProcessed?.Invoke(new PassengerProcessFeedbackData(
                PassengerProcessingType.Auto,
                reward.TotalReward,
                reward.BaseReward,
                reward.ShopBonus,
                feedbackPosition));

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

            if (!_queue.HasPassengerReady)
            {
                if (_manualScanner != null && _manualScanner.HasManualPassengerReady)
                {
                    blockReason = string.Empty;
                    return true;
                }

                blockReason = $"Queue has no passengers. Active count: {_queue.ActivePassengerCount}.";
                return false;
            }

            if (!_waitingRoom.HasReservableCapacity)
            {
                blockReason =
                    $"Gate has no reservable capacity. Passengers: {_waitingRoom.CurrentCount}, Reserved: {_waitingRoom.ReservedCount}, Capacity: {_waitingRoom.Capacity}.";
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
                    $"PassengerProcessor manual scan ready. Queue: {_queue.ActivePassengerCount}, Gate: {_waitingRoom.CurrentCount}, Reserved: {_waitingRoom.ReservedCount}.",
                    this);
                return;
            }

            Debug.Log(
                $"PassengerProcessor manual scan blocked. Reason: {blockReason}",
                this);
        }

        private void ReleaseManualReservations()
        {
            if (_manualScanner == null || _waitingRoom == null) return;

            int releasedPassengers = _manualScanner.RecycleHeldPassengers();
            ReleaseReservations(releasedPassengers);
        }

        private void CancelAutoProcessingReservation()
        {
            if (_aiScanner == null || _waitingRoom == null) return;

            if (_aiScanner.CancelAutoProcessing())
                ReleaseReservations(1);
        }

        private void ReleaseReservations(int count)
        {
            for (int i = 0; i < count; i++)
                _waitingRoom.ReleaseReservedSlot();
        }

        private void SetManualScanFailure(ManualScanFailureReason reason)
        {
            _lastManualScanFailureReason = reason;
            OnPassengerProcessFailed?.Invoke(new PassengerProcessFailedFeedbackData(
                PassengerProcessingType.Manual,
                reason,
                GetFeedbackWorldPosition(_manualScanner, null)));
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