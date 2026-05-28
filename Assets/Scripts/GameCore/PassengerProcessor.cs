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
        public bool IsPassengerFlowBlocked => _waitingRoom != null && !_waitingRoom.HasCapacity;

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

        private void TryFeedManualScanner()
        {
            if (_manualScanner == null || !_manualScanner.CanAcceptMore) return;
            if (_queue == null || !_queue.HasPassengerReady) return;

            PassengerUIVisual passenger;
            if (!_queue.TryDequeuePassenger(out passenger)) return;

            _manualScanner.ProcessPassenger(passenger, _waitingRoom);
            _queue.RefillBackSlotIfPossible();
        }

        private bool TryProcessOnePassenger(ScannerStationUIController scanner)
        {
            if (_economyController == null) return false;
            if (_waitingRoom == null || !_waitingRoom.HasCapacity) return false;
            if (scanner == null || scanner.IsBusy) return false;
            if (_queue == null || !_queue.HasPassengerReady) return false;

            PassengerUIVisual passenger;
            if (!_queue.TryDequeuePassenger(out passenger)) return false;

            scanner.ProcessPassenger(passenger, _waitingRoom);
            _queue.RefillBackSlotIfPossible();

            _economyController.AddPassengers(1);
            _economyController.AddMoney(1 * _economyController.MoneyPerPassenger);

            return true;
        }

        public void ProcessManualClick()
        {
            if (_economyController == null) return;
            if (_manualScanner == null) return;
            if (_waitingRoom == null) return;

            if (!_manualScanner.TryReleaseOneToWaitingRoom(_waitingRoom)) return;

            _economyController.AddPassengers(1);
            _economyController.AddMoney(1 * _economyController.MoneyPerPassenger);
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
    }
}
