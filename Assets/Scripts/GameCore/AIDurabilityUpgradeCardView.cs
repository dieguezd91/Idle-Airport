using UnityEngine.Events;

namespace IdleAirport.GameCore
{
    public sealed class AIDurabilityUpgradeCardView : PurchaseCardView
    {
        private const string Title = "Durability";
        private const string LockedLabel = "Locked";

        public void SetData(AITSAScannerUpgrade upgrade)
        {
            if (upgrade == null)
            {
                SetUnavailableState();
                return;
            }

            bool hasScanner = upgrade.OwnedCount > 0;
            bool canPurchase = hasScanner && upgrade.CanPurchaseDurabilityUpgrade();
            PurchaseCardVisualState visualState = canPurchase
                ? PurchaseCardVisualState.Available
                : hasScanner
                    ? PurchaseCardVisualState.NeedMoney
                    : PurchaseCardVisualState.Locked;

            string stateLabel = hasScanner
                ? $"Cap {upgrade.TokensPerScanner}"
                : LockedLabel;
            string benefitLabel = hasScanner
                ? $"+{upgrade.TokensPerDurabilityUpgrade}"
                : string.Empty;

            SetText(_nameText, Title);
            SetText(_stateText, CombineLines(stateLabel, benefitLabel));
            SetText(_actionText, hasScanner ? $"${FormatCost(upgrade.DurabilityUpgradeCost)}" : string.Empty);
            SetText(_iconText, BuildShortAcronym(Title));
            ApplyVisualState(visualState, canPurchase);
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
