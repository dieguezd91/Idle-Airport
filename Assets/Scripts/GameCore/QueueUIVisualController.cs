using System.Collections.Generic;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class QueueUIVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private RectTransform[] _queueSlots;

        [Header("Prefab Pool")]
        [SerializeField] private PassengerUIVisual _passengerTemplate;

        [Header("Settings")]
        [SerializeField] private int _initialPassengerCount = 5;
        [SerializeField] private int _poolSize = 10;

        private readonly List<PassengerUIVisual> _activeQueue = new();
        private readonly List<PassengerUIVisual> _pool = new();

        public bool IsBlocked => _waitingRoom != null && !_waitingRoom.HasCapacity;

        private void Start()
        {
            int minRequired = _initialPassengerCount;
            if (_waitingRoom != null)
                minRequired += _waitingRoom.Capacity;

            int targetSize = Mathf.Max(_poolSize, minRequired);

            if (_poolSize < minRequired)
                Debug.LogWarning($"QueueUIVisualController: _poolSize ({_poolSize}) is too small. " +
                    $"Need at least {minRequired} ({_initialPassengerCount} initial + {_waitingRoom.Capacity} waiting room capacity). " +
                    $"Pool expanded to {targetSize}.");

            BuildPool(targetSize);
            int count = Mathf.Min(_initialPassengerCount, _queueSlots.Length, _pool.Count);
            for (int i = 0; i < count; i++)
                ActivatePassenger(i);
        }

        public bool TryProcessFrontPassenger()
        {
            if (_activeQueue.Count == 0) return false;
            if (_waitingRoom == null) return false;
            if (!_waitingRoom.HasCapacity) return false;

            PassengerUIVisual front = _activeQueue[0];
            _activeQueue.RemoveAt(0);

            if (!_waitingRoom.TryReceivePassenger(front))
                return false;

            for (int i = 0; i < _activeQueue.Count; i++)
                _activeQueue[i].MoveTo(_queueSlots[i].anchoredPosition);

            TryAddPassengerToBack();
            return true;
        }

        private void BuildPool(int size)
        {
            for (int i = 0; i < size; i++)
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

        private void TryAddPassengerToBack()
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

        private PassengerUIVisual GetInactivePassenger()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeInHierarchy)
                    return _pool[i];
            }
            return null;
        }
    }
}
