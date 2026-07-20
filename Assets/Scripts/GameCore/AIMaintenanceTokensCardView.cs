using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

namespace IdleAirport.GameCore
{
    public sealed class AIMaintenanceTokensCardView : PurchaseCardView
    {
        private const string Title = "Tokens";
        private const string LockedLabel = "Locked";
        private const string FullLabel = "Full";

        [Header("Card Progress Bar")]
        [SerializeField] private Image _cardBackgroundImage;
        [SerializeField] private Image _cardFillImage;
        [SerializeField] private float _lerpDuration = 0.18f;

        private Coroutine _lerpRoutine;

        public void SetData(AITSAScannerUpgrade upgrade)
        {
            if (upgrade == null)
            {
                SetUnavailableState();
                return;
            }

            bool hasScanner = upgrade.OwnedCount > 0;
            bool hasCapacity = hasScanner && upgrade.MaxTokens > 0;
            bool isFull = hasCapacity && upgrade.CurrentTokens >= upgrade.MaxTokens;
            bool canPurchase = upgrade.CanPurchaseTokenPack();
            PurchaseCardVisualState visualState = !hasScanner
                ? PurchaseCardVisualState.Locked
                : !hasCapacity || isFull
                    ? PurchaseCardVisualState.Locked
                : canPurchase
                    ? PurchaseCardVisualState.Available
                    : PurchaseCardVisualState.NeedMoney;

            string benefitLabel = !hasScanner
                ? string.Empty
                : isFull
                    ? FullLabel
                    : $"+{upgrade.TokenPackSize}";
            string actionLabel = !hasScanner
                ? string.Empty
                : isFull
                    ? string.Empty
                    : canPurchase
                        ? $"${FormatCost(upgrade.TokenPackCost)}"
                        : $"Need ${FormatCost(upgrade.TokenPackCost)}";

            SetText(_nameText, Title);
            SetText(_stateText, benefitLabel);
            SetText(_actionText, actionLabel);
            SetText(_iconText, BuildShortAcronym(Title));
            ApplyVisualState(visualState, canPurchase && hasScanner && hasCapacity && !isFull);

            float targetFill = hasCapacity ? upgrade.TokenFill01 : 0f;
            UpdateCardFill(targetFill);

            if (_cardBackgroundImage != null)
                _cardBackgroundImage.gameObject.SetActive(hasScanner);
            if (_cardFillImage != null)
                _cardFillImage.gameObject.SetActive(hasScanner);
        }

        private void UpdateCardFill(float targetFill)
        {
            if (_cardFillImage == null) return;

            // Ensure the image is configured as filled even if set while inactive
            _cardFillImage.type = Image.Type.Filled;
            _cardFillImage.fillMethod = Image.FillMethod.Horizontal;

            if (_lerpRoutine != null)
            {
                StopCoroutine(_lerpRoutine);
                _lerpRoutine = null;
            }

            if (!gameObject.activeInHierarchy)
            {
                _cardFillImage.fillAmount = targetFill;
                return;
            }

            _lerpRoutine = StartCoroutine(LerpFillRoutine(_cardFillImage.fillAmount, targetFill));
        }

        private IEnumerator LerpFillRoutine(float startFill, float targetFill)
        {
            float elapsed = 0f;
            while (elapsed < _lerpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _lerpDuration);
                _cardFillImage.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                yield return null;
            }

            _cardFillImage.fillAmount = targetFill;
            _lerpRoutine = null;
        }

        public void SetClickHandler(UnityAction callback)
        {
            SetActionHandler(callback);
        }

        public void ClearClickHandler()
        {
            ClearActionHandler();
        }

        private void SetUnavailableState()
        {
            SetText(_nameText, Title);
            SetText(_stateText, LockedLabel);
            SetText(_actionText, string.Empty);
            SetText(_iconText, BuildShortAcronym(Title));
            ApplyVisualState(PurchaseCardVisualState.Locked, false);

            if (_cardBackgroundImage != null)
                _cardBackgroundImage.gameObject.SetActive(false);
            if (_cardFillImage != null)
                _cardFillImage.gameObject.SetActive(false);
        }
    }
}
