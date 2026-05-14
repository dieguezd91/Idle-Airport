using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class IdleAirportUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyController _economyController;
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private Button _scannerButton;

        [Header("UI Texts")]
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private TextMeshProUGUI _passengersProcessedText;
        [SerializeField] private TextMeshProUGUI _ppsText;

        [Header("Upgrade UI")]
        [SerializeField] private AITSAScannerUpgrade _aiTSAScannerUpgrade;
        [SerializeField] private Button _buyAITSButton;
        [SerializeField] private TextMeshProUGUI _aiTSAStatusText;

        private bool _isInitialized;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            SubscribeToEvents();
            UpdateAllTexts();
            UpdateUpgradeUI();
            _isInitialized = true;
        }

        private void Update()
        {
            if (_passengerProcessor != null && _ppsText != null)
            {
                _ppsText.text = $"Passengers per second: {NumberFormatter.Format(_passengerProcessor.PassengersPerSecond, 1)}/s";
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        public void OnScannerClicked()
        {
            _passengerProcessor.ProcessManualClick();
        }

        public void OnBuyAITSAScannerClicked()
        {
            if (_aiTSAScannerUpgrade == null) return;

            _aiTSAScannerUpgrade.Purchase();
            UpdateUpgradeUI();
        }

        private void SubscribeToEvents()
        {
            if (_economyController == null) return;

            _economyController.OnMoneyChanged += OnMoneyChanged;
            _economyController.OnTotalPassengersProcessedChanged += OnPassengersChanged;
            _economyController.OnMoneyChanged += OnMoneyChangedForUpgrade;
        }

        private void UnsubscribeFromEvents()
        {
            if (_economyController == null) return;

            _economyController.OnMoneyChanged -= OnMoneyChanged;
            _economyController.OnTotalPassengersProcessedChanged -= OnPassengersChanged;
            _economyController.OnMoneyChanged -= OnMoneyChangedForUpgrade;
        }

        private void OnMoneyChangedForUpgrade(int money)
        {
            UpdateUpgradeUI();
        }

        private void OnMoneyChanged(int money)
        {
            if (_moneyText != null)
            {
                _moneyText.text = NumberFormatter.Format(money);
            }
        }

        private void OnPassengersChanged(int passengers)
        {
            if (_passengersProcessedText != null)
            {
                _passengersProcessedText.text = $"Number of passengers processed: {NumberFormatter.Format(passengers)}";
            }
        }

        private void UpdateAllTexts()
        {
            if (_economyController == null) return;

            OnMoneyChanged(_economyController.Money);
            OnPassengersChanged(_economyController.TotalPassengersProcessed);
        }

        private void UpdateUpgradeUI()
        {
            if (_aiTSAScannerUpgrade == null) return;

            if (_aiTSAStatusText != null)
            {
                float cost = _aiTSAScannerUpgrade.CurrentCost;
                int owned = _aiTSAScannerUpgrade.OwnedCount;
                _aiTSAStatusText.text = $"${NumberFormatter.Format(Mathf.RoundToInt(cost))} | Owned: {owned}";
            }

            if (_buyAITSButton != null)
            {
                _buyAITSButton.interactable = _aiTSAScannerUpgrade.CanPurchase();
            }
        }

        private void ValidateReferences()
        {
            bool hasErrors = false;

            if (_economyController == null)
            {
                Debug.LogError("IdleAirportUIController: EconomyController is not assigned!");
                hasErrors = true;
            }

            if (_passengerProcessor == null)
            {
                Debug.LogError("IdleAirportUIController: PassengerProcessor is not assigned!");
                hasErrors = true;
            }

            if (_scannerButton == null)
            {
                Debug.LogError("IdleAirportUIController: ScannerButton is not assigned!");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                _scannerButton.onClick.AddListener(OnScannerClicked);
            }

            if (_buyAITSButton != null && _aiTSAScannerUpgrade != null)
            {
                _buyAITSButton.onClick.AddListener(OnBuyAITSAScannerClicked);
            }
        }
    }
}