using System.Collections;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class ShopIncomeFeedbackPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private ShopVisualDisplayController _shopDisplay;
        [SerializeField] private UIBurstFeedbackView _burstFeedback;
        [SerializeField] private FloatingRewardTextUI _floatingRewardText;
        [SerializeField] private RectTransform _shopsFeedbackAnchor;

        [Header("Settings")]
        [SerializeField] private float _throttleDuration = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _debugShopIncomeFeedback;

        private float _lastFeedbackTime;
        private double _accumulatedBonus;
        private Coroutine _throttleRoutine;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            if (_passengerProcessor != null)
            {
                _passengerProcessor.OnPassengerProcessed += HandlePassengerProcessed;
                if (_debugShopIncomeFeedback) Debug.Log("[ShopFeedback] Subscribed to PassengerProcessor processed event.");
            }
            else
            {
                Debug.LogWarning("[ShopFeedback] Suscribe failed: PassengerProcessor is missing.");
            }
        }

        private void OnDisable()
        {
            if (_passengerProcessor != null)
            {
                _passengerProcessor.OnPassengerProcessed -= HandlePassengerProcessed;
                if (_debugShopIncomeFeedback) Debug.Log("[ShopFeedback] Unsubscribed from PassengerProcessor processed event.");
            }
        }

        private void HandlePassengerProcessed(PassengerProcessor.PassengerProcessedData data)
        {
            if (_debugShopIncomeFeedback)
            {
                Debug.Log($"[ShopFeedback] Processed Event: type={data.ProcessingType}, base={data.BaseReward}, shopBonus={data.ShopBonus}, total={data.TotalReward}");
            }

            if (data.ShopBonus <= 0.0)
                return;

            _accumulatedBonus += data.ShopBonus;

            if (_throttleRoutine == null)
            {
                if (_debugShopIncomeFeedback) Debug.Log($"[ShopFeedback] Cooldown starting. Current accumulation: {_accumulatedBonus}");
                _throttleRoutine = StartCoroutine(ThrottleRoutine());
            }
            else
            {
                if (_debugShopIncomeFeedback) Debug.Log($"[ShopFeedback] Cooldown active. Accumulated shopBonus to: {_accumulatedBonus}");
            }
        }

        private IEnumerator ThrottleRoutine()
        {
            float timeSinceLast = Time.unscaledTime - _lastFeedbackTime;
            if (timeSinceLast < _throttleDuration)
            {
                yield return new WaitForSecondsRealtime(_throttleDuration - timeSinceLast);
            }

            PlayFeedback();

            _throttleRoutine = null;
        }

        private void PlayFeedback()
        {
            if (_accumulatedBonus <= 0.0)
            {
                if (_debugShopIncomeFeedback) Debug.Log($"[ShopFeedback] PlayFeedback called but accumulatedBonus is {_accumulatedBonus}");
                return;
            }

            double amountToPlay = _accumulatedBonus;
            _accumulatedBonus = 0.0;
            _lastFeedbackTime = Time.unscaledTime;

            if (_debugShopIncomeFeedback) Debug.Log($"[ShopFeedback] Processing feedback. Amount to play: {amountToPlay}");

            Vector3 position = transform.position;
            RectTransform targetRect = null;
            ShopVisualItemView visual = _shopDisplay != null ? _shopDisplay.GetNextOwnedVisual() : null;

            if (visual != null)
            {
                visual.PlayIncomeFeedback(amountToPlay);
                targetRect = visual.GetComponent<RectTransform>();
                position = visual.FeedbackWorldPosition;
                if (_debugShopIncomeFeedback) Debug.Log($"[ShopFeedback] Selected shop visual round-robin: {visual.name} at position {position}");
            }
            else if (_shopsFeedbackAnchor != null)
            {
                targetRect = _shopsFeedbackAnchor;
                position = _shopsFeedbackAnchor.position;
                if (_debugShopIncomeFeedback) Debug.Log($"[ShopFeedback] No shop visual found, falling back to anchor {_shopsFeedbackAnchor.name} at position {position}");
            }
            else
            {
                if (_debugShopIncomeFeedback) Debug.LogWarning("[ShopFeedback] No shop visual or fallback anchor available!");
            }

            // 1. Spawn 2 to 4 mini coin particles
            if (_burstFeedback != null)
            {
                int coinCount = Random.Range(2, 5);
                _burstFeedback.SpawnBurst(position, coinCount, 35f, 0.45f);
                if (_debugShopIncomeFeedback) Debug.Log($"[ShopFeedback] Spawned {coinCount} coin particles at {position}");
            }

            // 2. Show floating reward text: only "+$X", duration 0.55s, rise 18px
            if (_floatingRewardText != null)
            {
                string text = $"+${NumberFormatter.Format(amountToPlay, 0)}";
                Color textColor = new Color(0.55f, 0.9f, 1f, 1f); // Visible light blue/cyan color
                
                if (targetRect != null)
                {
                    _floatingRewardText.ShowAtRectTransform(targetRect, text, textColor, 0.55f, new Vector2(0f, 18f));
                }
                else
                {
                    _floatingRewardText.ShowText(position, text, textColor, 0.55f, new Vector2(0f, 18f));
                }
                
                if (_debugShopIncomeFeedback) Debug.Log($"[ShopFeedback] Called FloatingRewardTextUI for text: {text} at position: {position}");
            }
            else
            {
                Debug.LogWarning("[ShopFeedback] FloatingRewardTextUI reference is missing!");
            }
        }

        private void ValidateReferences()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();
            if (_shopDisplay == null)
                _shopDisplay = FindFirstObjectByType<ShopVisualDisplayController>();
            if (_burstFeedback == null)
                _burstFeedback = FindFirstObjectByType<UIBurstFeedbackView>();
            if (_floatingRewardText == null)
                _floatingRewardText = FindFirstObjectByType<FloatingRewardTextUI>();

            if (_passengerProcessor == null)
                Debug.LogWarning("[ShopFeedback] Reference missing: PassengerProcessor.");
            if (_shopDisplay == null)
                Debug.LogWarning("[ShopFeedback] Reference missing: ShopVisualDisplayController.");
            if (_floatingRewardText == null)
                Debug.LogWarning("[ShopFeedback] Reference missing: FloatingRewardTextUI.");
            if (_shopsFeedbackAnchor == null)
                Debug.LogWarning("[ShopFeedback] Reference missing: ShopsFeedbackAnchor fallback anchor.");
        }

        [ContextMenu("Test Shop Income Feedback")]
        public void TestShopIncomeFeedback()
        {
            Debug.Log("[ShopFeedback] Executing TestShopIncomeFeedback via ContextMenu");
            _accumulatedBonus = 99.0;
            PlayFeedback();
        }
    }
}
