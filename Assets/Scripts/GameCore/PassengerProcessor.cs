using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerProcessor : MonoBehaviour
    {
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

        [Header("References")]
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private StoresManager _storesManager;
        [SerializeField] private PassengerQueueUIController _queue;
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private ScannerStationUIController _manualScanner;
        [SerializeField] private ScannerStationUIController _aiScanner;

        [Header("Settings")]
        [SerializeField] private int _manualClickValue = 1;
        [SerializeField] private bool _debugManualScanState;

        private bool _lastCanManualScan;
        private string _lastManualScanBlockReason;
        private ManualScanFailureReason _lastManualScanFailureReason;

        public int ManualClickValue => _manualClickValue;
        public float CurrentProcessingDuration => _aiScanner != null ? _aiScanner.ProcessingDuration : 0f;
        public float CurrentPassengersPerSecond => HasActiveAIScanner && CurrentProcessingDuration > 0f
            ? 1f / CurrentProcessingDuration
            : 0f;
        public bool HasActiveAIScanner => _aiScanner != null && _aiScanner.IsOperational;
        public bool IsPassengerFlowBlocked => _waitingRoom != null && !_waitingRoom.HasReservableCapacity;
        public bool IsGameplayActive => isActiveAndEnabled;
        public bool CanManualScan => EvaluateCanManualScan(out _);
        public bool CanProcessManualClick => CanManualScan;
        public ManualScanFailureReason LastManualScanFailureReason => _lastManualScanFailureReason;

        private void Awake()
        {
            EnsureStoresManagerReference();
        }

        private void OnValidate()
        {
            EnsureStoresManagerReference(logWarning: false);
        }

        private void Update()
        {
            LogManualScanStateIfNeeded();
            TryRefillManualScanner();
            if (HasActiveAIScanner)
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
                _lastManualScanFailureReason = ManualScanFailureReason.MissingWaitingRoom;
                return false;
            }

            if (_manualScanner == null || !_manualScanner.HasManualPassengerReady)
            {
                _lastManualScanFailureReason = ManualScanFailureReason.NoPassengers;
                return false;
            }

            PassengerUIVisual passenger;
            if (!_manualScanner.TryReleaseOneHeldPassenger(out passenger))
            {
                _lastManualScanFailureReason = ManualScanFailureReason.DequeueFailed;
                return false;
            }

            bool processed = CompleteProcessedPassenger(passenger, consumeReservation: true, instantWaitingRoomPlacement: true);
            if (!processed)
                _lastManualScanFailureReason = ManualScanFailureReason.WaitingRoomReceiveFailed;

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
            CompleteProcessedPassenger(passenger, consumeReservation: true, instantWaitingRoomPlacement: true);
        }

        private bool CompleteProcessedPassenger(PassengerUIVisual passenger, bool consumeReservation, bool instantWaitingRoomPlacement = false)
        {
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

            RewardProcessedPassenger();
            return true;
        }

        private void RewardProcessedPassenger()
        {
            if (_economyController == null) return;

            double totalReward = GetTotalPassengerReward();
            _economyController.RewardProcessedPassenger(totalReward);
        }

        public double GetTotalPassengerReward()
        {
            double baseIncome = _economyController != null
                ? _economyController.GetBasePassengerIncome()
                : 0.0;
            double shopsBonus = _storesManager != null
                ? _storesManager.GetPassengerIncomeBonus()
                : 0.0;

            return baseIncome + shopsBonus;
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

        private void EnsureStoresManagerReference(bool logWarning = true)
        {
            if (_storesManager != null) return;

            _storesManager = FindFirstObjectByType<StoresManager>();
            if (_storesManager == null && logWarning)
            {
                Debug.LogWarning("PassengerProcessor: StoresManager is not assigned, shop passenger bonus will not be applied.", this);
            }
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
                    $"Waiting room has no reservable capacity. Passengers: {_waitingRoom.CurrentCount}, Reserved: {_waitingRoom.ReservedCount}, Capacity: {_waitingRoom.Capacity}.";
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
                    $"PassengerProcessor manual scan ready. Queue: {_queue.ActivePassengerCount}, Waiting: {_waitingRoom.CurrentCount}, Reserved: {_waitingRoom.ReservedCount}.",
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
    }
}
