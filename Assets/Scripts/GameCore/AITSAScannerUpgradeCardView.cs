using UnityEngine;
using UnityEngine.Events;

namespace IdleAirport.GameCore
{
    public sealed class AITSAScannerUpgradeCardView : PurchaseCardView
    {
        private const string Title = "AI Scanner";
        private const string ReadyLabel = "Ready";
        private const string LockedLabel = "Locked";
        private const string LevelPrefix = "Lv.";

        public void SetData(AITSAScannerUpgrade upgrade)
        {
            if (upgrade == null)
            {
                SetUnavailableState();
                return;
            }

            bool hasUpgrade = upgrade.OwnedCount > 0;
            bool canPurchase = upgrade.CanPurchase();
            PurchaseCardVisualState visualState = canPurchase
                ? PurchaseCardVisualState.Available
                : PurchaseCardVisualState.NeedMoney;

            string statusLabel = hasUpgrade
                ? $"{LevelPrefix} {upgrade.OwnedCount}"
                : ReadyLabel;
            string benefitLabel = hasUpgrade
                ? $"+{FormatRate(upgrade.CurrentPassengersPerSecond)} PPS"
                : $"+{FormatRate(upgrade.NextPassengersPerSecond)} PPS";
            string actionLabel = canPurchase
                ? $"Buy ${FormatCost(upgrade.CurrentCost)}"
                : $"Need ${FormatCost(upgrade.CurrentCost)}";

            SetText(_nameText, Title);
            SetText(_stateText, CombineLines(statusLabel, benefitLabel));
            SetText(_actionText, actionLabel);
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
            SetText(_actionText, LockedLabel);
            SetText(_iconText, BuildShortAcronym(Title));
            ApplyVisualState(PurchaseCardVisualState.Locked, false);
        }
    }
}
