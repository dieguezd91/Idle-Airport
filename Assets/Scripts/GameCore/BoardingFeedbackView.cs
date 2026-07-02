using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    public sealed class BoardingFeedbackView : MonoBehaviour
    {
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private Graphic _overlayImage;
        [SerializeField] private float _flashDuration = 0.25f;
        [SerializeField] private Color _flashTint = new(0.55f, 0.9f, 1f, 0.35f);

        private Coroutine _routine;
        private Color _baseColor;

        private void Awake()
        {
            if (_waitingRoom == null)
                _waitingRoom = FindFirstObjectByType<WaitingRoomUIController>();

            if (_overlayImage != null)
                _baseColor = _overlayImage.color;
        }

        private void OnEnable()
        {
            if (_waitingRoom != null)
                _waitingRoom.OnPassengersBoarded += HandlePassengersBoarded;
        }

        private void OnDisable()
        {
            if (_waitingRoom != null)
                _waitingRoom.OnPassengersBoarded -= HandlePassengersBoarded;
        }

        private void HandlePassengersBoarded(int count)
        {
            if (count <= 0 || _overlayImage == null)
                return;

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(PlayFlashRoutine());
        }

        private IEnumerator PlayFlashRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _flashDuration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                _overlayImage.color = Color.Lerp(_baseColor, _flashTint, pulse);
                yield return null;
            }

            _overlayImage.color = _baseColor;
            _routine = null;
        }
    }
}
