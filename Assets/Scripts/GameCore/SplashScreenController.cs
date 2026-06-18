using System.Collections;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.Events;

namespace IdleAirport.GameCore
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SplashScreenController : MonoBehaviour, IPointerClickHandler
    {
        [Header("Timing")]
        [SerializeField] private float _fadeOutDuration = 0.35f;
        [SerializeField] private float _blinkSpeed = 2f;
        [SerializeField] private float _minBlinkAlpha = 0.35f;
        [SerializeField] private float _maxBlinkAlpha = 1f;

        [Header("Behaviour")]
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _deactivateOnComplete = true;
        [SerializeField] private TextMeshProUGUI _clickToStartText;

        [Header("Events")]
        public UnityEvent onCompleted = new UnityEvent();

        private CanvasGroup _canvasGroup;
        private Coroutine _blinkRoutine;
        private Coroutine _fadeRoutine;
        private bool _isPlaying;
        private bool _isClosing;

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            transform.SetAsLastSibling();
            SetIdleState();
        }

        private void Start()
        {
            if (_playOnStart)
                Play();
        }

        private void OnDisable()
        {
            StopBlinkRoutine();
            StopFadeRoutine();
            _isPlaying = false;
            _isClosing = false;
        }

        public void Play()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            transform.SetAsLastSibling();
            StopBlinkRoutine();
            StopFadeRoutine();

            _isPlaying = true;
            _isClosing = false;
            SetVisibleState();
            StartBlinkRoutine();
        }

        public void Skip()
        {
            if (!_isPlaying || _isClosing)
                return;

            BeginCloseSequence();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isPlaying || _isClosing)
                return;

            BeginCloseSequence();
        }

        private void BeginCloseSequence()
        {
            _isClosing = true;
            StopBlinkRoutine();
            StopFadeRoutine();
            _fadeRoutine = StartCoroutine(FadeOutSequence());
        }

        private IEnumerator FadeOutSequence()
        {
            yield return FadeTo(0f, _fadeOutDuration);
            CompleteSequence();
        }

        private IEnumerator BlinkSequence()
        {
            if (_clickToStartText != null)
                SetTextAlpha(_maxBlinkAlpha);

            if (_blinkSpeed <= 0f)
                yield break;

            float elapsed = 0f;
            while (_isPlaying && !_isClosing)
            {
                elapsed += Time.unscaledDeltaTime;
                float phase = Mathf.PingPong(elapsed * _blinkSpeed, 1f);
                float alpha = Mathf.Lerp(_minBlinkAlpha, _maxBlinkAlpha, phase);
                SetTextAlpha(alpha);
                yield return null;
            }
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_canvasGroup == null)
                yield break;

            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                yield break;
            }

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetAlpha(targetAlpha);
        }

        private void StartBlinkRoutine()
        {
            _blinkRoutine = StartCoroutine(BlinkSequence());
        }

        private void CompleteSequence()
        {
            _isPlaying = false;
            _isClosing = false;
            StopBlinkRoutine();
            StopFadeRoutine();
            SetIdleState();
            onCompleted?.Invoke();

            if (_deactivateOnComplete)
                gameObject.SetActive(false);
        }

        private void StopBlinkRoutine()
        {
            if (_blinkRoutine == null)
                return;

            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }

        private void StopFadeRoutine()
        {
            if (_fadeRoutine == null)
                return;

            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        private void SetIdleState()
        {
            SetAlpha(0f);
            SetInteraction(false);
            SetTextAlpha(_maxBlinkAlpha);
        }

        private void SetVisibleState()
        {
            SetInteraction(true);
            SetAlpha(1f);
            SetTextAlpha(_maxBlinkAlpha);
        }

        private void SetInteraction(bool active)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.interactable = active;
            _canvasGroup.blocksRaycasts = active;
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private void SetTextAlpha(float alpha)
        {
            if (_clickToStartText == null)
                return;

            Color color = _clickToStartText.color;
            color.a = Mathf.Clamp01(alpha);
            _clickToStartText.color = color;
        }
    }
}
