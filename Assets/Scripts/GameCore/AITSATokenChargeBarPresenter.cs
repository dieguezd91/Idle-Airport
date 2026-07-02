using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class AITSATokenChargeBarPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AITSAScannerUpgrade _aiScannerUpgrade;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private CanvasGroup _warningGroup;

        [Header("Feedback")]
        [SerializeField] private float _feedbackDuration = 0.18f;
        [SerializeField] private float _emptyWarningDuration = 0.45f;
        [SerializeField] private float _pulseScale = 1.06f;
        [SerializeField] private bool _animateFill = true;

        private Coroutine _fillRoutine;
        private Coroutine _feedbackRoutine;
        private Coroutine _warningRoutine;
        private RectTransform _feedbackTarget;
        private Vector3 _baseScale = Vector3.one;
        private bool _loggedMissingUpgrade;

        private void Awake()
        {
            ResolveReferences();
            CacheFeedbackTarget();
            if (_warningGroup != null)
                _warningGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_aiScannerUpgrade == null)
            {
                LogMissingUpgradeOnce();
                enabled = false;
                return;
            }

            _aiScannerUpgrade.TokensChanged += HandleTokensChanged;
            _aiScannerUpgrade.TokensConsumed += HandleTokensConsumed;
            _aiScannerUpgrade.TokensRefilled += HandleTokensRefilled;
            _aiScannerUpgrade.TokensEmpty += HandleTokensEmpty;

            Refresh(animate: false);
        }

        private void OnDisable()
        {
            if (_aiScannerUpgrade != null)
            {
                _aiScannerUpgrade.TokensChanged -= HandleTokensChanged;
                _aiScannerUpgrade.TokensConsumed -= HandleTokensConsumed;
                _aiScannerUpgrade.TokensRefilled -= HandleTokensRefilled;
                _aiScannerUpgrade.TokensEmpty -= HandleTokensEmpty;
            }

            StopActiveRoutines();
        }

        public void Refresh()
        {
            Refresh(animate: false);
        }

        private void Refresh(bool animate)
        {
            if (_aiScannerUpgrade == null)
            {
                SetFill(0f);
                SetAmountText(0, 0);
                return;
            }

            SetAmountText(_aiScannerUpgrade.CurrentTokens, _aiScannerUpgrade.MaxTokens);
            SetFill(_aiScannerUpgrade.TokenFill01, animate && _animateFill);
        }

        private void HandleTokensChanged(int currentTokens, int maxTokens)
        {
            SetAmountText(currentTokens, maxTokens);
            SetFill(CalculateFill(currentTokens, maxTokens), _animateFill);
        }

        private void HandleTokensConsumed(int previousTokens, int currentTokens)
        {
            PlayPulse();
        }

        private void HandleTokensRefilled(int previousTokens, int currentTokens)
        {
            PlayPulse();
        }

        private void HandleTokensEmpty()
        {
            PlayEmptyWarning();
        }

        private void SetFill(float fill)
        {
            SetFill(fill, false);
        }

        private void SetFill(float fill, bool animate)
        {
            fill = Mathf.Clamp01(fill);

            if (_fillRoutine != null)
            {
                StopCoroutine(_fillRoutine);
                _fillRoutine = null;
            }

            if (!animate)
            {
                ApplyFill(fill);
                return;
            }

            _fillRoutine = StartCoroutine(AnimateFillRoutine(GetCurrentFill(), fill));
        }

        private IEnumerator AnimateFillRoutine(float from, float to)
        {
            float duration = Mathf.Max(0.01f, _feedbackDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ApplyFill(Mathf.Lerp(from, to, t));
                yield return null;
            }

            ApplyFill(to);
            _fillRoutine = null;
        }

        private void ApplyFill(float fill)
        {
            if (_fillImage != null)
                _fillImage.fillAmount = fill;

            if (_slider != null)
                _slider.value = fill;
        }

        private float GetCurrentFill()
        {
            if (_fillImage != null)
                return _fillImage.fillAmount;

            if (_slider != null)
                return _slider.value;

            return 0f;
        }

        private void SetAmountText(int currentTokens, int maxTokens)
        {
            if (_amountText == null)
                return;

            _amountText.text = maxTokens > 0 ? $"{currentTokens}/{maxTokens}" : "0/0";
        }

        private void PlayPulse()
        {
            if (_feedbackTarget == null)
                return;

            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);

            _feedbackRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float duration = Mathf.Max(0.01f, _feedbackDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                _feedbackTarget.localScale = _baseScale * Mathf.Lerp(1f, _pulseScale, pulse);
                yield return null;
            }

            _feedbackTarget.localScale = _baseScale;
            _feedbackRoutine = null;
        }

        private void PlayEmptyWarning()
        {
            if (_warningGroup == null)
                return;

            if (_warningRoutine != null)
                StopCoroutine(_warningRoutine);

            _warningRoutine = StartCoroutine(EmptyWarningRoutine());
        }

        private IEnumerator EmptyWarningRoutine()
        {
            float duration = Mathf.Max(0.01f, _emptyWarningDuration);
            float elapsed = 0f;
            _warningGroup.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _warningGroup.alpha = 1f - t;
                yield return null;
            }

            _warningGroup.alpha = 0f;
            _warningRoutine = null;
        }

        private void StopActiveRoutines()
        {
            if (_fillRoutine != null)
                StopCoroutine(_fillRoutine);
            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);
            if (_warningRoutine != null)
                StopCoroutine(_warningRoutine);

            _fillRoutine = null;
            _feedbackRoutine = null;
            _warningRoutine = null;

            if (_feedbackTarget != null)
                _feedbackTarget.localScale = _baseScale;
            if (_warningGroup != null)
                _warningGroup.alpha = 0f;
        }

        private void ResolveReferences()
        {
            if (_aiScannerUpgrade == null)
                _aiScannerUpgrade = FindFirstObjectByType<AITSAScannerUpgrade>();

            if (_fillImage == null)
                _fillImage = GetComponentInChildren<Image>(true);

            if (_slider == null)
                _slider = GetComponentInChildren<Slider>(true);

            if (_amountText == null)
                _amountText = GetComponentInChildren<TMP_Text>(true);
        }

        private void CacheFeedbackTarget()
        {
            _feedbackTarget = _fillImage != null
                ? _fillImage.rectTransform
                : _slider != null
                    ? _slider.GetComponent<RectTransform>()
                    : GetComponent<RectTransform>();

            if (_feedbackTarget != null)
                _baseScale = _feedbackTarget.localScale;
        }

        private void LogMissingUpgradeOnce()
        {
            if (_loggedMissingUpgrade)
                return;

            _loggedMissingUpgrade = true;
            Debug.LogWarning("AITSATokenChargeBarPresenter: AITSAScannerUpgrade reference is missing.", this);
        }

        private static float CalculateFill(int currentTokens, int maxTokens)
        {
            return maxTokens <= 0 ? 0f : Mathf.Clamp01(currentTokens / (float)maxTokens);
        }
    }
}