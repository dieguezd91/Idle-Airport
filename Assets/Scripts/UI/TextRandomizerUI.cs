using TMPro;
using UnityEngine;

namespace IdleAirport.UI
{
    public class TextRandomizerUI : MonoBehaviour
    {
        [SerializeField] private ScrollingTextUI scrollingText;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private string[] entries;
 
        private int _lastIndex = -1;

        private void Start()
        {
            RandomizeText();
        }
 
        [ContextMenu("Randomize Text")]
        public void RandomizeText()
        {
            if (entries == null || entries.Length == 0)
                return;
 
            var index = PickRandomIndex();
            _lastIndex = index;
 
            label.text = entries[index];
            label.ForceMeshUpdate();
 
            var rect = label.rectTransform;
            var size = rect.sizeDelta;
            size.x = label.preferredWidth;
            rect.sizeDelta = size;

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                scrollingText.Text = entries[index];
            }
            else
            {
                rect.anchoredPosition = new Vector2(size.x * .5f, rect.anchoredPosition.y);
            }
            
#else
            scrollingText.Text = entries[index];
#endif
        }
 
        private int PickRandomIndex()
        {
            if (entries.Length == 1)
                return 0;
 
            int index;
            do { index = Random.Range(0, entries.Length); }
            while (index == _lastIndex);
            return index;
        }
    }
}