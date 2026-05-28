using System.Collections.Generic;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class WaitingRoomUIController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector2 _areaSize = new Vector2(400f, 200f);
        [SerializeField] private float _cellSize = 45f;
        [SerializeField] private int _maxPassengers = 15;

        [Header("Auto Boarding")]
        [SerializeField] private bool _autoBoardPassengers = true;
        [SerializeField] private float _boardingInterval = 5f;
        [SerializeField] private int _passengersPerBoarding = 1;

        private RectTransform _container;
        private readonly List<PassengerUIVisual> _passengers = new();
        private float _boardingTimer;

        public bool HasCapacity => _passengers.Count < _maxPassengers;
        public int CurrentCount => _passengers.Count;
        public int Capacity => _maxPassengers;

        private void Awake()
        {
            _container = GetComponent<RectTransform>();
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
            if (_passengers.Count >= _maxPassengers) return false;

            passenger.transform.SetParent(_container, true);
            passenger.MoveTo(CalculatePosition(_passengers.Count));
            _passengers.Add(passenger);
            return true;
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
        }

        private void ReapplyGridPositions()
        {
            for (int i = 0; i < _passengers.Count; i++)
                _passengers[i].MoveTo(CalculatePosition(i));
        }

        private Vector2 CalculatePosition(int index)
        {
            int cols = Mathf.Max(1, Mathf.FloorToInt(_areaSize.x / _cellSize));
            int row = index / cols;
            int col = index % cols;

            float totalWidth = (cols - 1) * _cellSize;
            float startX = -totalWidth * 0.5f;
            float startY = (_areaSize.y - _cellSize) * 0.5f;

            return new Vector2(
                startX + col * _cellSize,
                startY - row * _cellSize);
        }
    }
}
