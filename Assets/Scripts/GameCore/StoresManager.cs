using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class StoresManager : MonoBehaviour
    {
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private Store[] _businesses;

        private double _pendingBusinessIncome;

        public event Action OnBusinessesChanged;

        public Store[] Businesses => _businesses;
        public int BusinessCount => _businesses != null ? _businesses.Length : 0;

        private void Update()
        {
            if (_passengerProcessor == null || _economyController == null) return;

            float pps = _passengerProcessor.PassengersPerSecond;
            if (pps <= 0f) return;

            if (_businesses == null) return;

            double totalRate = 0.0;
            foreach (var business in _businesses)
            {
                if (business.IsUnlocked && business.OwnedCount > 0)
                {
                    totalRate += business.IncomePerPassenger * business.OwnedCount;
                }
            }

            if (totalRate <= 0.0) return;

            _pendingBusinessIncome += pps * Time.deltaTime * totalRate;

            if (_pendingBusinessIncome >= 1.0)
            {
                double wholeIncome = Math.Floor(_pendingBusinessIncome);
                _pendingBusinessIncome -= wholeIncome;
                _economyController.AddMoney(wholeIncome);
            }
        }

        public bool CanPurchaseBusiness(int index)
        {
            if (_businesses == null) return false;
            if (index < 0 || index >= _businesses.Length) return false;

            Store business = _businesses[index];
            return business.CanPurchase(_economyController.Money);
        }

        public bool TryPurchaseBusiness(int index)
        {
            if (_businesses == null) return false;
            if (index < 0 || index >= _businesses.Length) return false;

            Store business = _businesses[index];
            if (!business.CanPurchase(_economyController.Money)) return false;

            if (!_economyController.SpendMoney(business.CurrentCost)) return false;

            business.Purchase();
            OnBusinessesChanged?.Invoke();
            return true;
        }

        public bool TryUnlockBusiness(int index)
        {
            if (index < 0 || index >= _businesses.Length) return false;

            Store business = _businesses[index];
            if (business.IsUnlocked) return false;

            business.Unlock();
            OnBusinessesChanged?.Invoke();
            return true;
        }
    }
}
