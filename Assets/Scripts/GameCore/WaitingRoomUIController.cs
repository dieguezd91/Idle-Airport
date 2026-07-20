using System.Collections.Generic;
using System;
using IdleAirport.GameCore.Prestige;
using UnityEngine;
using UnityEngine.Serialization;

namespace IdleAirport.GameCore
{
    public sealed class WaitingRoomUIController : MonoBehaviour, IPrestigeResettable
    {
        [Header("Settings")]
        [SerializeField] private Vector2 _areaSize = new Vector2(400f, 200f);
        [SerializeField] private float _cellSize = 45f;
        [SerializeField] private int _columns = 6;
        [SerializeField] private int _rows = 16;
        [SerializeField] private int _maxPassengers = 15;

        [Header("Flight Boarding")]
        [FormerlySerializedAs("_autoBoardPassengers")]
        [SerializeField] private bool _autoBoardPassengers = true;
        [FormerlySerializedAs("_boardingInterval")]
        [SerializeField] private float _boardingInterval = 4f;
        [FormerlySerializedAs("_passengersPerBoarding")]
        [SerializeField] private int _passengersPerBoarding = 8;

        private RectTransform _container;
        private readonly List<PassengerUIVisual> _passengers = new();
        private int _reservedSlots;
        private float _boardingTimer;
        private bool _wasFull;
        private Vector2 _baseAreaSize;
        private float _baseCellSize;
        private int _baseColumns;
        private int _baseRows;
        private int _baseMaxPassengers;
        private int _basePassengersPerBoarding;
        private bool _hasBaseLayout;
        private bool _isPrestigeBoardingLayoutApplied;

        public event Action<int, int, int> OnOccupancyChanged;
        public event Action<int> OnPassengersBoarded;
        public event Action OnWaitingRoomFull;

        public int VisualCapacity => Mathf.Max(0, _columns * _rows);
        public int Capacity => _maxPassengers > 0 ? Mathf.Min(_maxPassengers, VisualCapacity) : VisualCapacity;

        public bool HasCapacity => HasReservableCapacity;
        public bool HasPhysicalCapacity => _passengers.Count < Capacity;
        public bool HasReservableCapacity => _passengers.Count + _reservedSlots < Capacity;
        public bool IsFlightBoardingActive => _autoBoardPassengers;
        public bool HasPassengersInGate => _passengers.Count > 0;
        public float TimeUntilNextBoarding
        {
            get
            {
                if (!_autoBoardPassengers)
                    return 0f;

                if (_passengers.Count == 0)
                    return 0f;

                return Mathf.Max(0f, _boardingInterval - _boardingTimer);
            }
        }
        public int CurrentCount => _passengers.Count;
        public int ReservedCount => _reservedSlots;
        public Vector2 AreaSize => _areaSize;
        public bool IsPrestigeBoardingLayoutApplied => _isPrestigeBoardingLayoutApplied;

        private void Awake()
        {
            _container = GetComponent<RectTransform>();
            CacheBaseLayout();
            ValidateCapacityConfiguration();
            NotifyOccupancyChanged();
        }

        private void OnValidate()
        {
            _columns = Mathf.Max(1, _columns);
            _rows = Mathf.Max(1, _rows);
            _maxPassengers = Mathf.Max(0, _maxPassengers);
            _cellSize = Mathf.Max(1f, _cellSize);
            _areaSize = new Vector2(Mathf.Max(1f, _areaSize.x), Mathf.Max(1f, _areaSize.y));
            _boardingInterval = Mathf.Max(0.1f, _boardingInterval);
            _passengersPerBoarding = Mathf.Max(1, _passengersPerBoarding);
        }

        private void Update()
        {
            if (!_autoBoardPassengers) return;
            if (_passengers.Count == 0) return;

            _boardingTimer += Time.deltaTime;
            while (_boardingTimer >= _boardingInterval && _passengers.Count > 0)
            {
                _boardingTimer -= _boardingInterval;
                BoardPassengers();
            }
        }

        public void SetAreaSize(Vector2 areaSize)
        {
            _areaSize = new Vector2(Mathf.Max(1f, areaSize.x), Mathf.Max(1f, areaSize.y));
            ReapplyGridPositions();
        }

        public void ApplyPrestigeBoardingLayout()
        {
            ApplyPrestigeBoardingLayout(true);
        }

        public void ApplyPrestigeBoardingLayout(bool enabled)
        {
            ApplyPrestigeBoardingLayout(enabled ? 1 : 0);
        }

