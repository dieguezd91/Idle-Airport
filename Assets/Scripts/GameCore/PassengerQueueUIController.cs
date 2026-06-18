using System.Collections.Generic;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerQueueUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform[] _queueSlots;
        [SerializeField] private PassengerPool _passengerPool;

        [Header("Settings")]
        [SerializeField] private int _initialPassengerCount = 5;
        [SerializeField] private float _verticalSpacing = 30f;

        private readonly List<PassengerUIVisual> _activeQueue = new();
        private bool _isInitialized;

        public bool HasPassengerReady => _activeQueue.Count > 0;
        public int ActivePassengerCount => _activeQueue.Count;

        private void Start()
        {
            InitializeQueue();
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
            int count = Mathf.Min(_initialPassengerCount, _queueSlots.Length);
            for (int i = 0; i < count; i++)
                ActivatePassenger(i);

            ApplyStackOrdering();

            _isInitialized = true;
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
            if (nextSlot >= _queueSlots.Length) return;

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
            if (slotIndex >= _queueSlots.Length) return;

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

            return anchorPosition + new Vector2(0f, _verticalSpacing * slotIndex);
        }

        private bool TryGetQueueAnchor(out Vector2 anchorPosition)
        {
            anchorPosition = Vector2.zero;

            if (_queueSlots == null || _queueSlots.Length == 0)
                return false;

            float sumX = 0f;
            float lowestY = 0f;
            int validCount = 0;

            for (int i = 0; i < _queueSlots.Length; i++)
            {
                RectTransform slot = _queueSlots[i];
                if (slot == null)
                    continue;

                Vector2 slotPosition = slot.anchoredPosition;
                sumX += slotPosition.x;
                lowestY = validCount == 0 ? slotPosition.y : Mathf.Min(lowestY, slotPosition.y);
                validCount++;
            }

            if (validCount == 0)
                return false;

            anchorPosition = new Vector2(sumX / validCount, lowestY);
            return true;
        }
    }
}
