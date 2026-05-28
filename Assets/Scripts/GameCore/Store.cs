using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    [Serializable]
    public sealed class Store
    {
        [SerializeField] private string _name;
        [SerializeField][TextArea] private string _description;
        [SerializeField] private double _incomePerPassenger;
        [SerializeField] private double _baseCost;
        [SerializeField] private double _costMultiplier = 1.15;
        [SerializeField] private int _ownedCount;
        [SerializeField] private bool _isUnlocked;

        public string Name => _name;
        public string Description => _description;
        public double IncomePerPassenger => _incomePerPassenger;
        public double CurrentCost => _baseCost * Math.Pow(_costMultiplier, _ownedCount);
        public int OwnedCount => _ownedCount;
        public bool IsUnlocked => _isUnlocked;

        public bool CanPurchase(double availableMoney)
        {
            return _isUnlocked && availableMoney >= CurrentCost;
        }

        public void Purchase()
        {
            _ownedCount++;
        }

        public void Unlock()
        {
            _isUnlocked = true;
        }
    }
}
