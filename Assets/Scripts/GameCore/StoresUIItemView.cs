using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class StoresUIItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _buyButton;

        public void SetData(Store store, bool canPurchase)
        {
            if (_nameText != null)
                _nameText.text = store.Name;

            if (_statusText != null)
            {
                _statusText.text = !store.IsUnlocked
                    ? "Unavailable"
                    : store.OwnedCount > 0
                        ? $"Built L{store.OwnedCount} | +${NumberFormatter.Format(store.IncomePerPassenger, 2)}/passenger each"
                        : $"Available | Not built | +${NumberFormatter.Format(store.IncomePerPassenger, 2)}/passenger";
            }

            if (_costText != null)
            {
                _costText.text = store.IsUnlocked
                    ? $"Cost: ${NumberFormatter.Format(store.CurrentCost, 0)}"
                    : string.Empty;
            }

            if (_buyButton != null)
                _buyButton.interactable = store.IsUnlocked && canPurchase;
        }

        public void SetClickHandler(UnityAction callback)
        {
            if (_buyButton != null)
                _buyButton.onClick.AddListener(callback);
        }

        public void ClearClickHandler()
        {
            if (_buyButton != null)
                _buyButton.onClick.RemoveAllListeners();
        }
    }
}
