using UnityEngine.Events;

namespace IdleAirport.GameCore
{
    public sealed class AITSAScannerUpgradeCardView : PurchaseCardView
    {
        private const string Title = "AI Scanner";
        private const string LockedLabel = "Locked";
        private const string NotInstalledLabel = "Not installed";
        private const string OnlineLabel = "online";
        private const string TokensLabel = "tokens";

        public void SetData(AITSAScannerUpgrade upgrade)
        {
            if (upgrade == null)
            {
                SetUnavailableState();
                return;
            }

            bool canPurchase = upgrade.CanPurchase();
            PurchaseCardVisualState visualState = canPurchase
                ? PurchaseCardVisualState.Available
                : PurchaseCardVisualState.NeedMoney;

            string stateLabel = upgrade.OwnedCount <= 0
                ? NotInstalledLabel
                : $"{upgrade.EffectiveScannerCount}/{upgrade.OwnedCount} {OnlineLabel}";

            string tokenLabel = upgrade.OwnedCount <= 0
                ? $"+{upgrade.TokensPerScanner} {TokensLabel}"
                : $"{upgrade.CurrentTokens}/{upgrade.MaxTokens} {TokensLabel}";

            string actionText = canPurchase 
                ? $"${FormatCost(upgrade.CurrentCost)}" 
                : $"Need ${FormatCost(upgrade.CurrentCost)}";

            SetText(_nameText, Title);
            SetText(_stateText, CombineLines(stateLabel, tokenLabel));
            SetText(_actionText, actionText);
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
