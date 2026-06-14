using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class PassengerUIVisual : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 800f;

        private RectTransform _rectTransform;
        private Image _image;
        private Vector2 _targetAnchored;
        private bool _isMoving;

        public bool IsMoving => _isMoving;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
        }

        public void SetPositionImmediate(Vector2 anchoredPosition)
        {
            _rectTransform.anchoredPosition = anchoredPosition;
            _targetAnchored = anchoredPosition;
            _isMoving = false;
        }

        public void MoveTo(Vector2 targetAnchoredPosition)
        {
            _targetAnchored = targetAnchoredPosition;
            _isMoving = true;
        }

        public void SetColor(Color color)
        {
            if (_image != null)
                _image.color = color;
        }

        public void Recycle()
        {
            _isMoving = false;
            _targetAnchored = Vector2.zero;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isMoving) return;

            _rectTransform.anchoredPosition = Vector2.MoveTowards(
                _rectTransform.anchoredPosition, _targetAnchored, _moveSpeed * Time.deltaTime);

            if (Vector2.Distance(_rectTransform.anchoredPosition, _targetAnchored) < 0.5f)
            {
                _rectTransform.anchoredPosition = _targetAnchored;
                _isMoving = false;
            }
        }
    }
}
