using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerProcessor : MonoBehaviour
    {
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private int _manualClickValue = 1;

        [SerializeField] private float _passengersPerSecond;
        private float _passengerAccumulator;

        public int ManualClickValue => _manualClickValue;
        public float PassengersPerSecond => _passengersPerSecond;

        private void Update()
        {
            if (_passengersPerSecond <= 0f) return;
            if (_economyController == null) return;

            _passengerAccumulator += _passengersPerSecond * Time.deltaTime;

            int wholePassengers = Mathf.FloorToInt(_passengerAccumulator);
            if (wholePassengers > 0)
            {
                _passengerAccumulator -= wholePassengers;
                ProcessPassengers(wholePassengers);
            }
        }

        public void ProcessPassengers(int count)
        {
            if (_economyController == null)
            {
                Debug.LogError("PassengerProcessor: EconomyController is not assigned!");
                return;
            }

            _economyController.AddPassengers(count);
            _economyController.AddMoney(count * _economyController.MoneyPerPassenger);
        }

        public void ProcessManualClick()
        {
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