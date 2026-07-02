using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class BoardingFeedbackView : MonoBehaviour
    {
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private Graphic _overlayImage;
        [SerializeField] private float _flashDuration = 0.2f;
        [SerializeField] private Color _flashTint = new(0.55f, 0.9f, 1f, 0.35f);

        [Header("Scale Pulse Settings")]
        [SerializeField] private RectTransform _loungeContainer;
        [SerializeField] private float _pulseScale = 1.04f;

        [Header("Full Alert Settings")]
        [SerializeField] private Color _fullTint = new(1f, 0.3f, 0.3f, 0.35f);
        [SerializeField] private float _fullPulseDuration = 0.3f;

        private Coroutine _routine;
        private Color _baseColor;

        private void Awake()
        {
            if (_waitingRoom == null)
                _waitingRoom = FindFirstObjectByType<WaitingRoomUIController>();

            if (_overlayImage != null)
                _baseColor = _overlayImage.color;
        }

        private bool IsSafePulseTarget(RectTransform target)
        {
            if (target == null)
                return false;

            if (target == GetComponent<RectTransform>())
                return false;

            string n = target.name;
            if (n.Contains("BoardingLoungeSection") ||
                n.Contains("BoardingLounge") ||
                n.Contains("BoardingPanel") ||
                n.Contains("LoungePanel") ||
                n.Contains("Content") ||
                n.Contains("Container") ||
                n.Contains("Section"))
            {
                return false;
            }

            if (target.GetComponent<LayoutGroup>() != null ||
                target.GetComponent<ContentSizeFitter>() != null)
            {
                return false;
            }

            return true;
        }

        private void OnEnable()
        {
            if (_waitingRoom != null)
            {
                _waitingRoom.OnPassengersBoarded += HandlePassengersBoarded;
                _waitingRoom.OnWaitingRoomFull += HandleWaitingRoomFull;
            }
        }

        private void OnDisable()
        {
            if (_waitingRoom != null)
            {
                _waitingRoom.OnPassengersBoarded -= HandlePassengersBoarded;
                _waitingRoom.OnWaitingRoomFull -= HandleWaitingRoomFull;
            }
        }

        private void HandlePassengersBoarded(int count)
        {
            if (count <= 0)
                return;

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(PlayFlashRoutine());
        }

        private void HandleWaitingRoomFull(int current, int reserved, int capacity)
        {
            // Update to match WaitingRoomUIController signature (int, int, int)
            HandleWaitingRoomFull();
        }

        private void HandleWaitingRoomFull()
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(PlayFullPulseRoutine());
        }

        private IEnumerator PlayFlashRoutine()
        {
            if (!IsSafePulseTarget(_loungeContainer))
                yield break;

            Vector3 baseScale = _loungeContainer.localScale;
            float elapsed = 0f;
            while (elapsed < _flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _flashDuration);
                float pulse = Mathf.Sin(t * Mathf.PI);

                if (_overlayImage != null)
                    _overlayImage.color = Color.Lerp(_baseColor, _flashTint, pulse);

                _loungeContainer.localScale = baseScale * Mathf.Lerp(1f, _pulseScale, pulse);

                yield return null;
            }

            if (_overlayImage != null)
                _overlayImage.color = _baseColor;

            _loungeContainer.localScale = baseScale;
            _routine = null;
        }

        private IEnumerator PlayFullPulseRoutine()
        {
            if (!IsSafePulseTarget(_loungeContainer))
                yield break;

            Vector3 baseScale = _loungeContainer.localScale;
            float elapsed = 0f;
            float duration = _fullPulseDuration * 2f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI * 2f);
                float absPulse = Mathf.Abs(pulse);

                if (_overlayImage != null)
                    _overlayImage.color = Color.Lerp(_baseColor, _fullTint, absPulse);

                _loungeContainer.localScale = baseScale * Mathf.Lerp(1f, 1.03f, absPulse);

                yield return null;
            }

            if (_overlayImage != null)
                _overlayImage.color = _baseColor;

            _loungeContainer.localScale = baseScale;
            _routine = null;
        }
    }
}
