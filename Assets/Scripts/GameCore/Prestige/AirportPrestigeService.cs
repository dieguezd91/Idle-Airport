using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleAirport.GameCore.Prestige
{
    public sealed class AirportPrestigeService : MonoBehaviour, IPrestigeMultiplierProvider
    {
        [SerializeField] private int _basePassportsRequiredForPrestige = 50;
        [SerializeField] private float _passportRequirementGrowthMultiplier = 1.75f;
        [SerializeField] private int _passportRequirementRoundStep = 10;
        [SerializeField, Min(0f)] private double _requiredAirBucks = 250d;
        [SerializeField] private AirportPrestigeMultiplierConfig _multiplierConfig = new();
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private List<MonoBehaviour> _resettableBehaviours = new();

        private readonly AirportPrestigeData _data = new();
        private bool _lastCanPrestige;
        private bool _isPrestigeInProgress;

        private EconomyController _economyController;
        private AITSAScannerUpgrade _aiScannerUpgrade;
        private StoresManager _storesManager;

        public event Action<int, int> PassportsProgressChanged;
        public event Action<bool> PrestigeAvailabilityChanged;
        public event Action<int, double> PrestigeCompleted;

        public int PrestigeCount => _data.PrestigeCount;
        public int PassportsScannedThisRun => _data.PassportsScannedThisRun;
        public int PassportsRequiredForPrestige => AirportPrestigeRequirementCalculator.CalculateRequiredPassports(
            _basePassportsRequiredForPrestige,
            _passportRequirementGrowthMultiplier,
            _passportRequirementRoundStep,
            PrestigeCount);
        public double GlobalPrestigeMultiplier => Math.Max(1d, _data.GlobalPrestigeMultiplier);
        public double NextPrestigeMultiplier => CalculateMultiplier(PrestigeCount + 1);
        public double RequiredAirBucks => _requiredAirAirBucksCompound();
        private double _requiredAirAirBucksCompound()
        {
            return _requiredAirBucks * Math.Pow(1.5d, PrestigeCount);
        }
        public double CurrentAirBucks => _economyController != null ? _economyController.Money : 0d;

        public bool CanPrestige =>
            _economyController != null &&
            PassportsScannedThisRun >= PassportsRequiredForPrestige &&
            CurrentAirBucks >= RequiredAirBucks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeService()
        {
            if (FindFirstObjectByType<AirportPrestigeService>() != null)
                return;

            if (FindFirstObjectByType<PassengerProcessor>() == null)
                return;

            GameObject serviceObject = new GameObject("AirportPrestigeService");
            serviceObject.AddComponent<AirportPrestigeService>();
        }

        private void Awake()
        {
            AutoWireReferences();
            ValidateRequirementSettings();
            _data.GlobalPrestigeMultiplier = CalculateMultiplier(_data.PrestigeCount);
            _lastCanPrestige = CanPrestige;
        }

        private void OnValidate()
        {
            ValidateRequirementSettings();
            if (_multiplierConfig == null)
                _multiplierConfig = new AirportPrestigeMultiplierConfig();
            _multiplierConfig.Validate();
        }

        private void OnEnable()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();

            if (_passengerProcessor != null)
            {
                _passengerProcessor.OnPassengerProcessed += HandlePassengerProcessed;
            }

            if (_economyController == null)
            {
                ResolveTypedResettables();
            }

            if (_economyController != null)
            {
                _economyController.OnMoneyChanged -= HandleMoneyChanged;
                _economyController.OnMoneyChanged += HandleMoneyChanged;
            }
        }

        private void OnDisable()
        {
            if (_passengerProcessor != null)
            {
                _passengerProcessor.OnPassengerProcessed -= HandlePassengerProcessed;
            }

            if (_economyController != null)
            {
                _economyController.OnMoneyChanged -= HandleMoneyChanged;
            }
        }

        public bool TryPrestige()
        {
            ValidateRequirementSettings();

            if (_isPrestigeInProgress)
                return false;

            if (_economyController == null)
                return false;

            if (!CanPrestige)
                return false;

            _isPrestigeInProgress = true;
            try
            {
                if (!_economyController.TrySpendMoney(RequiredAirBucks))
                {
                    return false;
                }

                _data.PrestigeCount++;
                _data.GlobalPrestigeMultiplier = CalculateMultiplier(_data.PrestigeCount);
                _data.PassportsScannedThisRun = 0;

                ResetRunState();

                PassportsProgressChanged?.Invoke(PassportsScannedThisRun, PassportsRequiredForPrestige);
                
                // Force a final availability update to ensure availability reflects new state
                _lastCanPrestige = !CanPrestige;
                SetPrestigeAvailability(CanPrestige);

                PrestigeCompleted?.Invoke(PrestigeCount, GlobalPrestigeMultiplier);
                return true;
            }
            finally
            {
                _isPrestigeInProgress = false;
            }
        }

        private void ResetRunState()
        {
            _passengerProcessor?.ResetForPrestige();
            _aiScannerUpgrade?.ResetForPrestige();
            _storesManager?.ResetForPrestige();
            _economyController?.ResetForPrestige();
        }

        private void HandlePassengerProcessed(PassengerProcessor.PassengerProcessedData data)
        {
            RegisterPassportScanned();
        }

        private void HandleMoneyChanged(double currentMoney)
        {
            SetPrestigeAvailability(CanPrestige);
        }

        private void RegisterPassportScanned()
        {
            _data.PassportsScannedThisRun++;
            ValidateRequirementSettings();
            PassportsProgressChanged?.Invoke(PassportsScannedThisRun, PassportsRequiredForPrestige);
            SetPrestigeAvailability(CanPrestige);
        }

        private void SetPrestigeAvailability(bool canPrestige)
        {
            if (_lastCanPrestige == canPrestige)
                return;

            _lastCanPrestige = canPrestige;
            PrestigeAvailabilityChanged?.Invoke(canPrestige);
        }

        private void ValidateRequirementSettings()
        {
            _basePassportsRequiredForPrestige = Mathf.Max(1, _basePassportsRequiredForPrestige);
            _passportRequirementGrowthMultiplier = Mathf.Max(1f, _passportRequirementGrowthMultiplier);
            _passportRequirementRoundStep = Mathf.Max(1, _passportRequirementRoundStep);

            if (double.IsNaN(_requiredAirBucks) || double.IsInfinity(_requiredAirBucks) || _requiredAirBucks < 0d)
            {
                _requiredAirBucks = 0d;
            }
        }

        private void AutoWireReferences()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();

            if (_resettableBehaviours == null)
                _resettableBehaviours = new List<MonoBehaviour>();

            ResolveTypedResettables();
        }

        private void ResolveTypedResettables()
        {
            _economyController = ResolveResettable<EconomyController>(_economyController);
            _aiScannerUpgrade = ResolveResettable<AITSAScannerUpgrade>(_aiScannerUpgrade);
            _storesManager = ResolveResettable<StoresManager>(_storesManager);

            if (_passengerProcessor != null && !_resettableBehaviours.Contains(_passengerProcessor))
                _resettableBehaviours.Add(_passengerProcessor);
        }

        private T ResolveResettable<T>(T current) where T : MonoBehaviour, IPrestigeResettable
        {
            if (current != null)
                return current;

            if (_resettableBehaviours != null)
            {
                for (int i = 0; i < _resettableBehaviours.Count; i++)
                {
                    if (_resettableBehaviours[i] is T typed)
                         return typed;
                }
            }

            T found = FindFirstObjectByType<T>();
            if (found != null && _resettableBehaviours != null && !_resettableBehaviours.Contains(found))
                _resettableBehaviours.Add(found);

            return found;
        }

        private double CalculateMultiplier(int prestigeCount)
        {
            if (_multiplierConfig == null)
                _multiplierConfig = new AirportPrestigeMultiplierConfig();

            _multiplierConfig.Validate();
            return _multiplierConfig.CalculateMultiplier(prestigeCount);
        }
    }
}