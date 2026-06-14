using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerProcessor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private PassengerQueueUIController _queue;
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private ScannerStationUIController _manualScanner;
        [SerializeField] private ScannerStationUIController _aiScanner;

        [Header("Settings")]
        [SerializeField] private int _manualClickValue = 1;
        [SerializeField] private float _passengersPerSecond;
        [SerializeField] private float _maxPendingPassengers = 10f;
        private float _passengerAccumulator;

        public int ManualClickValue => _manualClickValue;
        public float PassengersPerSecond => _passengersPerSecond;
        public bool IsPassengerFlowBlocked => _waitingRoom != null && !_waitingRoom.HasReservableCapacity;
        public bool CanProcessManualClick =>
            _manualScanner != null &&
            _manualScanner.HeldCount > 0 &&
            _waitingRoom != null &&
            _waitingRoom.HasPhysicalCapacity;

        private void Update()
        {
            TryFeedManualScanner();

            if (_passengersPerSecond <= 0f) return;
            if (_economyController == null) return;
            if (_aiScanner == null) return;

            _passengerAccumulator += _passengersPerSecond * Time.deltaTime;
            _passengerAccumulator = Mathf.Min(_passengerAccumulator, _maxPendingPassengers);

            int requested = Mathf.FloorToInt(_passengerAccumulator);
            if (requested <= 0) return;

            int processed = 0;
            for (int i = 0; i < requested; i++)
            {
                if (TryProcessOnePassenger(_aiScanner))
                    processed++;
                else
                    break;
            }

            if (processed > 0)
                _passengerAccumulator -= processed;
        }

        private void OnDisable()
        {
            ReleaseManualReservations();
            CancelAutoProcessingReservation();
        }

        private void TryFeedManualScanner()
        {
            if (_manualScanner == null || !_manualScanner.CanAcceptMore) return;
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
                return;
            }

            _queue.RefillBackSlotIfPossible();
        }

        private bool TryProcessOnePassenger(ScannerStationUIController scanner)
        {
            if (scanner == null || scanner.IsBusy) return false;
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

        public void ProcessManualClick()
        {
            if (_manualScanner == null) return;
            if (_waitingRoom == null) return;
            if (!_waitingRoom.HasPhysicalCapacity) return;

            PassengerUIVisual passenger;
            if (!_manualScanner.TryReleaseOneHeldPassenger(out passenger)) return;

            CompleteProcessedPassenger(passenger, consumeReservation: true);
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

        public void AddPassengersPerSecond(float amount)
        {
            _passengersPerSecond += amount;
        }

        public void ResetPassengersPerSecond()
        {
            _passengersPerSecond = 0f;
            _passengerAccumulator = 0f;
        }

        private void OnAutoScannerCompleted(PassengerUIVisual passenger)
        {
            CompleteProcessedPassenger(passenger, consumeReservation: true);
        }

        private bool CompleteProcessedPassenger(PassengerUIVisual passenger, bool consumeReservation)
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
                ? _waitingRoom.TryReceivePassengerWithReservation(passenger)
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

            _economyController.AddPassengers(1);
            _economyController.AddMoney(1 * _economyController.MoneyPerPassenger);
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
