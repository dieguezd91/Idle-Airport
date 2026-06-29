using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace IdleAirport.UI
{
    public class ScrollingTextUI : MonoBehaviour
    {
        public float ScrollSpeed { get; set; }

        public string Text
        {
            get => label.text;
            set
            {
                label.text = value;
                _startX = label.rectTransform.rect.width * .5f;
                RestartScroll();
            }
        }

        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float scrollSpeed = 80f;
        [SerializeField] private float pauseAtStart = 1.5f;
        [SerializeField] private float pauseAtEnd = 0.5f;
        [SerializeField] private float loopGap = 60f;

        [Header("Events")]
        [SerializeField] private UnityEvent onScrollEnd;

        private RectTransform _rect;
        private RectTransform _parentRect;
        private Coroutine _scrollCoroutine;
        private float _startX;

        private void Awake()
        {
            _rect = label.rectTransform;
            _parentRect = _rect.parent as RectTransform;
            _startX = _rect.anchoredPosition.x;
            ScrollSpeed = scrollSpeed;
        }

        private void OnEnable() => RestartScroll();
        private void OnDisable() => StopScroll();

        public void RestartScroll()
        {
            StopScroll();
            _scrollCoroutine = StartCoroutine(ScrollRoutine());
        }

        public void StopScroll()
        {
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
                _scrollCoroutine = null;
            }
        }

        public float TextWidth => label.preferredWidth;
        public float ViewWidth => _parentRect != null ? _parentRect.rect.width : Screen.width;
        public float EntryX => _startX + ViewWidth;
        public float EndX => _startX - TextWidth - loopGap;

        private IEnumerator ScrollRoutine()
        {
            yield return null;

            label.ForceMeshUpdate();

            SetAnchoredX(EntryX);
            yield return new WaitForSeconds(pauseAtStart);

            while (true)
            {
                var x = EntryX;

                while (x > EndX)
                {
                    x -= ScrollSpeed * Time.deltaTime;
                    x = Mathf.Max(x, EndX);
                    SetAnchoredX(x);
                    yield return null;
                }

                onScrollEnd.Invoke();

                SetAnchoredX(EntryX);
                yield return new WaitForSeconds(pauseAtEnd);

                yield return new WaitForSeconds(pauseAtStart);
            }
        }

        private void SetAnchoredX(float x)
        {
            var pos = _rect.anchoredPosition;
            pos.x = x;
            _rect.anchoredPosition = pos;
        }
    }
}
