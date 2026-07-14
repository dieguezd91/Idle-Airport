using System.Collections;
using IdleAirport.GameCore.Prestige;
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

        [Header("AI Upgrade UI")]
        [SerializeField] private AITSAScannerUpgrade _aiTSAScannerUpgrade;
        [SerializeField] private AITSAScannerUpgradeCardView _aiTSAScannerCardView;
        [SerializeField] private AIMaintenanceTokensCardView _aiMaintenanceTokensCardView;
        [SerializeField] private AIDurabilityUpgradeCardView _aiDurabilityUpgradeCardView;
        [SerializeField] private Button _buyAITSButton;
        [SerializeField] private TextMeshProUGUI _aiTSAStatusText;
        [SerializeField] private AIUpgradeFeedbackView _aiUpgradeFeedbackView;

        [Header("Passenger Income UI")]
        [SerializeField] private TextMeshProUGUI _basePassengerIncomeText;
        [SerializeField] private TextMeshProUGUI _shopBonusIncomeText;
        [SerializeField] private TextMeshProUGUI _totalPassengerIncomeText;
        [SerializeField] private TextMeshProUGUI _waitingRoomStatusText;
        [SerializeField] private TextMeshProUGUI _hudFeedbackText;
        [SerializeField] private float _hudFeedbackDuration = 1.5f;
        [SerializeField] private float _hudFeedbackCooldown = 0.4f;

        [Header("Gate UI")]
        [SerializeField] private TextMeshProUGUI _nextFlightCountdownText;

        [Header("Store Views")]
        [SerializeField] private StoresManager _storesManager;
        [SerializeField] private StoresUIItemView[] _storeViews;
        [SerializeField] private bool _debugScanButton;

        private Coroutine _hudFeedbackRoutine;
        private string _lastFeedbackMessage;
        private float _lastFeedbackTime;
        private bool _suppressAIStateTransitionFeedback;
        private AIStateData? _previousAIState;
        private AirportPrestigeHudPresenter _prestigeHudPresenter;

        private TextMeshProUGUI _nextObjectiveText;
        private double _lastMoneyValue = -1.0;
        private Coroutine _moneyPulseRoutine;
        private Vector3 _moneyTextBaseScale = Vector3.one;
        private Coroutine _moneyFailRoutine;
        private Color _moneyTextBaseColor = Color.white;

        private void Awake()
        {
            ValidateReferences();
            _prestigeHudPresenter = FindFirstObjectByType<AirportPrestigeHudPresenter>();
            if (_prestigeHudPresenter == null)
            {
                AirportPrestigeService prestigeService = FindFirstObjectByType<AirportPrestigeService>();
                if (prestigeService != null && _passengersProcessedText != null)
                {
                    _prestigeHudPresenter = gameObject.AddComponent<AirportPrestigeHudPresenter>();
                    _prestigeHudPresenter.Configure(prestigeService, _passengersProcessedText, null, null);
                }
            }
            if (_moneyText != null)
            {
                _moneyTextBaseScale = _moneyText.transform.localScale;
            }
            if (_scannerButton != null)
            {
                _scannerButton.transition = Selectable.Transition.None;
            }
        }

        private void OnEnable()
        {
            CacheInitialAIState();
            BindButtonListeners();
            SubscribeToEvents();
            UpdateAllTexts();
            RefreshPPSText();
            UpdateUpgradeUI();
            UpdateStoreUI();
            UpdateNextObjective();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnbindButtonListeners();
        }

        private void Update()
        {
            if (_scannerButton != null && _passengerProcessor != null)
            {
                _scannerButton.interactable = _passengerProcessor.IsGameplayActive;
            }

            UpdateNextFlightCountdownText();
        }

        public void OnScannerClicked()
        {
            if (_passengerProcessor == null) return;

            bool processed = _passengerProcessor.TryManualScan();
            if (!processed)
            {
                var reason = _passengerProcessor.LastManualScanFailureReason;
                // Show HUD toast only for major blocks (waiting room full, no passenger, missing setup)
                if (reason == PassengerProcessor.ManualScanFailureReason.NoPassengers ||
                    reason == PassengerProcessor.ManualScanFailureReason.NoReservableCapacity ||
                    reason == PassengerProcessor.ManualScanFailureReason.WaitingRoomReceiveFailed ||
                    reason == PassengerProcessor.ManualScanFailureReason.MissingQueue ||
                    reason == PassengerProcessor.ManualScanFailureReason.MissingWaitingRoom)
                {
                    ShowHUDFeedback(GetManualScanFeedbackMessage(reason));
                }
            }

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

            _suppressAIStateTransitionFeedback = true;
            AITSAUpgradePurchaseResult result = _aiTSAScannerUpgrade.Purchase();
            _suppressAIStateTransitionFeedback = false;

            if (result == AITSAUpgradePurchaseResult.SuccessFirstPurchase || result == AITSAUpgradePurchaseResult.SuccessUpgrade)
            {
                if (_aiTSAScannerCardView != null) _aiTSAScannerCardView.PlaySuccess();
                if (_aiUpgradeFeedbackView != null) _aiUpgradeFeedbackView.PlayAIScannerSuccess();
            }
            else
            {
                if (_aiTSAScannerCardView != null) _aiTSAScannerCardView.PlayFail();
                if (result == AITSAUpgradePurchaseResult.InsufficientFunds)
                {
                    PlayMoneyFailFeedback();
                    ShowHUDFeedback("Not enough money");
                }
            }
            UpdateNextObjective();
        }

        public void OnBuyAITokenPackClicked()
        {
            if (_aiTSAScannerUpgrade == null) return;

            _suppressAIStateTransitionFeedback = true;
            AITokenPackPurchaseResult result = _aiTSAScannerUpgrade.PurchaseTokenPack();
            _suppressAIStateTransitionFeedback = false;

            if (result == AITokenPackPurchaseResult.Success)
            {
                if (_aiMaintenanceTokensCardView != null) _aiMaintenanceTokensCardView.PlaySuccess();
                if (_aiUpgradeFeedbackView != null) _aiUpgradeFeedbackView.PlayTokensSuccess();
            }
            else
            {
                if (_aiMaintenanceTokensCardView != null) _aiMaintenanceTokensCardView.PlayFail();
                if (result == AITokenPackPurchaseResult.InsufficientFunds
                    || result == AITokenPackPurchaseResult.TokensFull)
                {
                    if (_aiUpgradeFeedbackView != null) _aiUpgradeFeedbackView.PlayFail("Tokens");
                }

                if (result == AITokenPackPurchaseResult.InsufficientFunds)
                {
                    PlayMoneyFailFeedback();
                    ShowHUDFeedback("Not enough money");
                }
                else if (result == AITokenPackPurchaseResult.TokensFull)
                {
                    ShowHUDFeedback("Tokens full");
                }
            }
            UpdateNextObjective();
        }

        public void OnBuyAIDurabilityClicked()
        {
            if (_aiTSAScannerUpgrade == null) return;

            _suppressAIStateTransitionFeedback = true;
            AIDurabilityPurchaseResult result = _aiTSAScannerUpgrade.PurchaseDurabilityUpgrade();
            _suppressAIStateTransitionFeedback = false;

            if (result == AIDurabilityPurchaseResult.Success)
            {
                if (_aiDurabilityUpgradeCardView != null) _aiDurabilityUpgradeCardView.PlaySuccess();
                if (_aiUpgradeFeedbackView != null) _aiUpgradeFeedbackView.PlayDurabilitySuccess();
            }
            else
            {
                if (_aiDurabilityUpgradeCardView != null) _aiDurabilityUpgradeCardView.PlayFail();
                if (result == AIDurabilityPurchaseResult.InsufficientFunds)
                {
                    if (_aiUpgradeFeedbackView != null) _aiUpgradeFeedbackView.PlayFail("Durability");
                    PlayMoneyFailFeedback();
                    ShowHUDFeedback("Not enough money");
                }
            }
            UpdateNextObjective();
        }

        private void OnStorePurchased(int index, Store store)
        {
            if (_storeViews != null && index >= 0 && index < _storeViews.Length && _storeViews[index] != null)
            {
                _storeViews[index].PlaySuccess();
            }
            UpdateNextObjective();
        }

        private void HandleBusinessesChanged()
        {
            UpdateStoreUI();
            UpdatePassengerIncomeTexts();
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

        private void RegisterAIUpgradeButtonHandlers()
        {
            if (_aiTSAScannerCardView != null)
            {
                _aiTSAScannerCardView.SetClickHandler(OnBuyAITSAScannerClicked);
            }

            if (_buyAITSButton != null)
            {
                _buyAITSButton.onClick.RemoveListener(OnBuyAITSAScannerClicked);
                _buyAITSButton.onClick.AddListener(OnBuyAITSAScannerClicked);
            }

            if (_aiMaintenanceTokensCardView != null)
            {
                _aiMaintenanceTokensCardView.SetClickHandler(OnBuyAITokenPackClicked);
            }

            if (_aiDurabilityUpgradeCardView != null)
            {
                _aiDurabilityUpgradeCardView.SetClickHandler(OnBuyAIDurabilityClicked);
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
                _storesManager.OnBusinessesChanged += HandleBusinessesChanged;
                _storesManager.OnStorePurchaseFailed += OnStorePurchaseFailed;
            }

            if (_aiTSAScannerUpgrade != null)
            {
                _aiTSAScannerUpgrade.OnAIStateChanged += HandleAIStateChanged;
            }

            if (_passengerProcessor != null)
            {
                WaitingRoomUIController waitingRoom = FindWaitingRoom();
                if (waitingRoom != null)
                {
                    waitingRoom.OnOccupancyChanged += OnWaitingRoomOccupancyChanged;
                    waitingRoom.OnPassengersBoarded += OnPassengersBoarded;
                }
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
                _storesManager.OnBusinessesChanged -= HandleBusinessesChanged;
                _storesManager.OnStorePurchaseFailed -= OnStorePurchaseFailed;
            }

            if (_aiTSAScannerUpgrade != null)
            {
                _aiTSAScannerUpgrade.OnAIStateChanged -= HandleAIStateChanged;
            }

            if (_passengerProcessor != null)
            {
                WaitingRoomUIController waitingRoom = FindWaitingRoom();
                if (waitingRoom != null)
                {
                    waitingRoom.OnOccupancyChanged -= OnWaitingRoomOccupancyChanged;
                    waitingRoom.OnPassengersBoarded -= OnPassengersBoarded;
                }
            }
        }

        private void OnMoneyChanged(double money)
        {
            if (_moneyText != null)
            {
                _moneyText.text = NumberFormatter.Format(money);
                if (_lastMoneyValue >= 0.0 && money > _lastMoneyValue)
                {
                    PlayMoneyPulse();
                }
            }
            _lastMoneyValue = money;
            UpdateNextObjective();
        }

        private void OnMoneyChangedForUpgrade(double money)
        {
            UpdateUpgradeUI();
            UpdateStoreUI();
            UpdateNextObjective();
        }

        private void HandleAIStateChanged(AIStateData state)
        {
            UpdateUpgradeUI();
            RefreshPPSText();

            if (_suppressAIStateTransitionFeedback)
            {
                _previousAIState = state;
                return;
            }

            if (_previousAIState.HasValue)
            {
                int prev = _previousAIState.Value.EffectiveScannerCount;
                int curr = state.EffectiveScannerCount;

                if (prev > 0 && curr == 0 && state.OwnedCount > 0)
                    ShowHUDFeedback("AI Scanner out of tokens");
            }

            _previousAIState = state;
        }

        private void CacheInitialAIState()
        {
            if (_aiTSAScannerUpgrade != null)
                _previousAIState = _aiTSAScannerUpgrade.GetState();
        }

        private void OnPassengersChanged(int passengers)
        {
            if (_prestigeHudPresenter != null)
                return;

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
                _basePassengerIncomeText.text = $"Base: ${NumberFormatter.Format(baseIncome, 2)}/pax";

            if (_shopBonusIncomeText != null)
                _shopBonusIncomeText.text = $"Shops: ${NumberFormatter.Format(shopsBonus, 2)}/pax";

            if (_totalPassengerIncomeText != null)
                _totalPassengerIncomeText.text = $"Total: ${NumberFormatter.Format(totalIncome, 2)}/pax";
        }

        private void RefreshPPSText()
        {
            if (_ppsText == null)
                return;

            float pps = _passengerProcessor != null ? _passengerProcessor.CurrentPassengersPerSecond : 0f;
            _ppsText.text = $"AI TSA: {NumberFormatter.Format(pps, 2)} pax/s";
        }

        private void UpdateUpgradeUI()
        {
            if (_aiTSAScannerCardView != null)
                _aiTSAScannerCardView.SetData(_aiTSAScannerUpgrade);

            if (_aiMaintenanceTokensCardView != null)
                _aiMaintenanceTokensCardView.SetData(_aiTSAScannerUpgrade);

            if (_aiDurabilityUpgradeCardView != null)
                _aiDurabilityUpgradeCardView.SetData(_aiTSAScannerUpgrade);

            if (_aiTSAScannerUpgrade != null)
            {
                if (_aiTSAStatusText != null)
                    _aiTSAStatusText.text = BuildAIUpgradeFallbackText(_aiTSAScannerUpgrade);

                if (_buyAITSButton != null)
                    _buyAITSButton.interactable = _aiTSAScannerUpgrade.CanPurchase();
                return;
            }

            if (_aiTSAStatusText != null)
                _aiTSAStatusText.text = "AI Scanner\nLocked";

            if (_buyAITSButton != null)
                _buyAITSButton.interactable = false;
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
        }

        private void OnStorePurchaseFailed(StorePurchaseFailureReason reason)
        {
            if (reason == StorePurchaseFailureReason.InsufficientFunds)
                ShowHUDFeedback("Not enough money for this shop");
        }

        private void OnWaitingRoomOccupancyChanged(int current, int reserved, int capacity)
        {
            if (_waitingRoomStatusText == null)
                return;

            int displayedCurrent = Mathf.Clamp(current + reserved, 0, capacity);
            _waitingRoomStatusText.text = BuildGateStatusText(displayedCurrent, capacity);
        }

        private void OnPassengersBoarded(int count)
        {
            // The BoardingFeedbackView takes care of the local pulse animation. No text toast is displayed.
        }

        private void RefreshWaitingRoomStatus()
        {
            WaitingRoomUIController waitingRoom = FindWaitingRoom();
            if (waitingRoom == null)
            {
                if (_waitingRoomStatusText != null)
                    _waitingRoomStatusText.text = "Gate: 0/0";
                return;
            }

            OnWaitingRoomOccupancyChanged(waitingRoom.CurrentCount, waitingRoom.ReservedCount, waitingRoom.Capacity);
        }

        private void UpdateNextFlightCountdownText()
        {
            if (_nextFlightCountdownText == null)
                return;

            WaitingRoomUIController waitingRoom = FindWaitingRoom();
            if (waitingRoom == null || !waitingRoom.IsFlightBoardingActive)
            {
                _nextFlightCountdownText.text = string.Empty;
                return;
            }

            if (!waitingRoom.HasPassengersInGate)
            {
                _nextFlightCountdownText.text = "Next flight: --";
                return;
            }

            int seconds = Mathf.CeilToInt(waitingRoom.TimeUntilNextBoarding);
            _nextFlightCountdownText.text = $"Next flight: {seconds:00}s";
        }

        private WaitingRoomUIController FindWaitingRoom()
        {
            return _passengerProcessor != null ? _passengerProcessor.WaitingRoom : null;
        }

        private string GetManualScanFeedbackMessage(PassengerProcessor.ManualScanFailureReason reason)
        {
            return reason switch
            {
                PassengerProcessor.ManualScanFailureReason.NoPassengers => "No passenger ready to board",
                PassengerProcessor.ManualScanFailureReason.NoReservableCapacity => "Waiting for next flight",
                PassengerProcessor.ManualScanFailureReason.MissingQueue => "Boarding queue missing",
                PassengerProcessor.ManualScanFailureReason.MissingWaitingRoom => "Boarding area missing",
                PassengerProcessor.ManualScanFailureReason.WaitingRoomReceiveFailed => "Waiting for next flight",
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

        private string GetAITokenPackFeedbackMessage(AITokenPackPurchaseResult result, int previousEffectiveCount)
        {
            return result switch
            {
                AITokenPackPurchaseResult.Success when previousEffectiveCount == 0 && _aiTSAScannerUpgrade != null && _aiTSAScannerUpgrade.EffectiveScannerCount > 0
                    => "AI Scanner back online",
                AITokenPackPurchaseResult.Success => "Maintenance tokens refilled",
                AITokenPackPurchaseResult.TokensFull => "Tokens already full",
                AITokenPackPurchaseResult.InsufficientFunds => "Not enough money for tokens",
                _ => string.Empty
            };
        }

        private string GetAIDurabilityFeedbackMessage(AIDurabilityPurchaseResult result)
        {
            return result switch
            {
                AIDurabilityPurchaseResult.Success => "Scanner durability upgraded",
                AIDurabilityPurchaseResult.InsufficientFunds => "Not enough money for durability",
                _ => string.Empty
            };
        }

        private string BuildAIUpgradeFallbackText(AITSAScannerUpgrade upgrade)
        {
            if (upgrade == null)
                return "AI: OFF";

            if (upgrade.OwnedCount <= 0)
                return "AI: OFF";

            if (!upgrade.HasTokens)
                return "NO TOKENS";

            return $"AI: {upgrade.EffectiveScannerCount}/{upgrade.OwnedCount}";
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

            if (_aiTSAScannerUpgrade == null)
            {
                Debug.LogError("IdleAirportUIController: AITSAScannerUpgrade is not assigned!");
            }

            if (_scannerButton == null)
            {
                Debug.LogError("IdleAirportUIController: ScannerButton is not assigned!");
            }

            if (_storesManager != null && _storeViews == null)
            {
                Debug.LogWarning("IdleAirportUIController: StoreViews array is not assigned.", this);
            }

            if (_aiTSAScannerCardView == null && _buyAITSButton == null)
            {
                Debug.LogWarning("IdleAirportUIController: AI Scanner card view or fallback button is not assigned.", this);
            }

            if (_aiMaintenanceTokensCardView == null)
            {
                Debug.LogWarning("IdleAirportUIController: MaintenanceTokens card view is not assigned.", this);
            }

            if (_aiDurabilityUpgradeCardView == null)
            {
                Debug.LogWarning("IdleAirportUIController: DurabilityUpgrade card view is not assigned.", this);
            }
        }

        private void BindButtonListeners()
        {
            if (_scannerButton != null)
            {
                _scannerButton.onClick.RemoveListener(OnScannerClicked);
                _scannerButton.onClick.AddListener(OnScannerClicked);
            }

            RegisterStoreButtonHandlers();
            RegisterAIUpgradeButtonHandlers();
        }

        private void UnbindButtonListeners()
        {
            if (_scannerButton != null)
                _scannerButton.onClick.RemoveListener(OnScannerClicked);

            if (_buyAITSButton != null)
                _buyAITSButton.onClick.RemoveListener(OnBuyAITSAScannerClicked);

            if (_aiTSAScannerCardView != null)
                _aiTSAScannerCardView.ClearClickHandler();

            if (_aiMaintenanceTokensCardView != null)
                _aiMaintenanceTokensCardView.ClearClickHandler();

            if (_aiDurabilityUpgradeCardView != null)
                _aiDurabilityUpgradeCardView.ClearClickHandler();
        }

        private string BuildGateStatusText(int displayedCurrent, int capacity)
        {
            if (capacity > 0 && displayedCurrent >= capacity)
                return "Boarding";

            return $"Gate: {displayedCurrent}/{capacity}";
        }

        private void CreateNextObjectiveText() {}
        private void UpdateNextObjective() {}

        private void PlayMoneyPulse()
        {
            if (_moneyPulseRoutine != null) StopCoroutine(_moneyPulseRoutine);
            _moneyPulseRoutine = StartCoroutine(MoneyPulseRoutine());
        }

        private IEnumerator MoneyPulseRoutine()
        {
            if (_moneyText == null) yield break;
            float duration = 0.15f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                _moneyText.transform.localScale = _moneyTextBaseScale * Mathf.Lerp(1f, 1.06f, pulse);
                yield return null;
            }
            _moneyText.transform.localScale = _moneyTextBaseScale;
            _moneyPulseRoutine = null;
        }

        private void PlayMoneyFailFeedback()
        {
            if (_moneyText != null)
            {
                if (_moneyFailRoutine != null) StopCoroutine(_moneyFailRoutine);
                _moneyFailRoutine = StartCoroutine(MoneyFailRoutine());
            }
        }

        private IEnumerator MoneyFailRoutine()
        {
            float duration = 0.18f;
            float elapsed = 0f;
            float shakeDistance = 4f;
            Vector3 basePos = _moneyText.transform.localPosition;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float damp = 1f - t;
                float x = Mathf.Sin(t * Mathf.PI * 6f) * shakeDistance * damp;
                _moneyText.transform.localPosition = basePos + new Vector3(x, 0f, 0f);
                yield return null;
            }
            _moneyText.transform.localPosition = basePos;
            _moneyFailRoutine = null;
        }
    }
}
