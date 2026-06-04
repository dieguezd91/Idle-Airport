using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class ShopVisualItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _icon;

        public void SetData(string storeName, int ownedCount)
        {
            if (_nameText != null)
                _nameText.text = storeName;

            if (_levelText != null)
                _levelText.text = $"Level {ownedCount}";
        }
    }
}
