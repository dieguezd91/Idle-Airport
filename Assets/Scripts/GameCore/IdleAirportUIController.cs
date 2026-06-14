using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

        [Header("Passenger Income UI")]
        [SerializeField] private TextMeshProUGUI _basePassengerIncomeText;
        [SerializeField] private TextMeshProUGUI _shopBonusIncomeText;
        [SerializeField] private TextMeshProUGUI _totalPassengerIncomeText;
        [SerializeField] private TextMeshProUGUI _waitingRoomStatusText;
        [SerializeField] private TextMeshProUGUI _hudFeedbackText;
        [SerializeField] private float _hudFeedbackDuration = 1.5f;
        [SerializeField] private float _hudFeedbackCooldown = 0.4f;

        [Header("Store Views")]
        [SerializeField] private StoresManager _storesManager;
        [SerializeField] private StoresUIItemView[] _storeViews;
        [SerializeField] private bool _debugScanButton;

        private Coroutine _hudFeedbackRoutine;
        private string _lastFeedbackMessage;
        private float _lastFeedbackTime;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            BindButtonListeners();
        }

        private void Start()
        {
            SubscribeToEvents();
            RegisterStoreButtonHandlers();
            UpdateAllTexts();
            UpdateUpgradeUI();
            UpdateStoreUI();
        }

        private void OnDestroy()
        {
            UnbindButtonListeners();
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (_passengerProcessor != null && _ppsText != null)
            {
                _ppsText.text = $"AI TSA: {NumberFormatter.Format(_passengerProcessor.CurrentPassengersPerSecond, 2)} pax/s";
            }

            if (_scannerButton != null && _passengerProcessor != null)
            {
                _scannerButton.interactable = _passengerProcessor.IsGameplayActive;
            }

            UpdatePassengerIncomeTexts();
        }

        public void OnScannerClicked()
        {
            if (_passengerProcessor == null) return;

            bool processed = _passengerProcessor.TryManualScan();
            if (!processed)
                ShowHUDFeedback(GetManualScanFeedbackMessage(_passengerProcessor.LastManualScanFailureReason));

            if (!_debugScanButton) return;

            if (processed)
            {
                Debug.Log("Scan button clicked. Manual scan processed a passenger.", this);
                return;
            }

            Debug.Log(
                $"Scan button clicked. Manual scan failed: {_passengerProcessor.GetLastManualScanFailureReason()} | {_passengerProcessor.GetManualScanBlockReason()}",
                this);
        }

        public void OnBuyAITSAScannerClicked()
        {
            if (_aiTSAScannerUpgrade == null) return;

            AITSAUpgradePurchaseResult result = _aiTSAScannerUpgrade.Purchase();
            ShowHUDFeedback(GetAIFeedbackMessage(result));
            UpdateUpgradeUI();
        }

        private void OnStorePurchased(int index, Store store)
        {
            UpdateStoreUI();
            string message = store.OwnedCount <= 1
                ? $"Bought {store.Name}"
                : $"{store.Name} Level {store.OwnedCount}";
            ShowHUDFeedback(message);
        }

        private void RegisterStoreButtonHandlers()
        {
            if (_storesManager == null || _storeViews == null) return;

            for (int i = 0; i < _storeViews.Length; i++)
            {
                if (_storeViews[i] == null) continue;

                int index = i;
                _storeViews[i].SetClickHandler(() => _storesManager.TryPurchaseStore(index));
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
                _storesManager.OnBusinessesChanged += UpdateStoreUI;
                _storesManager.OnStorePurchaseFailed += OnStorePurchaseFailed;
            }

            if (_waitingRoomStatusText != null && _passengerProcessor != null)
            {
                WaitingRoomUIController waitingRoom = FindWaitingRoom();
                if (waitingRoom != null)
                    waitingRoom.OnOccupancyChanged += OnWaitingRoomOccupancyChanged;
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
                _storesManager.OnBusinessesChanged -= UpdateStoreUI;
                _storesManager.OnStorePurchaseFailed -= OnStorePurchaseFailed;
            }

            WaitingRoomUIController waitingRoom = FindWaitingRoom();
            if (waitingRoom != null)
            {
                waitingRoom.OnOccupancyChanged -= OnWaitingRoomOccupancyChanged;
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
            UpdateStoreUI();
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
            UpdatePassengerIncomeTexts();
            RefreshWaitingRoomStatus();
        }

        private void UpdatePassengerIncomeTexts()
        {
            double baseIncome = _economyController != null ? _economyController.GetBasePassengerIncome() : 0.0;
            double shopsBonus = _storesManager != null ? _storesManager.GetPassengerIncomeBonus() : 0.0;
            double totalIncome = _passengerProcessor != null
                ? _passengerProcessor.GetTotalPassengerReward()
                : baseIncome + shopsBonus;

            if (_basePassengerIncomeText != null)
                _basePassengerIncomeText.text = $"Base: +${NumberFormatter.Format(baseIncome, 2)}/passenger";

            if (_shopBonusIncomeText != null)
                _shopBonusIncomeText.text = $"Shops: +${NumberFormatter.Format(shopsBonus, 2)}/passenger";

            if (_totalPassengerIncomeText != null)
                _totalPassengerIncomeText.text = $"Total: +${NumberFormatter.Format(totalIncome, 2)}/passenger";
        }

        private void UpdateUpgradeUI()
        {
            if (_aiTSAScannerUpgrade == null) return;

            if (_aiTSAStatusText != null)
            {
                float cost = _aiTSAScannerUpgrade.CurrentCost;
                int owned = _aiTSAScannerUpgrade.OwnedCount;
                float currentPps = _aiTSAScannerUpgrade.CurrentPassengersPerSecond;
                float nextPps = _aiTSAScannerUpgrade.NextPassengersPerSecond;
                string statusLabel = owned > 0 ? $"Level {owned}" : "Locked";
                _aiTSAStatusText.text =
                    owned > 0
                        ? $"AI TSA {statusLabel} | Current: {NumberFormatter.Format(currentPps, 2)} pax/s | Next: {NumberFormatter.Format(nextPps, 2)} pax/s | Upgrade: ${NumberFormatter.Format(Mathf.RoundToInt(cost))}"
                        : $"AI TSA Locked | Activates at {NumberFormatter.Format(nextPps, 2)} pax/s | Cost: ${NumberFormatter.Format(Mathf.RoundToInt(cost))}";
            }

            if (_buyAITSButton != null)
            {
                _buyAITSButton.interactable = _aiTSAScannerUpgrade.CanPurchase();
            }
        }

        private void UpdateStoreUI()
        {
            if (_storesManager == null || _storeViews == null) return;

            Store[] stores = _storesManager.Stores;
            if (stores == null) return;

            int count = Mathf.Min(stores.Length, _storeViews.Length);

            for (int i = 0; i < count; i++)
            {
                if (_storeViews[i] == null) continue;

                _storeViews[i].SetData(stores[i], _storesManager.CanPurchaseStore(i));
            }

            if (_storeViews.Length > stores.Length)
            {
                for (int i = stores.Length; i < _storeViews.Length; i++)
                {
                    if (_storeViews[i] != null)
                        _storeViews[i].gameObject.SetActive(false);
                }
            }

            UpdatePassengerIncomeTexts();
        }

        private void OnStorePurchaseFailed(StorePurchaseFailureReason reason)
        {
            if (reason == StorePurchaseFailureReason.InsufficientFunds)
                ShowHUDFeedback("Not enough money for this shop");
        }

        private void OnWaitingRoomOccupancyChanged(int current, int reserved, int capacity)
        {
            if (_waitingRoomStatusText == null) return;

            int displayedCurrent = Mathf.Clamp(current + reserved, 0, capacity);
            string status = displayedCurrent >= capacity ? " | Waiting Room Full" : string.Empty;
            _waitingRoomStatusText.text = $"Waiting: {displayedCurrent}/{capacity}{status}";
        }

        private void RefreshWaitingRoomStatus()
        {
            WaitingRoomUIController waitingRoom = FindWaitingRoom();
            if (waitingRoom == null)
            {
                if (_waitingRoomStatusText != null)
                    _waitingRoomStatusText.text = "Waiting: 0/0";
                return;
            }

            OnWaitingRoomOccupancyChanged(waitingRoom.CurrentCount, waitingRoom.ReservedCount, waitingRoom.Capacity);
        }

        private WaitingRoomUIController FindWaitingRoom()
        {
            return _passengerProcessor != null ? _passengerProcessor.WaitingRoom : null;
        }

        private string GetManualScanFeedbackMessage(PassengerProcessor.ManualScanFailureReason reason)
        {
            return reason switch
            {
                PassengerProcessor.ManualScanFailureReason.NoPassengers => "No passenger ready to scan",
                PassengerProcessor.ManualScanFailureReason.NoReservableCapacity => "Waiting Room Full",
                PassengerProcessor.ManualScanFailureReason.MissingQueue => "Passenger queue missing",
                PassengerProcessor.ManualScanFailureReason.MissingWaitingRoom => "Waiting room missing",
                PassengerProcessor.ManualScanFailureReason.WaitingRoomReceiveFailed => "Waiting Room Full",
                _ => "Cannot scan right now"
            };
        }

        private string GetAIFeedbackMessage(AITSAUpgradePurchaseResult result)
        {
            return result switch
            {
                AITSAUpgradePurchaseResult.SuccessFirstPurchase => "AI TSA activated",
                AITSAUpgradePurchaseResult.SuccessUpgrade => "AI TSA upgraded",
                AITSAUpgradePurchaseResult.InsufficientFunds => "Not enough money for AI TSA",
                _ => string.Empty
            };
        }

        private void ShowHUDFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            float now = Time.unscaledTime;
            if (message == _lastFeedbackMessage && now - _lastFeedbackTime < _hudFeedbackCooldown)
                return;

            _lastFeedbackMessage = message;
            _lastFeedbackTime = now;

            if (_hudFeedbackText == null)
            {
                Debug.Log(message, this);
                return;
            }

            if (_hudFeedbackRoutine != null)
                StopCoroutine(_hudFeedbackRoutine);

            _hudFeedbackRoutine = StartCoroutine(ShowHUDFeedbackRoutine(message));
        }

        private IEnumerator ShowHUDFeedbackRoutine(string message)
        {
            _hudFeedbackText.gameObject.SetActive(true);
            _hudFeedbackText.text = message;
            yield return new WaitForSecondsRealtime(_hudFeedbackDuration);
            _hudFeedbackText.text = string.Empty;
            _hudFeedbackText.gameObject.SetActive(false);
            _hudFeedbackRoutine = null;
        }

        private void ValidateReferences()
        {
            if (_economyController == null)
            {
                Debug.LogError("IdleAirportUIController: EconomyController is not assigned!");
            }

            if (_passengerProcessor == null)
            {
                Debug.LogError("IdleAirportUIController: PassengerProcessor is not assigned!");
            }

            if (_scannerButton == null)
            {
                Debug.LogError("IdleAirportUIController: ScannerButton is not assigned!");
            }

            if (_buyAITSButton == null || _aiTSAScannerUpgrade == null) return;

            _buyAITSButton.onClick.RemoveListener(OnBuyAITSAScannerClicked);
            _buyAITSButton.onClick.AddListener(OnBuyAITSAScannerClicked);
        }

        private void BindButtonListeners()
        {
            if (_scannerButton != null)
            {
                _scannerButton.onClick.RemoveListener(OnScannerClicked);
                _scannerButton.onClick.AddListener(OnScannerClicked);
            }

            if (_buyAITSButton != null && _aiTSAScannerUpgrade != null)
            {
                _buyAITSButton.onClick.RemoveListener(OnBuyAITSAScannerClicked);
                _buyAITSButton.onClick.AddListener(OnBuyAITSAScannerClicked);
            }
        }

        private void UnbindButtonListeners()
        {
            if (_scannerButton != null)
                _scannerButton.onClick.RemoveListener(OnScannerClicked);

            if (_buyAITSButton != null)
                _buyAITSButton.onClick.RemoveListener(OnBuyAITSAScannerClicked);
        }
    }
}