        public void ApplyPrestigeBoardingLayout(int prestigeCount)
        {
            CacheBaseLayout();

            if (prestigeCount <= 0)
            {
                _areaSize = _baseAreaSize;
                _cellSize = _baseCellSize;
                _columns = _baseColumns;
                _rows = _baseRows;
                _maxPassengers = _baseMaxPassengers;
                _passengersPerBoarding = _basePassengersPerBoarding;
                _isPrestigeBoardingLayoutApplied = false;
                ReapplyGridPositions();
                NotifyOccupancyChanged();
                return;
            }

            int baseCapacity = _baseMaxPassengers > 0
                ? Mathf.Min(_baseMaxPassengers, _baseColumns * _baseRows)
                : _baseColumns * _baseRows;
            
            float scale = Mathf.Pow(1.5f, prestigeCount);
            int targetCapacity = Mathf.RoundToInt(baseCapacity * scale);
            int targetColumns = Mathf.RoundToInt(_baseColumns * Mathf.Sqrt(scale));
            int targetRows = Mathf.CeilToInt(targetCapacity / (float)targetColumns);

            _areaSize = _baseAreaSize;
            _columns = Mathf.Max(1, targetColumns);
            _rows = Mathf.Max(1, targetRows);
            _maxPassengers = Mathf.Min(targetCapacity, _columns * _rows);
            _cellSize = CalculatePrestigeCellSize(_columns, _rows);
            _passengersPerBoarding = _basePassengersPerBoarding * Mathf.RoundToInt(Mathf.Pow(2f, prestigeCount));
            _isPrestigeBoardingLayoutApplied = true;

            ReapplyGridPositions();
            NotifyOccupancyChanged();
        }

        public void ResetForPrestige()
        {
            for (int i = 0; i < _passengers.Count; i++)
                _passengers[i]?.Recycle();

            _passengers.Clear();
            _reservedSlots = 0;
            _boardingTimer = 0f;
            _wasFull = false;
            NotifyOccupancyChanged();
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

        private void BoardPassengers()
        {
            int boardedCount = Mathf.Min(_passengersPerBoarding, _passengers.Count);
            if (boardedCount <= 0)
                return;

            for (int i = 0; i < boardedCount; i++)
            {
                PassengerUIVisual p = _passengers[0];
                _passengers.RemoveAt(0);
                if (p != null)
                {
                    p.Recycle();
                }
            }

            ReapplyGridPositions();
            NotifyOccupancyChanged();
            OnPassengersBoarded?.Invoke(boardedCount);
        }

        private System.Collections.IEnumerator AnimateBoardingAndRecycle(PassengerUIVisual passenger)
        {
            if (passenger == null) yield break;

            Vector2 exitPos = new Vector2(_areaSize.x * 0.5f + 50f, 0f);
            passenger.MoveTo(exitPos);

            float duration = 0.35f;
            float elapsed = 0f;

            var canvasGroup = passenger.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = passenger.gameObject.AddComponent<CanvasGroup>();
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f - (elapsed / duration);
                }
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            passenger.Recycle();
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

        private float CalculatePrestigeCellSize(int columns, int rows)
        {
            float widthBasedSize = columns > 1
                ? (_baseAreaSize.x * 0.92f) / (columns - 1)
                : _baseCellSize;
            float heightBasedSize = rows > 1
                ? (_baseAreaSize.y * 0.92f) / rows
                : _baseCellSize;
            float targetSize = Mathf.Min(_baseCellSize * 0.8f, widthBasedSize, heightBasedSize);
            return Mathf.Max(20f, targetSize);
        }

        private void CacheBaseLayout()
        {
            if (_hasBaseLayout)
                return;

            _baseAreaSize = _areaSize;
            _baseCellSize = _cellSize;
            _baseColumns = Mathf.Max(1, _columns);
            _baseRows = Mathf.Max(1, _rows);
            _baseMaxPassengers = Mathf.Max(0, _maxPassengers);
            _basePassengersPerBoarding = Mathf.Max(1, _passengersPerBoarding);
            _hasBaseLayout = true;
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
            int current = _passengers.Count;
            int capacity = Capacity;
            bool isFull = current + _reservedSlots >= capacity;

            if (isFull && !_wasFull)
            {
                OnWaitingRoomFull?.Invoke();
            }
            _wasFull = isFull;

            OnOccupancyChanged?.Invoke(current, _reservedSlots, capacity);
        }
    }
}