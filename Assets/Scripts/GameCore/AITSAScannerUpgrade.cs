using UnityEngine;

namespace IdleAirport.GameCore
{
    public enum AITSAUpgradePurchaseResult
    {
        SuccessFirstPurchase,
        SuccessUpgrade,
        MissingEconomy,
        MissingPassengerProcessor,
        InsufficientFunds,
        SpendFailed
    }

    public sealed class AITSAScannerUpgrade : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private PassengerProcessor _passengerProcessor;

        [Header("Settings")]
        [SerializeField] private float _baseCost = 25f;
        [SerializeField] private float _costMultiplier = 1.15f;
        [SerializeField] private float _baseProcessingDuration = 1.5f;
        [SerializeField] private float _processingDurationMultiplierPerLevel = 0.9f;
        [SerializeField] private float _minimumProcessingDuration = 0.35f;

        public event System.Action<AITSAUpgradePurchaseResult> OnPurchaseAttempted;

        public float CurrentCost => _baseCost * Mathf.Pow(_costMultiplier, OwnedCount);
        public int OwnedCount => _ownedCount;
        public float CurrentProcessingDuration => _ownedCount > 0 ? GetProcessingDurationForOwnedCount(_ownedCount) : 0f;
        public float CurrentPassengersPerSecond => CurrentProcessingDuration > 0f ? 1f / CurrentProcessingDuration : 0f;
        public float NextProcessingDuration => GetProcessingDurationForOwnedCount(_ownedCount + 1);
        public float NextPassengersPerSecond => 1f / NextProcessingDuration;

        [SerializeField] private int _ownedCount;

        private void Start()
        {
            ApplyCurrentUpgradeState();
        }

        public bool CanPurchase()
        {
            if (_economyController == null) return false;
            return _economyController.Money >= CurrentCost;
        }

        public AITSAUpgradePurchaseResult Purchase()
        {
            if (_economyController == null)
            {
                Debug.LogError("AITSAScannerUpgrade: EconomyController is not assigned!");
                OnPurchaseAttempted?.Invoke(AITSAUpgradePurchaseResult.MissingEconomy);
                return AITSAUpgradePurchaseResult.MissingEconomy;
            }

            if (_passengerProcessor == null)
            {
                Debug.LogError("AITSAScannerUpgrade: PassengerProcessor is not assigned!");
                OnPurchaseAttempted?.Invoke(AITSAUpgradePurchaseResult.MissingPassengerProcessor);
                return AITSAUpgradePurchaseResult.MissingPassengerProcessor;
            }

            float cost = CurrentCost;
            if (_economyController.Money < cost)
            {
                OnPurchaseAttempted?.Invoke(AITSAUpgradePurchaseResult.InsufficientFunds);
                return AITSAUpgradePurchaseResult.InsufficientFunds;
            }

            if (!_economyController.SpendMoney(Mathf.RoundToInt(cost)))
            {
                OnPurchaseAttempted?.Invoke(AITSAUpgradePurchaseResult.SpendFailed);
                return AITSAUpgradePurchaseResult.SpendFailed;
            }

            bool wasInactive = _ownedCount == 0;
            _ownedCount++;
            ApplyCurrentUpgradeState();
            AITSAUpgradePurchaseResult result = wasInactive
                ? AITSAUpgradePurchaseResult.SuccessFirstPurchase
                : AITSAUpgradePurchaseResult.SuccessUpgrade;
            OnPurchaseAttempted?.Invoke(result);
            return result;
        }

        private void ApplyCurrentUpgradeState()
        {
            if (_passengerProcessor == null) return;

            if (_ownedCount > 0)
            {
                _passengerProcessor.EnableAIScanner();
                _passengerProcessor.SetAIAutoProcessingDuration(CurrentProcessingDuration);
            }

        }

        private float GetProcessingDurationForOwnedCount(int ownedCount)
        {
            int effectiveLevel = Mathf.Max(ownedCount - 1, 0);
            float duration = _baseProcessingDuration * Mathf.Pow(_processingDurationMultiplierPerLevel, effectiveLevel);
            return Mathf.Max(_minimumProcessingDuration, duration);
        }
    }
}
