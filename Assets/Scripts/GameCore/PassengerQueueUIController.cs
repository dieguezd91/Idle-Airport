using System.Collections.Generic;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerQueueUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform[] _queueSlots;

        [Header("Prefab Pool")]
        [SerializeField] private PassengerUIVisual _passengerTemplate;

        [Header("Settings")]
        [SerializeField] private int _initialPassengerCount = 5;
        [SerializeField] private int _poolSize = 25;

        private readonly List<PassengerUIVisual> _activeQueue = new();
        private readonly List<PassengerUIVisual> _pool = new();
        private bool _isInitialized;

        public bool HasPassengerReady => _activeQueue.Count > 0;
        public int ActivePassengerCount => _activeQueue.Count;

        private void Awake()
        {
            InitializeQueue();
        }

        private void InitializeQueue()
        {
            if (_isInitialized) return;

            BuildPool();
            int count = Mathf.Min(_initialPassengerCount, _queueSlots.Length, _pool.Count);
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
            int nextSlot = _activeQueue.Count;
            if (nextSlot >= _queueSlots.Length) return;

            PassengerUIVisual p = GetInactivePassenger();
            if (p == null) return;

            p.transform.SetParent(transform, false);
            p.gameObject.SetActive(true);
            p.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            p.SetPositionImmediate(_queueSlots[nextSlot].anchoredPosition);
            _activeQueue.Add(p);
        }

        private void BuildPool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                PassengerUIVisual p = Instantiate(_passengerTemplate, transform, false);
                p.gameObject.SetActive(false);
                _pool.Add(p);
            }
        }

        private void ActivatePassenger(int slotIndex)
        {
            if (slotIndex >= _pool.Count) return;
            if (slotIndex >= _queueSlots.Length) return;

            PassengerUIVisual p = _pool[slotIndex];
            p.gameObject.SetActive(true);
            p.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            p.SetPositionImmediate(_queueSlots[slotIndex].anchoredPosition);
            _activeQueue.Add(p);
        }

        private PassengerUIVisual GetInactivePassenger()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeInHierarchy)
                    return _pool[i];
            }
            PassengerUIVisual p = Instantiate(_passengerTemplate, transform, false);
            p.gameObject.SetActive(false);
            _pool.Add(p);
            return p;
        }
    }
}
