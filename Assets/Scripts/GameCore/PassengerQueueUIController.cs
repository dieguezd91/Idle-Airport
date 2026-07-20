using System.Collections.Generic;
using IdleAirport.GameCore.Prestige;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerQueueUIController : MonoBehaviour, IPrestigeResettable
    {
        [Header("References")]
        [SerializeField] private RectTransform _queueAnchor;
        [SerializeField] private PassengerPool _passengerPool;

        [Header("Settings")]
        [SerializeField] private int _initialPassengerCount = 15;
        [SerializeField] private int _maxQueueCapacity = 15;
        [SerializeField] private int _queueColumns = 3;
        [SerializeField] private float _horizontalSpacing = 25f;
        [SerializeField] private float _verticalSpacing = 22f;

        private readonly List<PassengerUIVisual> _activeQueue = new();
        private bool _isInitialized;

        public bool HasPassengerReady => _activeQueue.Count > 0;
        public int ActivePassengerCount => _activeQueue.Count;

        private void Start()
        {
            InitializeQueue();
        }

        private void OnValidate()
        {
            _initialPassengerCount = Mathf.Max(1, _initialPassengerCount);
            _maxQueueCapacity = Mathf.Max(_initialPassengerCount, _maxQueueCapacity);
            _queueColumns = Mathf.Max(1, _queueColumns);
            _horizontalSpacing = Mathf.Max(1f, _horizontalSpacing);
            _verticalSpacing = Mathf.Max(1f, _verticalSpacing);
        }

        private void InitializeQueue()
        {
            if (_isInitialized) return;
            if (_passengerPool == null)
            {
                Debug.LogError("PassengerQueueUIController: PassengerPool is not assigned.", this);
                return;
            }

            _passengerPool.Prewarm();
            int count = Mathf.Min(_initialPassengerCount, _maxQueueCapacity);
            for (int i = 0; i < count; i++)
                ActivatePassenger(i);

            ApplyStackOrdering();

            _isInitialized = true;
        }

        public void ResetForPrestige()
        {
            for (int i = 0; i < _activeQueue.Count; i++)
                _activeQueue[i]?.Recycle();

            _activeQueue.Clear();
            _isInitialized = false;
            InitializeQueue();
        }

        public bool TryDequeuePassenger(out PassengerUIVisual passenger)
        {
            if (_activeQueue.Count == 0)
            {
                passenger = null;
                return false;
            }

            passenger = _activeQueue[0];
            _activeQueue.RemoveAt(0);

            RepositionActiveQueue();
            ApplyStackOrdering();

            return true;
        }

        public void RefillBackSlotIfPossible()
        {
            if (_passengerPool == null) return;

            int nextSlot = _activeQueue.Count;
            if (nextSlot >= _maxQueueCapacity) return;

            if (!_passengerPool.TryGet(out PassengerUIVisual p))
                return;

            p.transform.SetParent(transform, false);
            p.SetPositionImmediate(GetQueueSlotPosition(nextSlot));
            p.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            p.gameObject.SetActive(true);
            _activeQueue.Add(p);
            ApplyStackOrdering();
        }

        private void ActivatePassenger(int slotIndex)
        {
            if (_passengerPool == null) return;
            if (slotIndex >= _maxQueueCapacity) return;

            if (!_passengerPool.TryGet(out PassengerUIVisual p))
                return;

            p.transform.SetParent(transform, false);
            p.SetPositionImmediate(GetQueueSlotPosition(slotIndex));
            p.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            p.gameObject.SetActive(true);
            _activeQueue.Add(p);
        }

        private void RepositionActiveQueue()
        {
            for (int i = 0; i < _activeQueue.Count; i++)
                _activeQueue[i].MoveTo(GetQueueSlotPosition(i));
        }

        private void ApplyStackOrdering()
        {
            for (int i = 0; i < _activeQueue.Count; i++)
            {
                Transform passengerTransform = _activeQueue[i].transform;
                passengerTransform.SetSiblingIndex(_activeQueue.Count - 1 - i);
            }
        }

        private Vector2 GetQueueSlotPosition(int slotIndex)
        {
            if (!TryGetQueueAnchor(out Vector2 anchorPosition))
                return Vector2.zero;

            int rowIndex = slotIndex / _queueColumns;
            int colIndex = slotIndex % _queueColumns;

            // Reverse column direction on even rows so index 0 starts on the right
            if (rowIndex % 2 == 0)
            {
                colIndex = (_queueColumns - 1) - colIndex;
            }

            float xOffset = (colIndex - (_queueColumns - 1) * 0.5f) * _horizontalSpacing;
            float yOffset = rowIndex * _verticalSpacing;

            return anchorPosition + new Vector2(xOffset, yOffset);
        }

        private bool TryGetQueueAnchor(out Vector2 anchorPosition)
        {
            if (_queueAnchor != null)
            {
                anchorPosition = _queueAnchor.anchoredPosition;
                return true;
            }

            // Fallback to component's own RectTransform
            if (transform is RectTransform rt)
            {
                anchorPosition = rt.anchoredPosition;
                return true;
            }

            anchorPosition = Vector2.zero;
            return false;
        }
    }
}