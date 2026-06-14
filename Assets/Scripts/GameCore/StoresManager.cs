using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public enum StorePurchaseFailureReason
    {
        None,
        InvalidIndex,
        MissingEconomy,
        Locked,
        InsufficientFunds,
        SpendFailed
    }

    public sealed class StoresManager : MonoBehaviour
    {
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private Store[] _stores;

        public event Action OnBusinessesChanged;
        public event Action<int, Store> OnStorePurchased;
        public event Action<StorePurchaseFailureReason> OnStorePurchaseFailed;

        public Store[] Stores => _stores;
        public int StoreCount => _stores != null ? _stores.Length : 0;

        public double TotalPassengerIncomeBonus
        {
            get
            {
                if (_stores == null) return 0.0;
                double totalBonus = 0.0;
                foreach (var store in _stores)
                {
                    if (store.OwnedCount > 0)
                    {
                        totalBonus += store.IncomePerPassenger * store.OwnedCount;
                    }
                }
                return totalBonus;
            }
        }

        public double GetPassengerIncomeBonus() => TotalPassengerIncomeBonus;

        public bool CanPurchaseStore(int index)
        {
            if (_stores == null) return false;
            if (index < 0 || index >= _stores.Length) return false;

            Store store = _stores[index];
            return store.CanPurchase(_economyController.Money);
        }

        public bool TryPurchaseStore(int index)
        {
            if (_stores == null || index < 0 || index >= _stores.Length)
            {
                OnStorePurchaseFailed?.Invoke(StorePurchaseFailureReason.InvalidIndex);
                return false;
            }

            if (_economyController == null)
            {
                OnStorePurchaseFailed?.Invoke(StorePurchaseFailureReason.MissingEconomy);
                return false;
            }

            Store store = _stores[index];
            if (!store.IsUnlocked)
            {
                OnStorePurchaseFailed?.Invoke(StorePurchaseFailureReason.Locked);
                return false;
            }

            if (_economyController.Money < store.CurrentCost)
            {
                OnStorePurchaseFailed?.Invoke(StorePurchaseFailureReason.InsufficientFunds);
                return false;
            }

            if (!_economyController.SpendMoney(store.CurrentCost))
            {
                OnStorePurchaseFailed?.Invoke(StorePurchaseFailureReason.SpendFailed);
                return false;
            }

            store.Purchase();
            OnStorePurchased?.Invoke(index, store);
            OnBusinessesChanged?.Invoke();
            return true;
        }

        public bool TryUnlockStore(int index)
        {
            if (index < 0 || index >= _stores.Length) return false;

            Store store = _stores[index];
            if (store.IsUnlocked) return false;

            store.Unlock();
            OnBusinessesChanged?.Invoke();
            return true;
        }

        public bool CanPurchaseBusiness(int index) => CanPurchaseStore(index);
        public bool TryPurchaseBusiness(int index) => TryPurchaseStore(index);
        public bool TryUnlockBusiness(int index) => TryUnlockStore(index);
    }
}
