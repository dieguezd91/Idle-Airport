using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class StoresUIItemView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Image _buttonBackground;

        [Header("Button Colors")]
        [SerializeField] private Color _availableButtonColor = new(0.2f, 0.55f, 0.25f, 1f);
        [SerializeField] private Color _disabledButtonColor = new(0.3f, 0.3f, 0.3f, 1f);

        private const string LockedText = "Locked";

        public void SetData(Store store, bool canPurchase)
        {
            if (store == null)
            {
                SetUnavailableState();
                return;
            }

            bool isUnlocked = store.IsUnlocked;
            bool isPurchasable = isUnlocked && canPurchase;

            SetName(store);
            SetStatus(store);
            SetCost(store, isUnlocked, canPurchase);
            SetButtonState(isPurchasable);
        }

        public void SetClickHandler(UnityAction callback)
        {
            if (_buyButton == null)
                return;

            _buyButton.onClick.RemoveAllListeners();

            if (callback != null)
                _buyButton.onClick.AddListener(callback);
        }

        public void ClearClickHandler()
        {
            if (_buyButton != null)
                _buyButton.onClick.RemoveAllListeners();
        }

        private void SetName(Store store)
        {
            if (_nameText != null)
                _nameText.text = store.Name;
        }

        private void SetStatus(Store store)
        {
            if (_statusText == null)
                return;

            if (!store.IsUnlocked)
            {
                _statusText.text = LockedText;
                return;
            }

            string income = NumberFormatter.Format(store.IncomePerPassenger, 2);

            _statusText.text = store.OwnedCount > 0
                ? $"Lv. {store.OwnedCount} • +${income}/pax"
                : $"+${income}/pax";
        }

        private void SetCost(Store store, bool isUnlocked, bool canPurchase)
        {
            if (_costText == null)
                return;

            if (!isUnlocked)
            {
                _costText.text = LockedText;
                return;
            }

            string cost = NumberFormatter.Format(store.CurrentCost, 0);

            _costText.text = canPurchase
                ? $"Buy ${cost}"
                : $"Need ${cost}";
        }

        private void SetButtonState(bool isPurchasable)
        {
            if (_buyButton != null)
                _buyButton.interactable = isPurchasable;

            if (_buttonBackground != null)
            {
                _buttonBackground.color = isPurchasable
                    ? _availableButtonColor
                    : _disabledButtonColor;
            }
        }

        private void SetUnavailableState()
        {
            if (_nameText != null)
                _nameText.text = string.Empty;

            if (_statusText != null)
                _statusText.text = LockedText;

            if (_costText != null)
                _costText.text = LockedText;

            SetButtonState(false);
        }
    }
}