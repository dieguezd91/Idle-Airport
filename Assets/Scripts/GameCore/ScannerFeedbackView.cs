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

        [Header("Passenger Reward Feedback")]
        [SerializeField] private Vector3 _passengerRewardFeedbackSpawnOffset = new(0f, 32f, 0f);

        [Header("Success Feedback")]
        [SerializeField] private float _pulseDuration = 0.18f;
        [SerializeField] private float _pulseScale = 1.06f;

        [Header("Failure Feedback")]
        [SerializeField] private float _shakeDuration = 0.16f;
        [SerializeField] private float _shakeDistance = 4f;
        [SerializeField] private Color _failColor = new(1f, 0.3f, 0.3f, 1f);

        [Header("AI Progress Bar")]
        [SerializeField] private Color _aiBarColor = new(0.2f, 0.7f, 1f, 1f);
        [SerializeField] private Color _aiBarBgColor = new(0.1f, 0.1f, 0.1f, 0.6f);

        [Header("AI Scanner State Colors")]
        [SerializeField] private Color _aiOperationalColor = new(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color _aiNoTokensColor = new(0.5f, 0.5f, 0.5f, 0.8f);

        private Coroutine _manualRoutine;
        private Coroutine _autoRoutine;
        private Vector3 _manualBaseScale = Vector3.one;
        private Vector3 _autoBaseScale = Vector3.one;
        private Vector2 _manualBasePosition;
        private UnityEngine.UI.Image _aiProgressBarFill;
        private GameObject _aiProgressBarContainer;
        private ScannerStationUIController _aiScannerInstance;
        private AITSAScannerUpgrade _aiUpgrade;

        private void Awake()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();

            if (_floatingRewardText == null)
                _floatingRewardText = FindFirstObjectByType<FloatingRewardTextUI>();

            if (_aiUpgrade == null)
                _aiUpgrade = FindFirstObjectByType<AITSAScannerUpgrade>();

            CacheBaseState();
        }

        private void Start()
        {
            var scanners = FindObjectsByType<ScannerStationUIController>(FindObjectsSortMode.None);
            foreach (var s in scanners)
            {
                if (s.IsAutoScanner)
                {
                    _aiScannerInstance = s;
                    break;
                }
            }

            if (_aiScannerInstance != null)
            {
                _aiScannerInstance.OnAutoProcessingStarted += HandleAutoProcessingStarted;
                _aiScannerInstance.OnAutoProcessingProgress += HandleAutoProcessingProgress;
                _aiScannerInstance.OnAutoProcessingCompleted += HandleAutoProcessingCompleted;

                CreateAIProgressBar();
            }
        }

        private void OnDestroy()
        {
            if (_aiScannerInstance != null)
            {
                _aiScannerInstance.OnAutoProcessingStarted -= HandleAutoProcessingStarted;
                _aiScannerInstance.OnAutoProcessingProgress -= HandleAutoProcessingProgress;
                _aiScannerInstance.OnAutoProcessingCompleted -= HandleAutoProcessingCompleted;
            }
        }

        private void Update()
        {
            UpdateAIScannerVisualState();
        }

        private void UpdateAIScannerVisualState()
        {
            if (_autoFeedbackTarget == null || _autoRoutine != null) return;

            var img = _autoFeedbackTarget.GetComponent<UnityEngine.UI.Image>();
            if (img == null) return;

            if (_aiUpgrade != null && _aiUpgrade.OwnedCount > 0)
            {
                img.color = _aiUpgrade.HasTokens ? _aiOperationalColor : _aiNoTokensColor;
            }
        }

        private void CreateAIProgressBar()
        {
            if (_autoFeedbackTarget == null) return;

            _aiProgressBarContainer = new GameObject("AIProgressBar", typeof(RectTransform));
            _aiProgressBarContainer.transform.SetParent(_autoFeedbackTarget, false);

            RectTransform containerRect = _aiProgressBarContainer.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(100f, 10f);
            containerRect.anchoredPosition = new Vector2(0f, -60f);

            var bgImage = _aiProgressBarContainer.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = _aiBarBgColor;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(_aiProgressBarContainer.transform, false);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            _aiProgressBarFill = fillObj.AddComponent<UnityEngine.UI.Image>();
            _aiProgressBarFill.color = _aiBarColor;
            _aiProgressBarFill.type = UnityEngine.UI.Image.Type.Filled;
            _aiProgressBarFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            _aiProgressBarFill.fillAmount = 0f;

            _aiProgressBarContainer.SetActive(false);
        }

        private void HandleAutoProcessingStarted(float duration)
        {
            if (_aiProgressBarContainer != null)
                _aiProgressBarContainer.SetActive(true);
            if (_aiProgressBarFill != null)
                _aiProgressBarFill.fillAmount = 0f;
        }

        private void HandleAutoProcessingProgress(float progress)
        {
            if (_aiProgressBarFill != null)
                _aiProgressBarFill.fillAmount = progress;
        }

        private void HandleAutoProcessingCompleted(PassengerUIVisual passenger)
        {
            if (_aiProgressBarContainer != null)
                _aiProgressBarContainer.SetActive(false);
        }

        private void OnEnable()
        {
            if (_passengerProcessor == null)
                return;

            _passengerProcessor.OnPassengerProcessed += HandlePassengerProcessed;
            _passengerProcessor.OnPassengerProcessFailed += HandleProcessFailed;
        }

        private void OnDisable()
        {
            if (_passengerProcessor == null)
                return;

            _passengerProcessor.OnPassengerProcessed -= HandlePassengerProcessed;
            _passengerProcessor.OnPassengerProcessFailed -= HandleProcessFailed;
        }

        private void HandlePassengerProcessed(PassengerProcessor.PassengerProcessedData data)
        {
            bool isManual = data.ProcessingType == PassengerProcessor.PassengerProcessingType.Manual;
            Vector3 position = GetPassengerRewardFeedbackPosition(isManual);

            if (isManual)
                PlayManualSuccess();
            else
                PlayAutoSuccess();

            if (_floatingRewardText != null && data.BaseReward > 0.0)
                _floatingRewardText.ShowManualReward(position, data.BaseReward);
        }

        private Vector3 GetPassengerRewardFeedbackPosition(bool isManual)
        {
            RectTransform target = isManual ? _manualFeedbackTarget : _autoFeedbackTarget;
            Vector3 basePosition = target != null ? target.position : Vector3.zero;
            return basePosition + _passengerRewardFeedbackSpawnOffset;
        }

        private void HandleProcessFailed(PassengerProcessor.PassengerProcessFailedFeedbackData data)
        {
            if (data.ProcessingType != PassengerProcessor.PassengerProcessingType.Manual)
                return;

            if (_manualRoutine != null)
                StopCoroutine(_manualRoutine);

            if (_manualFeedbackTarget != null)
            {
                var pulse = _manualFeedbackTarget.GetComponent<IdleAirport.UI.ImagePulseAnimUI>();
                if (pulse == null)
                {
                    pulse = _manualFeedbackTarget.GetComponentInChildren<IdleAirport.UI.ImagePulseAnimUI>();
                }
                if (pulse != null)
                {
                    pulse.Stop();
                }
            }

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
