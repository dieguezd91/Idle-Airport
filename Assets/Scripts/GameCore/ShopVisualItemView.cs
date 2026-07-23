using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class ShopVisualItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _bonusText;
        [SerializeField] private Image _icon;
        [SerializeField] private float _feedbackDuration = 0.22f;
        [SerializeField] private float _feedbackScale = 1.08f;
        [SerializeField] private float _incomeFeedbackScale = 1.04f;

        private Coroutine _feedbackRoutine;
        private Vector3 _baseScale = Vector3.one;

        public Vector3 FeedbackWorldPosition => transform.position;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void SetData(string storeName, int ownedCount, double incomePerPassenger)
        {
            SetData(storeName, null, ownedCount, incomePerPassenger);
        }

        public void SetData(string storeName, Sprite icon, int ownedCount, double incomePerPassenger)
        {
            if (_nameText != null)
            {
                _nameText.text = string.Empty;
                _nameText.gameObject.SetActive(false);
            }

            if (_icon != null)
            {
                if (icon != null)
                {
                    _icon.sprite = icon;
                    _icon.enabled = true;
                }
                else
                {
                    _icon.enabled = false;
                }
            }

            if (_levelText != null)
                _levelText.text = $"Lv. {ownedCount}";

            if (_bonusText != null)
            {
                double totalIncome = incomePerPassenger * ownedCount;
                _bonusText.text = $"+${NumberFormatter.Format(totalIncome, 2)}/pax";
            }
        }

        public void PlayPurchasedFeedback()
        {
            PlayScaleFeedback(_feedbackScale);
        }

        public void PlayIncomeFeedback(double amount)
        {
            PlayScaleFeedback(_incomeFeedbackScale);
        }

        private void PlayScaleFeedback(float scale)
        {
            if (!isActiveAndEnabled)
                return;

            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);

            _feedbackRoutine = StartCoroutine(PlayScaleFeedbackRoutine(scale));
        }

        private IEnumerator PlayScaleFeedbackRoutine(float scale)
        {
            float elapsed = 0f;
            while (elapsed < _feedbackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _feedbackDuration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                transform.localScale = _baseScale * Mathf.Lerp(1f, scale, pulse);
                yield return null;
            }

            transform.localScale = _baseScale;
            _feedbackRoutine = null;
        }
    }
}
