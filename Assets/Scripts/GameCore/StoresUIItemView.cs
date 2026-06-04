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

        public void SetData(Store business, bool canPurchase)
        {
            if (_nameText != null)
                _nameText.text = business.Name;

            if (_statusText != null)
            {
                _statusText.text = business.IsUnlocked
                    ? $"Owned: {business.OwnedCount} | +{NumberFormatter.Format(business.IncomePerPassenger, 2)}/s"
                    : "Locked";
            }

            if (_costText != null)
            {
                _costText.text = business.IsUnlocked
                    ? $"Cost: ${NumberFormatter.Format(business.CurrentCost, 0)}"
                    : string.Empty;
            }

            if (_buyButton != null)
                _buyButton.interactable = business.IsUnlocked && canPurchase;
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
