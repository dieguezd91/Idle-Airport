using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class ShopVisualDisplayController : MonoBehaviour
    {
        [SerializeField] private StoresManager _storesManager;
        [SerializeField] private RectTransform _builtContainer;
        [SerializeField] private ShopVisualItemView _itemTemplate;

        private ShopVisualItemView[] _shopVisuals;

        private void Start()
        {
            if (!ValidateReferences()) return;

            _shopVisuals = new ShopVisualItemView[_storesManager.BusinessCount];
            _storesManager.OnStorePurchased += HandleStorePurchased;
            SyncFromCurrentState();
        }

        private void OnDestroy()
        {
            if (_storesManager != null)
                _storesManager.OnStorePurchased -= HandleStorePurchased;
        }

        private void HandleStorePurchased(int index, Store store)
        {
            CreateOrUpdateVisual(index, store.Name, store.OwnedCount);
        }

        private void SyncFromCurrentState()
        {
            Store[] businesses = _storesManager.Businesses;
            if (businesses == null) return;

            for (int i = 0; i < businesses.Length; i++)
            {
                if (businesses[i].OwnedCount > 0)
                {
                    CreateOrUpdateVisual(i, businesses[i].Name, businesses[i].OwnedCount);
                }
            }
        }

        private void CreateOrUpdateVisual(int index, string name, int ownedCount)
        {
            if (_shopVisuals[index] == null)
            {
                ShopVisualItemView visual = Instantiate(_itemTemplate, _builtContainer);
                visual.name = $"ShopItem_{name}";
                visual.gameObject.SetActive(true);
                _shopVisuals[index] = visual;
            }

            _shopVisuals[index].SetData(name, ownedCount);
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
