using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class AITSAScannerUpgrade : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private PassengerProcessor _passengerProcessor;

        [Header("Settings")]
        [SerializeField] private float _baseCost = 25f;
        [SerializeField] private float _costMultiplier = 1.15f;
        [SerializeField] private float _effectPerPurchase = 0.5f;

        public float CurrentCost => _baseCost * Mathf.Pow(_costMultiplier, OwnedCount);
        public int OwnedCount => _ownedCount;

        [SerializeField] private int _ownedCount;

        public bool CanPurchase()
        {
            if (_economyController == null) return false;
            return _economyController.Money >= CurrentCost;
        }

        public void Purchase()
        {
            if (_economyController == null)
            {
                Debug.LogError("AITSAScannerUpgrade: EconomyController is not assigned!");
                return;
            }

            if (_passengerProcessor == null)
            {
                Debug.LogError("AITSAScannerUpgrade: PassengerProcessor is not assigned!");
                return;
            }

            float cost = CurrentCost;
            if (!_economyController.SpendMoney(Mathf.RoundToInt(cost))) return;

            _ownedCount++;
            _passengerProcessor.EnableAIScanner();
            _passengerProcessor.AddPassengersPerSecond(_effectPerPurchase);
        }
    }
}