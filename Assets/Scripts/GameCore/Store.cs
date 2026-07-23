using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    [Serializable]
    public sealed class Store
    {
        [SerializeField] private string _name;
        [SerializeField][TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private double _incomePerPassenger;
        [SerializeField] private double _baseCost;
        [SerializeField] private double _costMultiplier = 1.15;
        [SerializeField] private int _ownedCount;
        [SerializeField] private bool _isUnlocked;

        private bool _initialIsUnlocked;
        private bool _hasCapturedBaseline;

        public string Name => _name;
        public string Description => _description;
        public Sprite Icon => _icon;
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

        public void CapturePrestigeBaseline()
        {
            if (_hasCapturedBaseline)
                return;

            _initialIsUnlocked = _isUnlocked;
            _hasCapturedBaseline = true;
        }

        public void ResetForPrestige()
        {
            CapturePrestigeBaseline();
            _ownedCount = 0;
            _isUnlocked = _initialIsUnlocked;
        }
    }
}