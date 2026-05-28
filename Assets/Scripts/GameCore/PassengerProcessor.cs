using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerProcessor : MonoBehaviour
    {
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private QueueUIVisualController _queueVisual;
        [SerializeField] private int _manualClickValue = 1;

        [SerializeField] private float _passengersPerSecond;
        [SerializeField] private float _maxPendingPassengers = 10f;
        private float _passengerAccumulator;

        public int ManualClickValue => _manualClickValue;
        public float PassengersPerSecond => _passengersPerSecond;
        public bool IsPassengerFlowBlocked => _queueVisual != null && _queueVisual.IsBlocked;

        private void Update()
        {
            if (_passengersPerSecond <= 0f) return;
            if (_economyController == null) return;
            if (_queueVisual == null) return;

            _passengerAccumulator += _passengersPerSecond * Time.deltaTime;
            _passengerAccumulator = Mathf.Min(_passengerAccumulator, _maxPendingPassengers);

            int requested = Mathf.FloorToInt(_passengerAccumulator);
            if (requested <= 0) return;

            int processed = 0;
            for (int i = 0; i < requested; i++)
            {
                if (_queueVisual.TryProcessFrontPassenger())
                    processed++;
                else
                    break;
            }

            if (processed > 0)
            {
                _passengerAccumulator -= processed;
                _economyController.AddPassengers(processed);
                _economyController.AddMoney(processed * _economyController.MoneyPerPassenger);
            }
        }

        public void ProcessPassengers(int count)
        {
            if (_economyController == null)
            {
                Debug.LogError("PassengerProcessor: EconomyController is not assigned!");
                return;
            }

            if (_queueVisual == null || count <= 0) return;

            int processed = 0;
            for (int i = 0; i < count; i++)
            {
                if (_queueVisual.TryProcessFrontPassenger())
                    processed++;
                else
                    break;
            }

            if (processed > 0)
            {
                _economyController.AddPassengers(processed);
                _economyController.AddMoney(processed * _economyController.MoneyPerPassenger);
            }
        }

        public void ProcessManualClick()
        {
            if (_queueVisual != null && _queueVisual.IsBlocked) return;
            ProcessPassengers(_manualClickValue);
        }

        public void SetEconomyController(EconomyController controller)
        {
            _economyController = controller;
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
