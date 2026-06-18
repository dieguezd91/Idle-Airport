using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class ShopVisualItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _bonusText;
        [SerializeField] private Image _icon;

        public void SetData(string storeName, int ownedCount, double incomePerPassenger)
        {
            if (_nameText != null)
                _nameText.text = storeName;

            if (_levelText != null)
                _levelText.text = $"Lv. {ownedCount}";

            if (_bonusText != null)
            {
                double totalIncome = incomePerPassenger * ownedCount;
                _bonusText.text = $"+${NumberFormatter.Format(totalIncome, 2)}/pax";
            }
        }
    }
}