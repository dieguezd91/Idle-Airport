using UnityEngine.Events;

namespace IdleAirport.GameCore
{
    public sealed class StoresUIItemView : PurchaseCardView
    {
        private const string LockedText = "Locked";

        public void SetData(Store store, bool canPurchase)
        {
            if (store == null)
            {
                SetUnavailableState();
                return;
            }

            bool isUnlocked = store.IsUnlocked;
            bool isPurchasable = isUnlocked && canPurchase;

            PurchaseCardVisualState visualState = ResolveVisualState(isUnlocked, isPurchasable);

            SetText(_nameText, store.Name);
            SetText(_stateText, BuildStateText(store));

            string actionText = string.Empty;
            if (isUnlocked)
            {
                actionText = isPurchasable 
                    ? $"${FormatCost(store.CurrentCost)}" 
                    : $"Need ${FormatCost(store.CurrentCost)}";
            }
            SetText(_actionText, actionText);
            SetText(_iconText, BuildShortAcronym(store.Name));

            ApplyVisualState(visualState, isPurchasable);
        }

        public void SetClickHandler(UnityAction callback)
        {
            SetActionHandler(callback);
        }

        public void ClearClickHandler()
        {
            ClearActionHandler();
        }

        private static PurchaseCardVisualState ResolveVisualState(bool isUnlocked, bool isPurchasable)
        {
            if (!isUnlocked)
                return PurchaseCardVisualState.Locked;

            return isPurchasable
                ? PurchaseCardVisualState.Available
                : PurchaseCardVisualState.NeedMoney;
        }

        private string BuildStateText(Store store)
        {
            if (!store.IsUnlocked)
                return LockedText;

            return $"+${FormatBonus(store.IncomePerPassenger)}/pax";
        }

        private string BuildPriceText(Store store)
        {
            if (!store.IsUnlocked)
                return string.Empty;

            return $"${FormatCost(store.CurrentCost)}";
        }

        private void SetUnavailableState()
        {
            SetText(_nameText, string.Empty);
            SetText(_stateText, LockedText);
            SetText(_actionText, string.Empty);
            SetText(_iconText, string.Empty);

            ApplyVisualState(PurchaseCardVisualState.Locked, false);
        }
    }
}