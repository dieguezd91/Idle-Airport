using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class FloatingRewardTextUI : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private int _poolSize = 8;

        [Header("Motion")]
        [SerializeField] private Vector2 _offset = new(0f, 36f);
        [SerializeField] private float _duration = 0.75f;

        [Header("Text")]
        [SerializeField] private int _fontSize = 22;
        [SerializeField] private Color _rewardColor = new(0.96f, 0.86f, 0.35f, 1f);
        [SerializeField] private Color _shopBonusColor = new(0.55f, 0.9f, 1f, 1f);

        private FloatingRewardItem[] _items;
        private int _nextIndex;

        private void Awake()
        {
            CreatePool();
        }

        public void Show(PassengerProcessor.PassengerProcessFeedbackData data)
        {
            ShowReward(data.FeedbackWorldPosition, data.TotalReward, data.ShopBonus);
        }

        public void ShowReward(Vector3 worldPosition, double totalReward, double shopBonus = 0.0)
        {
            if (shopBonus > 0.0)
            {
                string bonusStr = NumberFormatter.Format(shopBonus, 0);
                string totalStr = NumberFormatter.Format(totalReward, 0);
                string text = $"+${totalStr} <size=70%><color=#8ce6ff>(+${bonusStr} shops)</color></size>";
                ShowText(worldPosition, text, _rewardColor);
            }
            else
            {
                ShowText(worldPosition, $"+${NumberFormatter.Format(totalReward, 0)}", _rewardColor);
            }
        }

        public void ShowManualReward(Vector3 worldPosition, double totalReward)
        {
            ShowText(worldPosition, $"+${NumberFormatter.Format(totalReward, 0)}", _rewardColor, 0.55f, new Vector2(0f, 18f));
        }

        public void ShowShopBonus(Vector3 worldPosition, double shopBonus)
        {
            ShowText(worldPosition, $"${NumberFormatter.Format(shopBonus, 0)}", _shopBonusColor);
        }

        public void ShowAtRectTransform(RectTransform target, string text, Color color, float duration = 0.55f, Vector2? offset = null)
        {
            if (target == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Vector2 screenPoint;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, target.position);
            }
            else
            {
                screenPoint = RectTransformUtility.WorldToScreenPoint(null, target.position);
            }

            RectTransform parentRect = GetComponent<RectTransform>();
            if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, canvas != null ? (canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera) : null, out Vector2 localPoint))
            {
                Vector2 appliedOffset = offset ?? _offset;
                localPoint += appliedOffset;

                if (_items == null || _items.Length == 0)
                    CreatePool();

                FloatingRewardItem item = GetNextItem();
                Vector3 worldPos = parentRect.TransformPoint(localPoint);
                item.Play(worldPos, text, Vector2.zero, duration, color);
            }
            else
            {
                ShowText(target.position, text, color, duration, offset);
            }
        }

        public void ShowText(Vector3 worldPosition, string text, Color color, float? customDuration = null, Vector2? customOffset = null)
        {
            if (_items == null || _items.Length == 0)
                CreatePool();

            FloatingRewardItem item = GetNextItem();
            item.Play(worldPosition, text, customOffset ?? _offset, customDuration ?? _duration, color);
        }

        private void CreatePool()
        {
            int count = Mathf.Max(1, _poolSize);
            _items = new FloatingRewardItem[count];

            for (int i = 0; i < count; i++)
                _items[i] = CreateItem($"FloatingReward_{i}");
        }

        private FloatingRewardItem GetNextItem()
        {
            FloatingRewardItem item = _items[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _items.Length;
            return item;
        }

        private FloatingRewardItem CreateItem(string itemName)
        {
            GameObject itemObject = new(itemName, typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
            itemObject.transform.SetParent(transform, false);
            itemObject.SetActive(false);

            RectTransform rectTransform = itemObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 52f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            TextMeshProUGUI label = itemObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSize = _fontSize;
            label.raycastTarget = false;
            label.richText = true;

            return new FloatingRewardItem(itemObject, rectTransform, itemObject.GetComponent<CanvasGroup>(), label);
        }

        private sealed class FloatingRewardItem
        {
            private readonly GameObject _gameObject;
            private readonly RectTransform _rectTransform;
            private readonly CanvasGroup _canvasGroup;
            private readonly TextMeshProUGUI _label;
            private Coroutine _routine;

            public FloatingRewardItem(GameObject gameObject, RectTransform rectTransform, CanvasGroup canvasGroup, TextMeshProUGUI label)
            {
                _gameObject = gameObject;
                _rectTransform = rectTransform;
                _canvasGroup = canvasGroup;
                _label = label;
            }

            public void Play(Vector3 worldPosition, string text, Vector2 offset, float duration, Color color)
            {
                if (_routine != null)
                    _label.StopCoroutine(_routine);

                _gameObject.SetActive(true);
                _rectTransform.position = worldPosition;
                _rectTransform.SetAsLastSibling();
                _rectTransform.localScale = Vector3.one;
                _label.text = text;
                _label.color = color;
                _canvasGroup.alpha = 1f;

                _routine = _label.StartCoroutine(PlayRoutine(worldPosition, offset, Mathf.Max(0.01f, duration)));
            }

            private IEnumerator PlayRoutine(Vector3 startPosition, Vector2 offset, float duration)
            {
                Vector3 endPosition = startPosition + new Vector3(offset.x, offset.y, 0f);

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = 1f - Mathf.Pow(1f - t, 2f);

                    _rectTransform.position = Vector3.Lerp(startPosition, endPosition, eased);
                    _canvasGroup.alpha = 1f - t;
                    yield return null;
                }

                _canvasGroup.alpha = 0f;
                _gameObject.SetActive(false);
                _routine = null;
            }
        }
    }
}
