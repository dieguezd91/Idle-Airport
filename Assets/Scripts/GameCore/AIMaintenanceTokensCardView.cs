using UnityEngine.Events;

namespace IdleAirport.GameCore
{
    public sealed class AIMaintenanceTokensCardView : PurchaseCardView
    {
        private const string Title = "Tokens";
        private const string LockedLabel = "Locked";
        private const string FullLabel = "Full";

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

            string stateLabel = hasScanner
                ? $"{upgrade.CurrentTokens}/{upgrade.MaxTokens}"
                : LockedLabel;
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
            SetText(_stateText, CombineLines(stateLabel, benefitLabel));
            SetText(_actionText, actionLabel);
            SetText(_iconText, BuildShortAcronym(Title));
            ApplyVisualState(visualState, canPurchase && hasScanner && hasCapacity && !isFull);
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
        }
    }
}
