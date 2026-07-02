using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace IdleAirport.GameCore
{
    public sealed class UIBurstFeedbackView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color _particleColor = new(0.96f, 0.86f, 0.35f, 1f); // Gold color
        [SerializeField] private float _fontSize = 16f;
        [SerializeField] private int _poolSize = 15;

        private List<TextMeshProUGUI> _pool = new();
        private int _poolIndex;

        private void Awake()
        {
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject obj = new GameObject($"DollarParticle_{i}", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
                obj.transform.SetParent(transform, false);
                obj.SetActive(false);

                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(40f, 40f); // Space for "$" text

                TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
                label.text = "$";
                label.fontSize = _fontSize;
                label.color = _particleColor;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;

                _pool.Add(label);
            }
        }

        public void SpawnBurst(Vector3 worldPosition, int count, float radius, float duration)
        {
            if (_pool.Count == 0) return;

            for (int i = 0; i < count; i++)
            {
                TextMeshProUGUI label = GetNextParticle();
                if (label == null) continue;

                label.gameObject.SetActive(true);
                RectTransform rect = label.GetComponent<RectTransform>();
                rect.position = worldPosition;

                CanvasGroup cg = label.GetComponent<CanvasGroup>();
                if (cg == null) cg = label.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;

                Vector2 randomDir = Random.insideUnitCircle.normalized;
                // Vertical bias: ascend slightly and disperse laterally
                randomDir.y = Mathf.Abs(randomDir.y) * 0.8f + 0.2f; 
                float dist = Random.Range(radius * 0.4f, radius);
                Vector2 targetOffset = randomDir * dist;

                StartCoroutine(ParticleRoutine(rect, cg, rect.anchoredPosition, rect.anchoredPosition + targetOffset, duration));
            }
        }

        private TextMeshProUGUI GetNextParticle()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                int index = (_poolIndex + i) % _pool.Count;
                if (!_pool[index].gameObject.activeSelf)
                {
                    _poolIndex = (index + 1) % _pool.Count;
                    return _pool[index];
                }
            }
            int recycleIndex = _poolIndex;
            _poolIndex = (_poolIndex + 1) % _pool.Count;
            _pool[recycleIndex].gameObject.SetActive(false);
            return _pool[recycleIndex];
        }

        private IEnumerator ParticleRoutine(RectTransform rect, CanvasGroup cg, Vector2 start, Vector2 end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                float eased = 1f - (1f - t) * (1f - t);
                rect.anchoredPosition = Vector2.Lerp(start, end, eased);
                cg.alpha = 1f - t;
                yield return null;
            }

            rect.gameObject.SetActive(false);
        }
    }
}
