using System.Collections;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class UIButtonFeedbackView : MonoBehaviour
    {
        private Vector3 _baseScale = Vector3.one;
        private Vector2 _baseAnchoredPosition;
        private RectTransform _rectTransform;
        private Coroutine _activeRoutine;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
            {
                _baseScale = _rectTransform.localScale;
                _baseAnchoredPosition = _rectTransform.anchoredPosition;
            }
        }

        public void PlaySuccess()
        {
            if (!isActiveAndEnabled || _rectTransform == null) return;
            StopActiveFeedback();
            _activeRoutine = StartCoroutine(SuccessRoutine());
        }

        public void PlayFail()
        {
            if (!isActiveAndEnabled || _rectTransform == null) return;
            StopActiveFeedback();
            _activeRoutine = StartCoroutine(FailRoutine());
        }

        public void PlayPulse()
        {
            if (!isActiveAndEnabled || _rectTransform == null) return;
            StopActiveFeedback();
            _activeRoutine = StartCoroutine(PulseRoutine());
        }

        private void StopActiveFeedback()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _baseScale;
                _rectTransform.anchoredPosition = _baseAnchoredPosition;
            }
        }

        private IEnumerator SuccessRoutine()
        {
            float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                _rectTransform.localScale = _baseScale * Mathf.Lerp(1f, 1.08f, pulse);
                yield return null;
            }
            _rectTransform.localScale = _baseScale;
            _activeRoutine = null;
        }

        private IEnumerator FailRoutine()
        {
            float duration = 0.18f;
            float elapsed = 0f;
            float shakeDistance = 4f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float damp = 1f - t;
                float x = Mathf.Sin(t * Mathf.PI * 6f) * shakeDistance * damp;
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(x, 0f);
                yield return null;
            }
            _rectTransform.anchoredPosition = _baseAnchoredPosition;
            _activeRoutine = null;
        }

        private IEnumerator PulseRoutine()
        {
            float duration = 0.4f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                _rectTransform.localScale = _baseScale * Mathf.Lerp(1f, 1.05f, pulse);
                yield return null;
            }
            _rectTransform.localScale = _baseScale;
            _activeRoutine = null;
        }
    }
}
