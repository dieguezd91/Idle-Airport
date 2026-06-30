using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.UI
{
    public class ImagePulseAnimUI : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private AnimationCurve scaleCurve = new (new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
        [SerializeField] private float duration = 1f;
        [SerializeField] private float scaleMultiplier = 1.2f;
        [SerializeField] private Color targetColor = Color.white;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float cooldown = 0.2f;
 
        private Vector3 _baseScale;
        private Color _baseColor;
        private float _timer;
        private float _lastPlayTime = -Mathf.Infinity;
        private bool _isPlaying;
 
        public bool IsPlaying { get; private set; }
 
        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();
 
            _baseScale = targetImage.rectTransform.localScale;
            _baseColor = targetImage.color;
        }
 
        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }
 
        public void Play()
        {
            if (Time.time - _lastPlayTime < cooldown)
                return;
 
            _lastPlayTime = Time.time;
            Stop();
            _isPlaying = true;
            IsPlaying = true;
        }
 
        public void Stop()
        {
            _isPlaying = false;
            IsPlaying = false;
            _timer = 0f;
            targetImage.rectTransform.localScale = _baseScale;
            targetImage.color = _baseColor;
        }
 
        private void Update()
        {
            if (!_isPlaying)
                return;
 
            _timer += Time.deltaTime;
            var t = Mathf.Clamp01(_timer / duration);
 
            var curveValue = scaleCurve.Evaluate(t);
            targetImage.rectTransform.localScale = _baseScale * Mathf.Lerp(1f, scaleMultiplier, curveValue);
            targetImage.color = Color.Lerp(_baseColor, targetColor, curveValue);
 
            if (t >= 1f)
            {
                _isPlaying = false;
                IsPlaying = false;
            }
        }
    }
}
