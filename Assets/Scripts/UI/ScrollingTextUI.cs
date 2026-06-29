using System.Collections;
using TMPro;
using UnityEngine;

public class ScrollingTextUI : MonoBehaviour
{
    public float ScrollSpeed { get; set; }
 
    public string Text
    {
        get => label.text;
        set
        {
            label.text = value;
            RestartScroll();
        }
    }
 
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float scrollSpeed = 80f;
    [SerializeField] private float pauseAtStart = 1.5f;
    [SerializeField] private float pauseAtEnd = 0.5f;
    [SerializeField] private float loopGap = 60f;
 
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
 
    private IEnumerator ScrollRoutine()
    {
        yield return null;
 
        label.ForceMeshUpdate();
 
        var textWidth = label.preferredWidth;
        var viewWidth = _parentRect != null ? _parentRect.rect.width : Screen.width;
        var entryX = _startX + viewWidth;
        var endX = _startX - textWidth - loopGap;
 
        SetAnchoredX(entryX);
        yield return new WaitForSeconds(pauseAtStart);
 
        while (true)
        {
            var x = entryX;
 
            while (x > endX)
            {
                x -= ScrollSpeed * Time.deltaTime;
                x = Mathf.Max(x, endX);
                SetAnchoredX(x);
                yield return null;
            }
 
            yield return new WaitForSeconds(pauseAtEnd);
 
            SetAnchoredX(entryX);
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
