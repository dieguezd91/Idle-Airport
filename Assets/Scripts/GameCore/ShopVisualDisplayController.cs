using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class ShopVisualDisplayController : MonoBehaviour
    {
        [SerializeField] private StoresManager _storesManager;
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private FloatingRewardTextUI _floatingRewardText;
        [SerializeField] private RectTransform _builtContainer;
        [SerializeField] private ShopVisualItemView _itemTemplate;

        private ShopVisualItemView[] _shopVisuals;
        private int _incomeFeedbackIndex;

        private void Awake()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();

            if (_floatingRewardText == null)
                _floatingRewardText = FindFirstObjectByType<FloatingRewardTextUI>();
        }

        private void Start()
        {
            if (!ValidateReferences()) return;

            _shopVisuals = new ShopVisualItemView[_storesManager.StoreCount];
            _storesManager.OnStorePurchased += HandleStorePurchased;
            if (_passengerProcessor != null)
            {
                _passengerProcessor.OnPassengerManuallyProcessed += HandlePassengerProcessed;
                _passengerProcessor.OnPassengerAutoProcessed += HandlePassengerProcessed;
            }

            SyncFromCurrentState();
        }

        private void OnDestroy()
        {
            if (_storesManager != null)
                _storesManager.OnStorePurchased -= HandleStorePurchased;

            if (_passengerProcessor != null)
            {
                _passengerProcessor.OnPassengerManuallyProcessed -= HandlePassengerProcessed;
                _passengerProcessor.OnPassengerAutoProcessed -= HandlePassengerProcessed;
            }
        }

        private void HandleStorePurchased(int index, Store store)
        {
            ShopVisualItemView visual = CreateOrUpdateVisual(index, store);
            if (visual != null)
                visual.PlayPurchasedFeedback();
        }

        private void HandlePassengerProcessed(PassengerProcessor.PassengerProcessFeedbackData data)
        {
            if (data.ShopBonus <= 0.0)
                return;

            PlayIncomeFeedback(data.ShopBonus);
        }

        public void PlayIncomeFeedback(double amount)
        {
            ShopVisualItemView visual = GetNextOwnedVisual();
            if (visual != null)
                visual.PlayIncomeFeedback(amount);

            if (_floatingRewardText == null)
                return;

            Vector3 position = visual != null
                ? visual.FeedbackWorldPosition
                : _builtContainer != null
                    ? _builtContainer.position
                    : transform.position;

            _floatingRewardText.ShowShopBonus(position, amount);
        }

        private void SyncFromCurrentState()
        {
            Store[] stores = _storesManager.Stores;
            if (stores == null) return;

            for (int i = 0; i < stores.Length; i++)
            {
                if (stores[i].OwnedCount > 0)
                {
                    CreateOrUpdateVisual(i, stores[i]);
                }
            }
        }

        private ShopVisualItemView CreateOrUpdateVisual(int index, Store store)
        {
            if (store == null || _shopVisuals == null || index < 0 || index >= _shopVisuals.Length)
                return null;

            if (_shopVisuals[index] == null)
            {
                ShopVisualItemView visual = Instantiate(_itemTemplate, _builtContainer);
                visual.name = $"ShopItem_{store.Name}";
                visual.gameObject.SetActive(true);
                _shopVisuals[index] = visual;
            }

            _shopVisuals[index].SetData(store.Name, store.OwnedCount, store.IncomePerPassenger);
            return _shopVisuals[index];
        }

        private ShopVisualItemView GetNextOwnedVisual()
        {
            if (_shopVisuals == null || _shopVisuals.Length == 0)
                return null;

            for (int i = 0; i < _shopVisuals.Length; i++)
            {
                int index = (_incomeFeedbackIndex + i) % _shopVisuals.Length;
                ShopVisualItemView visual = _shopVisuals[index];
                if (visual == null || !visual.gameObject.activeInHierarchy)
                    continue;

                _incomeFeedbackIndex = (index + 1) % _shopVisuals.Length;
                return visual;
            }

            return null;
        }

        private bool ValidateReferences()
        {
            bool hasErrors = false;

            if (_storesManager == null)
            {
                Debug.LogError("ShopVisualDisplayController: StoresManager is not assigned!");
                hasErrors = true;
            }
            if (_builtContainer == null)
            {
                Debug.LogError("ShopVisualDisplayController: BuiltContainer is not assigned!");
                hasErrors = true;
            }
            if (_itemTemplate == null)
            {
                Debug.LogError("ShopVisualDisplayController: ItemTemplate is not assigned!");
                hasErrors = true;
            }

            return !hasErrors;
        }
    }
}
