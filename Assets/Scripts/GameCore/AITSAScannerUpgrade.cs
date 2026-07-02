using System;
using IdleAirport.GameCore.Prestige;
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

    public enum AITokenPackPurchaseResult
    {
        Success,
        MissingEconomy,
        MissingPassengerProcessor,
        InsufficientFunds,
        TokensFull,
        SpendFailed
    }

    public enum AIDurabilityPurchaseResult
    {
        Success,
        MissingEconomy,
        MissingPassengerProcessor,
        InsufficientFunds,
        SpendFailed
    }

    public enum AITokenPackPurchaseFailureReason
    {
        MissingEconomy,
        MissingPassengerProcessor,
        InsufficientFunds,
        TokensFull,
        SpendFailed
    }

    public enum AIDurabilityPurchaseFailureReason
    {
        MissingEconomy,
        MissingPassengerProcessor,
        InsufficientFunds,
        SpendFailed
    }

    public sealed class AITSAScannerUpgrade : MonoBehaviour, IPrestigeResettable
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
        [SerializeField] private int _tokensPerScanner = 10;
        [SerializeField] private int _currentTokens;
        [SerializeField] private int _tokenPackSize = 10;
        [SerializeField] private float _tokenPackBaseCost = 50f;
        [SerializeField] private float _tokenPackCostMultiplier = 1.15f;
        [SerializeField] private int _tokenPacksPurchased;
        [SerializeField] private int _tokensPerDurabilityUpgrade = 5;
        [SerializeField] private float _durabilityBaseCost = 150f;
        [SerializeField] private float _durabilityCostMultiplier = 1.25f;
        [SerializeField] private int _durabilityUpgradeCount;
        [SerializeField] private int _ownedCount;

        private int _baseTokensPerScanner;

        public event Action<AITSAUpgradePurchaseResult> OnPurchaseAttempted;
        public event Action<int, int> OnAITokensChanged;
        public event Action<int, int> OnAIEffectiveScannerCountChanged;
        public event Action OnAITokensDepleted;
        public event Action OnAITokenPackPurchased;
        public event Action OnAIDurabilityUpgraded;
        public event Action<AITokenPackPurchaseFailureReason> OnAITokenPurchaseFailed;
        public event Action<AIDurabilityPurchaseFailureReason> OnAIDurabilityPurchaseFailed;

        public float CurrentCost => _baseCost * Mathf.Pow(_costMultiplier, OwnedCount);
        public float TokenPackCost => _tokenPackBaseCost * Mathf.Pow(_tokenPackCostMultiplier, _tokenPacksPurchased);
        public float DurabilityUpgradeCost => _durabilityBaseCost * Mathf.Pow(_durabilityCostMultiplier, _durabilityUpgradeCount);
        public int OwnedCount => _ownedCount;
        public int CurrentTokens => _currentTokens;
        public int MaxTokens => Mathf.Max(0, _ownedCount * TokensPerScanner);
        public int DurabilityUpgradeCount => _durabilityUpgradeCount;
        public int TokensPerScanner => Mathf.Max(1, _tokensPerScanner);
        public int TokenPackSize => Mathf.Max(1, _tokenPackSize);
        public int TokensPerDurabilityUpgrade => Mathf.Max(1, _tokensPerDurabilityUpgrade);
        public int EffectiveScannerCount => CalculateEffectiveScannerCount();
        public bool HasTokens => CurrentTokens > 0;
        public bool CanAutoProcess => EffectiveScannerCount > 0;
        public float CurrentProcessingDuration => CanAutoProcess ? GetProcessingDurationForScannerCount(EffectiveScannerCount) : 0f;
        public float CurrentPassengersPerSecond => CurrentProcessingDuration > 0f ? 1f / CurrentProcessingDuration : 0f;
        public float NextProcessingDuration => GetProcessingDurationForScannerCount(_ownedCount + 1);
        public float NextPassengersPerSecond => NextProcessingDuration > 0f ? 1f / NextProcessingDuration : 0f;

        private void Awake()
        {
            _baseTokensPerScanner = _tokensPerScanner;
            ClampState();
        }

        private void Start()
        {
            ApplyCurrentUpgradeState();
        }

        private void OnValidate()
        {
            _baseCost = Mathf.Max(0f, _baseCost);
            _costMultiplier = Mathf.Max(1f, _costMultiplier);
            _baseProcessingDuration = Mathf.Max(0.01f, _baseProcessingDuration);
            _processingDurationMultiplierPerLevel = Mathf.Max(0.01f, _processingDurationMultiplierPerLevel);
            _minimumProcessingDuration = Mathf.Max(0.01f, _minimumProcessingDuration);
            _tokensPerScanner = Mathf.Max(1, _tokensPerScanner);
            _currentTokens = Mathf.Max(0, _currentTokens);
            _tokenPackSize = Mathf.Max(1, _tokenPackSize);
            _tokenPackBaseCost = Mathf.Max(0f, _tokenPackBaseCost);
            _tokenPackCostMultiplier = Mathf.Max(1f, _tokenPackCostMultiplier);
            _tokensPerDurabilityUpgrade = Mathf.Max(1, _tokensPerDurabilityUpgrade);
            _durabilityBaseCost = Mathf.Max(0f, _durabilityBaseCost);
            _durabilityCostMultiplier = Mathf.Max(1f, _durabilityCostMultiplier);
            _ownedCount = Mathf.Max(0, _ownedCount);
            _tokenPacksPurchased = Mathf.Max(0, _tokenPacksPurchased);
            _durabilityUpgradeCount = Mathf.Max(0, _durabilityUpgradeCount);
            ClampState();
        }

        public bool CanPurchase()
        {
            return HasRequiredReferences() && _economyController.Money >= CurrentCost;
        }

        public bool CanPurchaseTokenPack()
        {
            return HasRequiredReferences()
                && MaxTokens > 0
                && CurrentTokens < MaxTokens
                && _economyController.Money >= TokenPackCost;
        }

        public bool CanPurchaseDurabilityUpgrade()
        {
            return HasRequiredReferences() && _economyController.Money >= DurabilityUpgradeCost;
        }

        public AITSAUpgradePurchaseResult Purchase()
        {
            if (!HasRequiredReferences())
            {
                if (_economyController == null)
                {
                    Debug.LogError("AITSAScannerUpgrade: EconomyController is not assigned!");
                    OnPurchaseAttempted?.Invoke(AITSAUpgradePurchaseResult.MissingEconomy);
                    return AITSAUpgradePurchaseResult.MissingEconomy;
                }

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

            if (!SpendMoney(cost))
            {
                OnPurchaseAttempted?.Invoke(AITSAUpgradePurchaseResult.SpendFailed);
                return AITSAUpgradePurchaseResult.SpendFailed;
            }

            int previousTokens = CurrentTokens;
            int previousEffective = EffectiveScannerCount;
            bool wasInactive = _ownedCount == 0;

            _ownedCount++;
            _currentTokens = Mathf.Clamp(_currentTokens + TokensPerScanner, 0, MaxTokens);
            ApplyCurrentUpgradeState();
            NotifyTokenAndScannerChanges(previousTokens, previousEffective);

            AITSAUpgradePurchaseResult result = wasInactive
                ? AITSAUpgradePurchaseResult.SuccessFirstPurchase
                : AITSAUpgradePurchaseResult.SuccessUpgrade;
            OnPurchaseAttempted?.Invoke(result);
            return result;
        }

        public AITokenPackPurchaseResult PurchaseTokenPack()
        {
            if (!HasRequiredReferences())
            {
                if (_economyController == null)
                {
                    OnAITokenPurchaseFailed?.Invoke(AITokenPackPurchaseFailureReason.MissingEconomy);
                    return AITokenPackPurchaseResult.MissingEconomy;
                }

                OnAITokenPurchaseFailed?.Invoke(AITokenPackPurchaseFailureReason.MissingPassengerProcessor);
                return AITokenPackPurchaseResult.MissingPassengerProcessor;
            }

            if (MaxTokens <= 0 || CurrentTokens >= MaxTokens)
            {
                OnAITokenPurchaseFailed?.Invoke(AITokenPackPurchaseFailureReason.TokensFull);
                return AITokenPackPurchaseResult.TokensFull;
            }

            float cost = TokenPackCost;
            if (_economyController.Money < cost)
            {
                OnAITokenPurchaseFailed?.Invoke(AITokenPackPurchaseFailureReason.InsufficientFunds);
                return AITokenPackPurchaseResult.InsufficientFunds;
            }

            if (!SpendMoney(cost))
            {
                OnAITokenPurchaseFailed?.Invoke(AITokenPackPurchaseFailureReason.SpendFailed);
                return AITokenPackPurchaseResult.SpendFailed;
            }

            int previousTokens = CurrentTokens;
            int previousEffective = EffectiveScannerCount;

            _tokenPacksPurchased++;
            _currentTokens = Mathf.Clamp(_currentTokens + TokenPackSize, 0, MaxTokens);
            ApplyCurrentUpgradeState();
            NotifyTokenAndScannerChanges(previousTokens, previousEffective);

            OnAITokenPackPurchased?.Invoke();
            return AITokenPackPurchaseResult.Success;
        }

        public AIDurabilityPurchaseResult PurchaseDurabilityUpgrade()
        {
            if (!HasRequiredReferences())
            {
                if (_economyController == null)
                {
                    OnAIDurabilityPurchaseFailed?.Invoke(AIDurabilityPurchaseFailureReason.MissingEconomy);
                    return AIDurabilityPurchaseResult.MissingEconomy;
                }

                OnAIDurabilityPurchaseFailed?.Invoke(AIDurabilityPurchaseFailureReason.MissingPassengerProcessor);
                return AIDurabilityPurchaseResult.MissingPassengerProcessor;
            }

            float cost = DurabilityUpgradeCost;
            if (_economyController.Money < cost)
            {
                OnAIDurabilityPurchaseFailed?.Invoke(AIDurabilityPurchaseFailureReason.InsufficientFunds);
                return AIDurabilityPurchaseResult.InsufficientFunds;
            }

            if (!SpendMoney(cost))
            {
                OnAIDurabilityPurchaseFailed?.Invoke(AIDurabilityPurchaseFailureReason.SpendFailed);
                return AIDurabilityPurchaseResult.SpendFailed;
            }

            int previousTokens = CurrentTokens;
            int previousEffective = EffectiveScannerCount;

            _durabilityUpgradeCount++;
            _tokensPerScanner += TokensPerDurabilityUpgrade;
            _currentTokens = Mathf.Clamp(_currentTokens + _ownedCount * TokensPerDurabilityUpgrade, 0, MaxTokens);
            ApplyCurrentUpgradeState();
            NotifyTokenAndScannerChanges(previousTokens, previousEffective);

            OnAIDurabilityUpgraded?.Invoke();
            return AIDurabilityPurchaseResult.Success;
        }

        public bool TryConsumeTokenAfterAutoScan()
        {
            if (!CanAutoProcess || _currentTokens <= 0)
                return false;

            int previousTokens = CurrentTokens;
            int previousEffective = EffectiveScannerCount;

            _currentTokens = Mathf.Clamp(_currentTokens - 1, 0, MaxTokens);
            ApplyCurrentUpgradeState();
            NotifyTokenAndScannerChanges(previousTokens, previousEffective);
            return true;
        }

        public void ResetForPrestige()
        {
            int previousTokens = CurrentTokens;
            int previousEffective = EffectiveScannerCount;

            _ownedCount = 0;
            _currentTokens = 0;
            _tokenPacksPurchased = 0;
            _durabilityUpgradeCount = 0;
            _tokensPerScanner = Mathf.Max(1, _baseTokensPerScanner);

            if (_passengerProcessor != null)
                _passengerProcessor.DisableAIScanner();

            NotifyTokenAndScannerChanges(previousTokens, previousEffective);
            OnPurchaseAttempted?.Invoke(AITSAUpgradePurchaseResult.SuccessFirstPurchase);
        }

        private void ApplyCurrentUpgradeState()
        {
            if (_passengerProcessor == null || _ownedCount <= 0)
                return;

            float duration = CurrentProcessingDuration;
            if (duration > 0f)
            {
                _passengerProcessor.EnableAIScanner();
                _passengerProcessor.SetAIAutoProcessingDuration(duration);
            }
        }

        private void NotifyTokenAndScannerChanges(int previousTokens, int previousEffective)
        {
            int currentTokens = CurrentTokens;
            int currentEffective = EffectiveScannerCount;

            if (previousTokens != currentTokens)
                OnAITokensChanged?.Invoke(previousTokens, currentTokens);

            if (previousEffective != currentEffective)
                OnAIEffectiveScannerCountChanged?.Invoke(previousEffective, currentEffective);

            if (previousTokens > 0 && currentTokens == 0)
                OnAITokensDepleted?.Invoke();
        }

        private bool HasRequiredReferences()
        {
            return _economyController != null && _passengerProcessor != null;
        }

        private void ClampState()
        {
            _currentTokens = Mathf.Clamp(_currentTokens, 0, MaxTokens);
        }

        private int CalculateEffectiveScannerCount()
        {
            if (_ownedCount <= 0 || _currentTokens <= 0)
                return 0;

            int effectiveCount = Mathf.CeilToInt(_currentTokens / (float)TokensPerScanner);
            return Mathf.Clamp(effectiveCount, 0, _ownedCount);
        }

        private float GetProcessingDurationForScannerCount(int scannerCount)
        {
            if (scannerCount <= 0)
                return 0f;

            int effectiveLevel = Mathf.Max(scannerCount - 1, 0);
            float duration = _baseProcessingDuration * Mathf.Pow(_processingDurationMultiplierPerLevel, effectiveLevel);
            return Mathf.Max(_minimumProcessingDuration, duration);
        }

        private bool SpendMoney(float cost)
        {
            return _economyController != null && _economyController.SpendMoney(Mathf.RoundToInt(cost));
        }
    }
}