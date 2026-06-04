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

        [Header("Shop Income")]
        [SerializeField] private TextMeshProUGUI _totalShopIncomeText;

        [Header("Business Views")]
        [SerializeField] private StoresManager _storesManager;
        [SerializeField] private StoresUIItemView[] _businessViews;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            SubscribeToEvents();
            RegisterBusinessButtonHandlers();
            UpdateAllTexts();
            UpdateUpgradeUI();
            UpdateBusinessUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (_passengerProcessor != null && _ppsText != null)
            {
                _ppsText.text = $"{NumberFormatter.Format(_passengerProcessor.PassengersPerSecond, 1)}/s";
            }

            if (_storesManager != null && _totalShopIncomeText != null)
            {
                _totalShopIncomeText.text = $"{NumberFormatter.Format(_storesManager.TotalIncomePerSecond, 2)}/s";
            }
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

        private void OnStorePurchased(int index, Store store)
        {
            UpdateBusinessUI();
        }

        private void RegisterBusinessButtonHandlers()
        {
            if (_storesManager == null || _businessViews == null) return;

            for (int i = 0; i < _businessViews.Length; i++)
            {
                if (_businessViews[i] == null) continue;

                int index = i;
                _businessViews[i].SetClickHandler(() => _storesManager.TryPurchaseBusiness(index));
            }
        }

        private void SubscribeToEvents()
        {
            if (_economyController == null) return;

            _economyController.OnMoneyChanged += OnMoneyChanged;
            _economyController.OnTotalPassengersProcessedChanged += OnPassengersChanged;
            _economyController.OnMoneyChanged += OnMoneyChangedForUpgrade;

            if (_storesManager != null)
            {
                _storesManager.OnStorePurchased += OnStorePurchased;
                _storesManager.OnBusinessesChanged += UpdateBusinessUI;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_economyController == null) return;

            _economyController.OnMoneyChanged -= OnMoneyChanged;
            _economyController.OnTotalPassengersProcessedChanged -= OnPassengersChanged;
            _economyController.OnMoneyChanged -= OnMoneyChangedForUpgrade;

            if (_storesManager != null)
            {
                _storesManager.OnStorePurchased -= OnStorePurchased;
                _storesManager.OnBusinessesChanged -= UpdateBusinessUI;
            }
        }

        private void OnMoneyChanged(double money)
        {
            if (_moneyText != null)
            {
                _moneyText.text = NumberFormatter.Format(money);
            }
        }

        private void OnMoneyChangedForUpgrade(double money)
        {
            UpdateUpgradeUI();
            UpdateBusinessUI();
        }

        private void OnPassengersChanged(int passengers)
        {
            if (_passengersProcessedText != null)
            {
                _passengersProcessedText.text = $"{NumberFormatter.Format(passengers)}";
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

        private void UpdateBusinessUI()
        {
            if (_storesManager == null || _businessViews == null) return;

            Store[] businesses = _storesManager.Businesses;
            if (businesses == null) return;

            int count = Mathf.Min(businesses.Length, _businessViews.Length);

            for (int i = 0; i < count; i++)
            {
                if (_businessViews[i] == null) continue;

                _businessViews[i].SetData(businesses[i], _storesManager.CanPurchaseBusiness(i));
            }

            if (_businessViews.Length > businesses.Length)
            {
                for (int i = businesses.Length; i < _businessViews.Length; i++)
                {
                    if (_businessViews[i] != null)
                        _businessViews[i].gameObject.SetActive(false);
                }
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
