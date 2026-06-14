using System.Collections.Generic;
using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class WaitingRoomUIController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector2 _areaSize = new Vector2(400f, 200f);
        [SerializeField] private float _cellSize = 45f;
        [SerializeField] private int _columns = 6;
        [SerializeField] private int _rows = 16;
        [SerializeField] private int _maxPassengers = 15;

        [Header("Auto Boarding")]
        [SerializeField] private bool _autoBoardPassengers = true;
        [SerializeField] private float _boardingInterval = 5f;
        [SerializeField] private int _passengersPerBoarding = 1;

        private RectTransform _container;
        private readonly List<PassengerUIVisual> _passengers = new();
        private int _reservedSlots;
        private float _boardingTimer;

        public event Action<int, int, int> OnOccupancyChanged;

        public int VisualCapacity => Mathf.Max(0, _columns * _rows);
        public int Capacity => _maxPassengers > 0 ? Mathf.Min(_maxPassengers, VisualCapacity) : VisualCapacity;

        public bool HasCapacity => HasReservableCapacity;
        public bool HasPhysicalCapacity => _passengers.Count < Capacity;
        public bool HasReservableCapacity => _passengers.Count + _reservedSlots < Capacity;
        public int CurrentCount => _passengers.Count;
        public int ReservedCount => _reservedSlots;

        private void Awake()
        {
            _container = GetComponent<RectTransform>();
            ValidateCapacityConfiguration();
            NotifyOccupancyChanged();
        }

        private void OnValidate()
        {
            _columns = Mathf.Max(1, _columns);
            _rows = Mathf.Max(1, _rows);
            _maxPassengers = Mathf.Max(0, _maxPassengers);
        }

        private void Update()
        {
            if (!_autoBoardPassengers) return;
            if (_passengers.Count == 0) return;

            _boardingTimer += Time.deltaTime;
            if (_boardingTimer >= _boardingInterval)
            {
                _boardingTimer = 0f;
                int count = Mathf.Min(_passengersPerBoarding, _passengers.Count);
                RemovePassengers(count);
            }
        }

        public bool TryReceivePassenger(PassengerUIVisual passenger)
        {
            if (passenger == null) return false;
            if (!HasPhysicalCapacity) return false;

            passenger.transform.SetParent(_container, true);
            passenger.MoveTo(CalculatePosition(_passengers.Count));
            _passengers.Add(passenger);
            NotifyOccupancyChanged();
            return true;
        }

        public bool TryReceivePassengerImmediate(PassengerUIVisual passenger)
        {
            if (passenger == null) return false;
            if (!HasPhysicalCapacity) return false;

            passenger.transform.SetParent(_container, true);
            passenger.SetPositionImmediate(CalculatePosition(_passengers.Count));
            _passengers.Add(passenger);
            NotifyOccupancyChanged();
            return true;
        }

        public bool TryReserveSlot()
        {
            if (!HasReservableCapacity) return false;

            _reservedSlots++;
            NotifyOccupancyChanged();
            return true;
        }

        public void ReleaseReservedSlot()
        {
            if (_reservedSlots <= 0)
            {
                Debug.LogWarning("WaitingRoomUIController: Tried to release a reservation when none existed.");
                return;
            }

            _reservedSlots--;
            NotifyOccupancyChanged();
        }

        public bool TryReceivePassengerWithReservation(PassengerUIVisual passenger)
        {
            if (_reservedSlots <= 0) return false;
            if (!HasPhysicalCapacity) return false;

            _reservedSlots--;
            return TryReceivePassenger(passenger);
        }

        public bool TryReceivePassengerWithReservationImmediate(PassengerUIVisual passenger)
        {
            if (_reservedSlots <= 0) return false;
            if (!HasPhysicalCapacity) return false;

            _reservedSlots--;
            return TryReceivePassengerImmediate(passenger);
        }

        private void RemovePassengers(int count)
        {
            for (int i = 0; i < count; i++)
            {
                PassengerUIVisual p = _passengers[0];
                _passengers.RemoveAt(0);
                p.Recycle();
            }
            ReapplyGridPositions();
            NotifyOccupancyChanged();
        }

        private void ReapplyGridPositions()
        {
            for (int i = 0; i < _passengers.Count; i++)
                _passengers[i].MoveTo(CalculatePosition(i));
        }

        private Vector2 CalculatePosition(int index)
        {
            int col = index % _columns;
            int row = index / _columns;

            float totalWidth = (_columns - 1) * _cellSize;
            float startX = -totalWidth * 0.5f;
            float startY = (_areaSize.y - _cellSize) * 0.5f;

            return new Vector2(
                startX + col * _cellSize,
                startY - row * _cellSize);
        }

        private void ValidateCapacityConfiguration()
        {
            if (_maxPassengers > VisualCapacity)
            {
                Debug.LogWarning(
                    $"WaitingRoomUIController: _maxPassengers ({_maxPassengers}) exceeds visual capacity ({VisualCapacity}). " +
                    "Capacity has been clamped to the visual limit.", this);
            }
        }

        private void NotifyOccupancyChanged()
        {
            OnOccupancyChanged?.Invoke(_passengers.Count, _reservedSlots, Capacity);
        }
    }
}
