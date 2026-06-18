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

            for (int i = 0; i < _activeQueue.Count; i++)
                _activeQueue[i].MoveTo(_queueSlots[i].anchoredPosition);

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
            p.SetPositionImmediate(_queueSlots[nextSlot].anchoredPosition);
            p.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            p.gameObject.SetActive(true);
            _activeQueue.Add(p);
        }

        private void ActivatePassenger(int slotIndex)
        {
            if (_passengerPool == null) return;
            if (slotIndex >= _queueSlots.Length) return;

            if (!_passengerPool.TryGet(out PassengerUIVisual p))
                return;

            p.transform.SetParent(transform, false);
            p.SetPositionImmediate(_queueSlots[slotIndex].anchoredPosition);
            p.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            p.gameObject.SetActive(true);
            _activeQueue.Add(p);
        }
    }
}
