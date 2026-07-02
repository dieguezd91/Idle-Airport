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
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private AirportPrestigeVisualApplier _visualApplier;
        [SerializeField] private List<MonoBehaviour> _resettableBehaviours = new();

        private readonly AirportPrestigeData _data = new();
        private bool _lastCanPrestige;

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
        public bool CanPrestige => PassportsScannedThisRun >= PassportsRequiredForPrestige;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeService()
        {
            if (FindFirstObjectByType<AirportPrestigeService>() != null)
                return;

            if (FindFirstObjectByType<PassengerProcessor>() == null)
                return;

            GameObject serviceObject = new GameObject("AirportPrestigeService");
            serviceObject.AddComponent<AirportPrestigeVisualApplier>();
            serviceObject.AddComponent<AirportPrestigeService>();
        }

        private void Awake()
        {
            AutoWireReferences();
            ValidateRequirementSettings();
            _data.GlobalPrestigeMultiplier = CalculateMultiplier(_data.PrestigeCount);
            _lastCanPrestige = CanPrestige;
            _visualApplier?.ApplyPrestigeVisuals(_data.PrestigeCount);
        }

        private void OnValidate()
        {
            ValidateRequirementSettings();
        }

        private void OnEnable()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();

            if (_passengerProcessor == null)
                return;

            _passengerProcessor.OnPassengerManuallyProcessed += HandlePassengerProcessed;
            _passengerProcessor.OnPassengerAutoProcessed += HandlePassengerProcessed;
        }

        private void OnDisable()
        {
            if (_passengerProcessor == null)
                return;

            _passengerProcessor.OnPassengerManuallyProcessed -= HandlePassengerProcessed;
            _passengerProcessor.OnPassengerAutoProcessed -= HandlePassengerProcessed;
        }

        public bool TryPrestige()
        {
            ValidateRequirementSettings();

            if (!CanPrestige)
                return false;

            _data.PrestigeCount++;
            _data.GlobalPrestigeMultiplier = CalculateMultiplier(_data.PrestigeCount);
            _data.PassportsScannedThisRun = 0;

            _visualApplier?.ApplyPrestigeVisuals(_data.PrestigeCount);
            ResetRunState();

            PassportsProgressChanged?.Invoke(PassportsScannedThisRun, PassportsRequiredForPrestige);
            SetPrestigeAvailability(false);
            PrestigeCompleted?.Invoke(PrestigeCount, GlobalPrestigeMultiplier);
            return true;
        }

        private void HandlePassengerProcessed(PassengerProcessor.PassengerProcessFeedbackData data)
        {
            RegisterPassportScanned();
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
        }

        private void AutoWireReferences()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();

            if (_visualApplier == null)
                _visualApplier = GetComponent<AirportPrestigeVisualApplier>() ?? FindFirstObjectByType<AirportPrestigeVisualApplier>();

            if (_resettableBehaviours == null)
                _resettableBehaviours = new List<MonoBehaviour>();

            if (_resettableBehaviours.Count > 0)
                return;

            AddResettableIfFound(FindFirstObjectByType<EconomyController>());
            AddResettableIfFound(FindFirstObjectByType<AITSAScannerUpgrade>());
            AddResettableIfFound(FindFirstObjectByType<StoresManager>());
            AddResettableIfFound(_passengerProcessor);
        }

        private void AddResettableIfFound(MonoBehaviour behaviour)
        {
            if (behaviour == null || behaviour is not IPrestigeResettable)
                return;

            if (!_resettableBehaviours.Contains(behaviour))
                _resettableBehaviours.Add(behaviour);
        }

        private void ResetRunState()
        {
            if (_resettableBehaviours == null)
                return;

            for (int i = 0; i < _resettableBehaviours.Count; i++)
            {
                MonoBehaviour behaviour = _resettableBehaviours[i];
                if (behaviour == null)
                    continue;

                if (behaviour is IPrestigeResettable resettable)
                {
                    resettable.ResetForPrestige();
                }
                else
                {
                    Debug.LogWarning($"{behaviour.name} does not implement IPrestigeResettable.", behaviour);
                }
            }
        }

        private static double CalculateMultiplier(int prestigeCount)
        {
            return Math.Pow(2d, Math.Max(0, prestigeCount));
        }
    }
}