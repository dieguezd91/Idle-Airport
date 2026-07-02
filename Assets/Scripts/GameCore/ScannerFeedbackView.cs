using System.Collections;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class ScannerFeedbackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private FloatingRewardTextUI _floatingRewardText;
        [SerializeField] private RectTransform _manualFeedbackTarget;
        [SerializeField] private RectTransform _autoFeedbackTarget;

        [Header("Success Feedback")]
        [SerializeField] private float _pulseDuration = 0.18f;
        [SerializeField] private float _pulseScale = 1.03f;

        [Header("Failure Feedback")]
        [SerializeField] private float _shakeDuration = 0.16f;
        [SerializeField] private float _shakeDistance = 4f;

        private Coroutine _manualRoutine;
        private Coroutine _autoRoutine;
        private Vector3 _manualBaseScale = Vector3.one;
        private Vector3 _autoBaseScale = Vector3.one;
        private Vector2 _manualBasePosition;

        private void Awake()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();

            if (_floatingRewardText == null)
                _floatingRewardText = FindFirstObjectByType<FloatingRewardTextUI>();

            CacheBaseState();
        }

        private void OnEnable()
        {
            if (_passengerProcessor == null)
                return;

            _passengerProcessor.OnPassengerManuallyProcessed += HandleManualProcessed;
            _passengerProcessor.OnPassengerAutoProcessed += HandleAutoProcessed;
            _passengerProcessor.OnPassengerProcessFailed += HandleProcessFailed;
        }

        private void OnDisable()
        {
            if (_passengerProcessor == null)
                return;

            _passengerProcessor.OnPassengerManuallyProcessed -= HandleManualProcessed;
            _passengerProcessor.OnPassengerAutoProcessed -= HandleAutoProcessed;
            _passengerProcessor.OnPassengerProcessFailed -= HandleProcessFailed;
        }

        private void HandleManualProcessed(PassengerProcessor.PassengerProcessFeedbackData data)
        {
            PlayManualSuccess();
            if (_floatingRewardText != null)
                _floatingRewardText.ShowReward(data.FeedbackWorldPosition, data.TotalReward);
        }

        private void HandleAutoProcessed(PassengerProcessor.PassengerProcessFeedbackData data)
        {
            PlayAutoSuccess();
            if (_floatingRewardText != null)
                _floatingRewardText.ShowReward(data.FeedbackWorldPosition, data.TotalReward);
        }

        private void HandleProcessFailed(PassengerProcessor.PassengerProcessFailedFeedbackData data)
        {
            if (data.ProcessingType != PassengerProcessor.PassengerProcessingType.Manual)
                return;

            if (_manualRoutine != null)
                StopCoroutine(_manualRoutine);

            _manualRoutine = StartCoroutine(PlayFailureRoutine());
        }

        private void PlayManualSuccess()
        {
            if (_manualFeedbackTarget == null)
                return;

            if (_manualRoutine != null)
                StopCoroutine(_manualRoutine);

            _manualRoutine = StartCoroutine(PlaySuccessRoutine(_manualFeedbackTarget, _manualBaseScale, isManual: true));
        }

        private void PlayAutoSuccess()
        {
            if (_autoFeedbackTarget == null)
                return;

            if (_autoRoutine != null)
                StopCoroutine(_autoRoutine);

            _autoRoutine = StartCoroutine(PlaySuccessRoutine(_autoFeedbackTarget, _autoBaseScale, isManual: false));
        }

        private IEnumerator PlaySuccessRoutine(RectTransform target, Vector3 baseScale, bool isManual)
        {
            float elapsed = 0f;
            while (elapsed < _pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _pulseDuration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                target.localScale = baseScale * Mathf.Lerp(1f, _pulseScale, pulse);
                yield return null;
            }

            target.localScale = baseScale;

            if (isManual)
                _manualRoutine = null;
            else
                _autoRoutine = null;
        }

        private IEnumerator PlayFailureRoutine()
        {
            RectTransform target = _manualFeedbackTarget;
            if (target == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _shakeDuration);
                float damp = 1f - t;
                float x = Mathf.Sin(t * Mathf.PI * 6f) * _shakeDistance * damp;
                target.anchoredPosition = _manualBasePosition + new Vector2(x, 0f);
                yield return null;
            }

            target.anchoredPosition = _manualBasePosition;
            target.localScale = _manualBaseScale;
            _manualRoutine = null;
        }

        private void CacheBaseState()
        {
            if (_manualFeedbackTarget != null)
            {
                _manualBaseScale = _manualFeedbackTarget.localScale;
                _manualBasePosition = _manualFeedbackTarget.anchoredPosition;
            }

            if (_autoFeedbackTarget != null)
                _autoBaseScale = _autoFeedbackTarget.localScale;
        }
    }
}
