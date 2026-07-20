using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace IdleAirport.GameCore.Prestige
{
    public sealed class AirportPrestigeHudPresenter : MonoBehaviour
    {
        [SerializeField] private AirportPrestigeService _prestigeService;
        [SerializeField] private TMP_Text _passportProgressText;
        [SerializeField] private Image _passportIcon;
        [SerializeField] private Button _prestigeButton;
        [SerializeField] private Image _prestigeButtonFillImage;
        [SerializeField] private TMP_Text _prestigeLevelText;
        [SerializeField] private string _defaultButtonText = "PRESTIGE";

        [Header("Tooltip Settings")]
        [SerializeField] private GameObject _tooltipRoot;
        [SerializeField] private TMP_Text _tooltipText;

        private EconomyController _economyController;
        private TooltipTrigger _tooltipTrigger;
        private bool _isHovered;

        private void Awake()
        {
            if (_prestigeService == null)
                _prestigeService = FindFirstObjectByType<AirportPrestigeService>();

            if (_passportProgressText == null && name == "PassengerCounterText")
                _passportProgressText = GetComponent<TMP_Text>();

            _economyController = FindFirstObjectByType<EconomyController>();
        }

        private void OnEnable()
        {
            if (_economyController == null)
                _economyController = FindFirstObjectByType<EconomyController>();

            BindButton();
            SubscribeToService();
            SubscribeToEconomy();
            SetupTooltipTrigger();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindButton();
            UnsubscribeFromService();
            UnsubscribeFromEconomy();
            CleanupTooltipTrigger();
        }

        public void Configure(AirportPrestigeService prestigeService, TMP_Text passportProgressText, Image passportIcon, Button prestigeButton)
        {
            UnbindButton();
            UnsubscribeFromService();
            UnsubscribeFromEconomy();
            CleanupTooltipTrigger();

            _prestigeService = prestigeService;
            _passportProgressText = passportProgressText;
            _passportIcon = passportIcon;
            _prestigeButton = prestigeButton;

            if (isActiveAndEnabled)
            {
                BindButton();
                SubscribeToService();
                SubscribeToEconomy();
                SetupTooltipTrigger();
            }

            Refresh();
        }

        private void SetupTooltipTrigger()
        {
            if (_prestigeButton == null)
                return;

            _tooltipTrigger = _prestigeButton.gameObject.GetComponent<TooltipTrigger>();
            if (_tooltipTrigger == null)
                _tooltipTrigger = _prestigeButton.gameObject.AddComponent<TooltipTrigger>();

            _tooltipTrigger.OnEnter = HandlePointerEnter;
            _tooltipTrigger.OnExit = HandlePointerExit;
        }

        private void CleanupTooltipTrigger()
        {
            if (_tooltipTrigger != null)
            {
                _tooltipTrigger.OnEnter = null;
                _tooltipTrigger.OnExit = null;
            }
            _isHovered = false;
            if (_tooltipRoot != null)
                _tooltipRoot.SetActive(false);
        }

        private void HandlePointerEnter()
        {
            _isHovered = true;
            UpdateTooltipText();
            if (_tooltipRoot != null)
                _tooltipRoot.SetActive(true);
            Refresh();
        }

        private void HandlePointerExit()
        {
            _isHovered = false;
            if (_tooltipRoot != null)
                _tooltipRoot.SetActive(false);
            Refresh();
        }

        private void UpdateTooltipText()
        {
            if (_tooltipText == null || _prestigeService == null)
                return;

            double current = _prestigeService.CurrentAirBucks;
            double required = _prestigeService.RequiredAirBucks;
            _tooltipText.text = $"${NumberFormatter.Format(current)}/${NumberFormatter.Format(required)}";
        }

        private void BindButton()
        {
            if (_prestigeButton == null)
                return;

            _prestigeButton.onClick.RemoveListener(HandlePrestigeClicked);
            _prestigeButton.onClick.AddListener(HandlePrestigeClicked);
        }

        private void UnbindButton()
        {
            if (_prestigeButton != null)
                _prestigeButton.onClick.RemoveListener(HandlePrestigeClicked);
        }

        private void SubscribeToService()
        {
            if (_prestigeService == null)
                return;

            _prestigeService.PassportsProgressChanged -= HandlePassportsProgressChanged;
            _prestigeService.PrestigeAvailabilityChanged -= HandlePrestigeAvailabilityChanged;
            _prestigeService.PrestigeCompleted -= HandlePrestigeCompleted;
            _prestigeService.PassportsProgressChanged += HandlePassportsProgressChanged;
            _prestigeService.PrestigeAvailabilityChanged += HandlePrestigeAvailabilityChanged;
            _prestigeService.PrestigeCompleted += HandlePrestigeCompleted;
        }

        private void UnsubscribeFromService()
        {
            if (_prestigeService == null)
                return;

            _prestigeService.PassportsProgressChanged -= HandlePassportsProgressChanged;
            _prestigeService.PrestigeAvailabilityChanged -= HandlePrestigeAvailabilityChanged;
            _prestigeService.PrestigeCompleted -= HandlePrestigeCompleted;
        }

        private void SubscribeToEconomy()
        {
            if (_economyController == null)
                return;

            _economyController.OnMoneyChanged -= HandleMoneyChanged;
            _economyController.OnMoneyChanged += HandleMoneyChanged;
        }

        private void UnsubscribeFromEconomy()
        {
            if (_economyController == null)
                return;

            _economyController.OnMoneyChanged -= HandleMoneyChanged;
        }

        private void HandleMoneyChanged(double currentMoney)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_prestigeService == null)
            {
                SetProgressText(0, 0);
                SetButtonState(false);
                return;
            }

            SetProgressText(
                _prestigeService.PassportsScannedThisRun,
                _prestigeService.PassportsRequiredForPrestige);
            SetButtonState(_prestigeService.CanPrestige);
            SetPrestigeLevel();
            UpdateButtonFill();

            if (_isHovered)
            {
                UpdateTooltipText();
            }

            if (_passportIcon != null)
                _passportIcon.enabled = _passportIcon.sprite != null;
        }

        private void UpdateButtonFill()
        {
            if (_prestigeButtonFillImage == null || _prestigeService == null)
                return;

            _prestigeButtonFillImage.type = Image.Type.Filled;
            _prestigeButtonFillImage.fillMethod = Image.FillMethod.Horizontal;
            
            double current = _prestigeService.CurrentAirBucks;
            double required = _prestigeService.RequiredAirBucks;
            double ratio = required > 0d ? current / required : 1d;
            _prestigeButtonFillImage.fillAmount = Mathf.Clamp01((float)ratio);
        }

        private void HandlePassportsProgressChanged(int current, int required)
        {
            Refresh();
        }

        private void HandlePrestigeAvailabilityChanged(bool canPrestige)
        {
            SetButtonState(canPrestige);
        }

        private void HandlePrestigeCompleted(int prestigeCount, double globalMultiplier)
        {
            SetPrestigeLevel();
        }

        private void HandlePrestigeClicked()
        {
            if (_prestigeService == null)
                return;

            _prestigeService.TryPrestige();
            Refresh();
        }

        private void SetProgressText(int current, int required)
        {
            if (_passportProgressText != null)
            {
                if (_isHovered)
                {
                    _passportProgressText.text = $"{NumberFormatter.Format(current)} / {NumberFormatter.Format(required)}";
                }
                else
                {
                    _passportProgressText.text = _defaultButtonText;
                }
            }
        }

        private void SetButtonState(bool canPrestige)
        {
            if (_prestigeButton != null)
            {
                _prestigeButton.interactable = _prestigeService != null ? _prestigeService.CanPrestige : false;
            }
        }

        private void SetPrestigeLevel()
        {
            if (_prestigeLevelText == null || _prestigeService == null)
                return;

            _prestigeLevelText.text = NumberFormatter.Format(_prestigeService.PrestigeCount);
        }
    }

    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnEnter;
        public Action OnExit;

        public void OnPointerEnter(PointerEventData eventData) => OnEnter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => OnExit?.Invoke();
    }
}