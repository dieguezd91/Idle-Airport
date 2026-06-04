using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class StoresManager : MonoBehaviour
    {
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private Store[] _stores;

        private double _pendingBusinessIncome;

        public event Action OnBusinessesChanged;
        public event Action<int, Store> OnStorePurchased;

        public Store[] Businesses => _stores;
        public int BusinessCount => _stores != null ? _stores.Length : 0;

        public double TotalIncomePerSecond
        {
            get
            {
                if (_stores == null) return 0.0;
                double total = 0.0;
                foreach (var business in _stores)
                {
                    if (business.OwnedCount > 0)
                    {
                        total += business.IncomePerPassenger * business.OwnedCount;
                    }
                }
                return total;
            }
        }

        private void Update()
        {
            if (_economyController == null) return;

            if (_passengerProcessor != null && _passengerProcessor.IsPassengerFlowBlocked) return;

            if (_stores == null) return;

            double totalRate = TotalIncomePerSecond;
            if (totalRate <= 0.0) return;

            _pendingBusinessIncome += totalRate * Time.deltaTime;

            if (_pendingBusinessIncome >= 1.0)
            {
                double wholeIncome = Math.Floor(_pendingBusinessIncome);
                _pendingBusinessIncome -= wholeIncome;
                _economyController.AddMoney(wholeIncome);
            }
        }

        public bool CanPurchaseBusiness(int index)
        {
            if (_stores == null) return false;
            if (index < 0 || index >= _stores.Length) return false;

            Store business = _stores[index];
            return business.CanPurchase(_economyController.Money);
        }

        public bool TryPurchaseBusiness(int index)
        {
            if (_stores == null) return false;
            if (index < 0 || index >= _stores.Length) return false;

            Store business = _stores[index];
            if (!business.CanPurchase(_economyController.Money)) return false;

            if (!_economyController.SpendMoney(business.CurrentCost)) return false;

            business.Purchase();
            OnStorePurchased?.Invoke(index, business);
            OnBusinessesChanged?.Invoke();
            return true;
        }

        public bool TryUnlockBusiness(int index)
        {
            if (index < 0 || index >= _stores.Length) return false;

            Store business = _stores[index];
            if (business.IsUnlocked) return false;

            business.Unlock();
            OnBusinessesChanged?.Invoke();
            return true;
        }
    }
}
