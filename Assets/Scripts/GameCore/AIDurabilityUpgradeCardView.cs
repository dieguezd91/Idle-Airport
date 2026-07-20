using UnityEngine.Events;

namespace IdleAirport.GameCore
{
    public sealed class AIDurabilityUpgradeCardView : PurchaseCardView
    {
        private const string Title = "Token Capacity";
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
                ? $"Max Capacity: {upgrade.TokensPerScanner}"
                : LockedLabel;
            string benefitLabel = hasScanner
                ? $"Upgrade: +{upgrade.TokensPerDurabilityUpgrade} Max"
                : string.Empty;

            string displayName = (hasScanner && upgrade.DurabilityUpgradeCount > 0)
                ? $"{Title} <size=80%><color=#8ce6ff>(Lv. {upgrade.DurabilityUpgradeCount})</color></size>"
                : Title;

            string actionLabel = string.Empty;
            if (hasScanner)
            {
                actionLabel = canPurchase 
                    ? $"${FormatCost(upgrade.DurabilityUpgradeCost)}" 
                    : $"Need ${FormatCost(upgrade.DurabilityUpgradeCost)}";
            }

            SetText(_nameText, displayName);
            SetText(_stateText, CombineLines(stateLabel, benefitLabel));
            SetText(_actionText, actionLabel);
            SetText(_iconText, "CAP");
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
            SetText(_iconText, "CAP");
            ApplyVisualState(PurchaseCardVisualState.Locked, false);
        }
    }
}
