using System.Collections;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class AIUpgradeFeedbackView : MonoBehaviour
    {
        [Header("Button Feedbacks")]
        [SerializeField] private UIButtonFeedbackView _aiScannerButtonFeedback;
        [SerializeField] private UIButtonFeedbackView _tokensButtonFeedback;
        [SerializeField] private UIButtonFeedbackView _durabilityButtonFeedback;

        [Header("Target Visuals")]
        [SerializeField] private RectTransform _aiScannerMainVisual;
        [SerializeField] private RectTransform _tokensIconVisual;
        [SerializeField] private RectTransform _durabilityIconVisual;

        private Coroutine _scannerPopRoutine;
        private Coroutine _tokensPunchRoutine;
        private Coroutine _durabilityPunchRoutine;

        private Vector3 _scannerBaseScale = Vector3.one;
        private Vector3 _tokensBaseScale = Vector3.one;
        private Vector3 _durabilityBaseScale = Vector3.one;

        private void Awake()
        {
            if (_aiScannerMainVisual != null) _scannerBaseScale = _aiScannerMainVisual.localScale;
            if (_tokensIconVisual != null) _tokensBaseScale = _tokensIconVisual.localScale;
            if (_durabilityIconVisual != null) _durabilityBaseScale = _durabilityIconVisual.localScale;
        }

        public void PlayAIScannerSuccess()
        {
            // Play success on the button itself
            if (_aiScannerButtonFeedback != null)
            {
                _aiScannerButtonFeedback.PlaySuccess();
            }

            // Pop the main AI scanner visual (0.85 -> 1.08 -> 1 over 0.25s)
            if (_aiScannerMainVisual != null)
            {
                if (_scannerPopRoutine != null) StopCoroutine(_scannerPopRoutine);
                _scannerPopRoutine = StartCoroutine(PopRoutine(_aiScannerMainVisual, _scannerBaseScale, 0.85f, 1.08f, 0.25f));
            }
        }

        public void PlayTokensSuccess()
        {
            // Play success on the tokens button
            if (_tokensButtonFeedback != null)
            {
                _tokensButtonFeedback.PlaySuccess();
            }

            // Punch the token icon/text
            if (_tokensIconVisual != null)
            {
                if (_tokensPunchRoutine != null) StopCoroutine(_tokensPunchRoutine);
                _tokensPunchRoutine = StartCoroutine(PunchRoutine(_tokensIconVisual, _tokensBaseScale, 1.15f, 0.18f));
            }
        }

        public void PlayDurabilitySuccess()
        {
            // Play success on durability button
            if (_durabilityButtonFeedback != null)
            {
                _durabilityButtonFeedback.PlaySuccess();
            }

            // Punch the shield/durability icon
            if (_durabilityIconVisual != null)
            {
                if (_durabilityPunchRoutine != null) StopCoroutine(_durabilityPunchRoutine);
                _durabilityPunchRoutine = StartCoroutine(PunchRoutine(_durabilityIconVisual, _durabilityBaseScale, 1.15f, 0.18f));
            }
        }

        public void PlayFail(string buttonType)
        {
            if (buttonType == "AI" && _aiScannerButtonFeedback != null)
            {
                _aiScannerButtonFeedback.PlayFail();
            }
            else if (buttonType == "Tokens" && _tokensButtonFeedback != null)
            {
                _tokensButtonFeedback.PlayFail();
            }
            else if (buttonType == "Durability" && _durabilityButtonFeedback != null)
            {
                _durabilityButtonFeedback.PlayFail();
            }
        }

        private IEnumerator PopRoutine(RectTransform target, Vector3 baseScale, float startFactor, float peakFactor, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Quick pop envelope
                float scaleFactor;
                if (t < 0.5f)
                {
                    scaleFactor = Mathf.Lerp(startFactor, peakFactor, t * 2f);
                }
                else
                {
                    scaleFactor = Mathf.Lerp(peakFactor, 1f, (t - 0.5f) * 2f);
                }

                target.localScale = baseScale * scaleFactor;
                yield return null;
            }
            target.localScale = baseScale;
        }

        private IEnumerator PunchRoutine(RectTransform target, Vector3 baseScale, float punchFactor, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                target.localScale = baseScale * Mathf.Lerp(1f, punchFactor, pulse);
                yield return null;
            }
            target.localScale = baseScale;
        }
    }
}
