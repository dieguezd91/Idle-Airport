using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore.Prestige
{
    public sealed class AirportPrestigeHudPresenter : MonoBehaviour
    {
        [SerializeField] private AirportPrestigeService _prestigeService;
        [SerializeField] private TMP_Text _passportProgressText;
        [SerializeField] private Image _passportIcon;
        [SerializeField] private Button _prestigeButton;
        [SerializeField] private TMP_Text _prestigeLevelText;

        private void Awake()
        {
            if (_prestigeService == null)
                _prestigeService = FindFirstObjectByType<AirportPrestigeService>();

            if (_passportProgressText == null && name == "PassengerCounterText")
                _passportProgressText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            BindButton();
            SubscribeToService();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindButton();
            UnsubscribeFromService();
        }

        public void Configure(AirportPrestigeService prestigeService, TMP_Text passportProgressText, Image passportIcon, Button prestigeButton)
        {
            UnbindButton();
            UnsubscribeFromService();

            _prestigeService = prestigeService;
            _passportProgressText = passportProgressText;
            _passportIcon = passportIcon;
            _prestigeButton = prestigeButton;

            if (isActiveAndEnabled)
            {
                BindButton();
                SubscribeToService();
            }

            Refresh();
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

            if (_passportIcon != null)
                _passportIcon.enabled = _passportIcon.sprite != null;
        }

        private void HandlePassportsProgressChanged(int current, int required)
        {
            SetProgressText(current, required);
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
                _passportProgressText.text = $"{NumberFormatter.Format(current)} / {NumberFormatter.Format(required)}";
        }

        private void SetButtonState(bool canPrestige)
        {
            if (_prestigeButton != null)
                _prestigeButton.interactable = canPrestige;
        }

        // Reads the prestige level straight from the service on every call:
        // no local copy is kept, so the text always reflects the current state.
        private void SetPrestigeLevel()
        {
            if (_prestigeLevelText == null || _prestigeService == null)
                return;

            _prestigeLevelText.text = NumberFormatter.Format(_prestigeService.PrestigeCount);
        }
    }
}