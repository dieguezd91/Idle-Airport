using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class EconomyController : MonoBehaviour
    {
        public event Action<double> OnMoneyChanged;
        public event Action<int> OnTotalPassengersProcessedChanged;

        [SerializeField] private double _money;
        [SerializeField] private int _totalPassengersProcessed;
        [SerializeField] private int _moneyPerPassenger = 1;

        public double Money => _money;
        public int TotalPassengersProcessed => _totalPassengersProcessed;
        public int MoneyPerPassenger => _moneyPerPassenger;

        public double GetBasePassengerIncome()
        {
            return _moneyPerPassenger;
        }

        public void RewardProcessedPassenger(double totalReward)
        {
            if (totalReward < 0)
            {
                Debug.LogWarning($"EconomyController: Processed passenger reward cannot be negative: {totalReward}");
                return;
            }

            AddPassengers(1);
            AddMoney(totalReward);
        }

        public void AddMoney(double amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"EconomyController: Cannot add negative money: {amount}");
                return;
            }

            _money += amount;
            OnMoneyChanged?.Invoke(_money);
        }

        public bool SpendMoney(double amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"EconomyController: SpendMoney requires a positive amount: {amount}");
                return false;
            }

            if (_money < amount) return false;

            _money -= amount;
            OnMoneyChanged?.Invoke(_money);
            return true;
        }

        public void AddPassengers(int count)
        {
            if (count < 0)
            {
                Debug.LogWarning($"EconomyController: Cannot add negative passengers: {count}");
                return;
            }

            _totalPassengersProcessed += count;
            OnTotalPassengersProcessedChanged?.Invoke(_totalPassengersProcessed);
        }

        public void SetMoneyPerPassenger(int value)
        {
            if (value < 0)
            {
                Debug.LogWarning($"EconomyController: MoneyPerPassenger cannot be negative: {value}");
                return;
            }

            _moneyPerPassenger = value;
        }

        public void Reset()
        {
            _money = 0;
            _totalPassengersProcessed = 0;
            _moneyPerPassenger = 1;
            OnMoneyChanged?.Invoke(_money);
            OnTotalPassengersProcessedChanged?.Invoke(_totalPassengersProcessed);
        }
    }
}
