using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public enum PurchaseCardVisualState
    {
        Available,
        NeedMoney,
        Locked
    }

    public abstract class PurchaseCardView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] protected TextMeshProUGUI _nameText;

        [FormerlySerializedAs("_statusText")]
        [SerializeField] protected TextMeshProUGUI _stateText;

        [FormerlySerializedAs("_costText")]
        [SerializeField] protected TextMeshProUGUI _actionText;

        [SerializeField] protected TextMeshProUGUI _iconText;

        [FormerlySerializedAs("_buyButton")]
        [SerializeField] protected Button _actionButton;

        [FormerlySerializedAs("_buttonBackground")]
        [SerializeField] protected Image _cardBackground;

        [SerializeField] protected Image _actionBackground;
        [SerializeField] protected Image _iconBackground;

        [Header("Card Colors")]
        [SerializeField] private Color _availableCardColor = new(0.07f, 0.11f, 0.17f, 0.96f);
        [SerializeField] private Color _needMoneyCardColor = new(0.12f, 0.09f, 0.04f, 0.96f);
        [SerializeField] private Color _lockedCardColor = new(0.05f, 0.05f, 0.08f, 0.92f);

        [Header("Action Colors")]
        [SerializeField] private Color _availableActionColor = new(0.18f, 0.58f, 0.24f, 1f);
        [SerializeField] private Color _needMoneyActionColor = new(0.79f, 0.49f, 0.12f, 1f);
        [SerializeField] private Color _lockedActionColor = new(0.28f, 0.28f, 0.31f, 1f);

        [Header("Accent Colors")]
        [SerializeField] private Color _availableAccentColor = new(0.27f, 0.85f, 0.90f, 1f);
        [SerializeField] private Color _needMoneyAccentColor = new(1f, 0.66f, 0.18f, 1f);
        [SerializeField] private Color _lockedAccentColor = new(0.56f, 0.60f, 0.69f, 1f);

        [Header("Text Colors")]
        private static readonly Color NameTextColor = Color.black;
        private static readonly Color StateTextColor = new(0.88f, 0.92f, 0.97f, 1f);
        private static readonly Color ActionTextColor = Color.black;
        private static readonly Color IconTextColor = Color.white;

        [Header("Locked Feedback")]
        [SerializeField, Range(0f, 1f)] private float _lockedTextAlpha = 0.72f;

        protected virtual void Awake()
        {
            DisableTextRaycasts();
        }

        protected void SetActionHandler(UnityAction callback)
        {
            if (_actionButton == null)
                return;

            _actionButton.onClick.RemoveAllListeners();

            if (callback != null)
                _actionButton.onClick.AddListener(callback);
        }

        protected void ClearActionHandler()
        {
            if (_actionButton != null)
                _actionButton.onClick.RemoveAllListeners();
        }

        protected void ApplyVisualState(PurchaseCardVisualState state, bool interactable)
        {
            if (_actionButton != null)
                _actionButton.interactable = interactable;

            Color cardColor = GetCardColor(state);
            Color actionColor = GetActionColor(state);
            Color accentColor = GetAccentColor(state);

            SetColor(_cardBackground, cardColor);
            SetColor(GetActionGraphic(), actionColor);
            SetColor(_iconBackground, accentColor);

            ApplyTextColors(state);
            DisableTextRaycasts();
        }

        protected void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? string.Empty;
        }

        protected static string CombineLines(params string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return string.Empty;

            StringBuilder builder = new();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (builder.Length > 0)
                    builder.Append('\n');

                builder.Append(line);
            }

            return builder.ToString();
        }

        protected static string BuildShortAcronym(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string[] parts = value.Split(' ');
            char[] acronym = new char[2];
            int count = 0;

            for (int i = 0; i < parts.Length && count < acronym.Length; i++)
            {
                string part = parts[i].Trim();

                if (part.Length == 0)
                    continue;

                acronym[count] = char.ToUpperInvariant(part[0]);
                count++;
            }

            if (count > 0)
                return new string(acronym, 0, count);

            string trimmed = value.Trim();
            int length = Mathf.Min(3, trimmed.Length);

            return trimmed.Substring(0, length).ToUpperInvariant();
        }

        protected static string FormatCost(double value)
        {
            return NumberFormatter.Format(value, 0);
        }

        protected static string FormatBonus(double value)
        {
            return NumberFormatter.Format(value, 2);
        }

        protected static string FormatRate(double value)
        {
            return NumberFormatter.Format(value, 2);
        }

        private Color GetCardColor(PurchaseCardVisualState state)
        {
            return state switch
            {
                PurchaseCardVisualState.Available => _availableCardColor,
                PurchaseCardVisualState.NeedMoney => _needMoneyCardColor,
                PurchaseCardVisualState.Locked => _lockedCardColor,
                _ => _lockedCardColor
            };
        }

        private Color GetActionColor(PurchaseCardVisualState state)
        {
            return state switch
            {
                PurchaseCardVisualState.Available => _availableActionColor,
                PurchaseCardVisualState.NeedMoney => _needMoneyActionColor,
                PurchaseCardVisualState.Locked => _lockedActionColor,
                _ => _lockedActionColor
            };
        }

        private Color GetAccentColor(PurchaseCardVisualState state)
        {
            return state switch
            {
                PurchaseCardVisualState.Available => _availableAccentColor,
                PurchaseCardVisualState.NeedMoney => _needMoneyAccentColor,
                PurchaseCardVisualState.Locked => _lockedAccentColor,
                _ => _lockedAccentColor
            };
        }

        private Graphic GetActionGraphic()
        {
            if (_actionBackground != null)
                return _actionBackground;

            if (_actionButton != null)
                return _actionButton.targetGraphic;

            return null;
        }

        private void ApplyTextColors(PurchaseCardVisualState state)
        {
            SetTextColor(_nameText, NameTextColor);
            SetTextColor(_stateText, StateTextColor);
            SetTextColor(_actionText, ActionTextColor);
            SetTextColor(_iconText, IconTextColor);

            float mainTextAlpha = state == PurchaseCardVisualState.Locked
                ? _lockedTextAlpha
                : 1f;

            SetTextAlpha(_nameText, mainTextAlpha);
            SetTextAlpha(_stateText, mainTextAlpha);
        }

        private void DisableTextRaycasts()
        {
            SetRaycastTarget(_nameText, false);
            SetRaycastTarget(_stateText, false);
            SetRaycastTarget(_actionText, false);
            SetRaycastTarget(_iconText, false);
        }

        private static void SetColor(Graphic graphic, Color color)
        {
            if (graphic != null)
                graphic.color = color;
        }

        private static void SetTextColor(TextMeshProUGUI label, Color color)
        {
            if (label != null)
                label.color = color;
        }

        private static void SetTextAlpha(TextMeshProUGUI label, float alpha)
        {
            if (label == null)
                return;

            Color color = label.color;
            color.a = alpha;
            label.color = color;
        }

        private static void SetRaycastTarget(Graphic graphic, bool enabled)
        {
            if (graphic != null)
                graphic.raycastTarget = enabled;
        }
    }
}