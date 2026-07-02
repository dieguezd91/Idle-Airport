using System;
using IdleAirport.GameCore.Prestige;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class EconomyController : MonoBehaviour, IPrestigeResettable
    {
        public event Action<double> OnMoneyChanged;
        public event Action<int> OnTotalPassengersProcessedChanged;

        [SerializeField] private MonoBehaviour _prestigeMultiplierProviderBehaviour;
        [SerializeField] private double _money;
        [SerializeField] private int _totalPassengersProcessed;
        [SerializeField] private int _moneyPerPassenger = 1;

        private IPrestigeMultiplierProvider _prestigeMultiplierProvider;

        public double Money => _money;
        public int TotalPassengersProcessed => _totalPassengersProcessed;
        public int MoneyPerPassenger => _moneyPerPassenger;

        private void Awake()
        {
            CachePrestigeMultiplierProvider();
        }

        private void OnValidate()
        {
            if (_prestigeMultiplierProviderBehaviour != null
                && _prestigeMultiplierProviderBehaviour is not IPrestigeMultiplierProvider)
            {
                Debug.LogWarning(
                    "EconomyController: Prestige multiplier provider must implement IPrestigeMultiplierProvider.",
                    this);
            }
        }

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

            totalReward *= GetPrestigeMultiplier();

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
            ResetRunStateForPrestige();
            _moneyPerPassenger = 1;
        }

        public void ResetRunStateForPrestige()
        {
            _money = 0;
            _totalPassengersProcessed = 0;
            OnMoneyChanged?.Invoke(_money);
            OnTotalPassengersProcessedChanged?.Invoke(_totalPassengersProcessed);
        }

        public void ResetForPrestige()
        {
            ResetRunStateForPrestige();
        }

        public void SetPrestigeMultiplierProvider(IPrestigeMultiplierProvider provider)
        {
            _prestigeMultiplierProvider = provider;
            _prestigeMultiplierProviderBehaviour = provider as MonoBehaviour;
        }

        private double GetPrestigeMultiplier()
        {
            if (_prestigeMultiplierProvider == null)
                CachePrestigeMultiplierProvider();

            double multiplier = _prestigeMultiplierProvider != null
                ? _prestigeMultiplierProvider.GlobalPrestigeMultiplier
                : 1d;

            return multiplier > 0d ? multiplier : 1d;
        }

        private void CachePrestigeMultiplierProvider()
        {
            _prestigeMultiplierProvider = _prestigeMultiplierProviderBehaviour as IPrestigeMultiplierProvider;
        }
    }
}