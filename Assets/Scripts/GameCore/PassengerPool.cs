using System.Collections.Generic;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class PassengerPool : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PassengerUIVisual _passengerPrefab;
        [SerializeField] private RectTransform _container;

        [Header("Settings")]
        [SerializeField] private int _initialPoolSize = 25;
        [SerializeField] private int _maximumPoolSize;

        private readonly Stack<PassengerUIVisual> _availablePassengers = new();
        private readonly HashSet<PassengerUIVisual> _availableLookup = new();
        private readonly List<PassengerUIVisual> _allPassengers = new();
        private bool _isPrewarmed;

        public int AvailableCount => _availablePassengers.Count;
        public int TotalCount => _allPassengers.Count;
        public int InitialPoolSize => _initialPoolSize;
        public int MaximumPoolSize => _maximumPoolSize;
        public RectTransform Container => _container;

        private void Awake()
        {
            Prewarm();
        }

        private void OnValidate()
        {
            _initialPoolSize = Mathf.Max(0, _initialPoolSize);
            _maximumPoolSize = Mathf.Max(0, _maximumPoolSize);

            if (_maximumPoolSize > 0 && _initialPoolSize > _maximumPoolSize)
                _initialPoolSize = _maximumPoolSize;
        }

        public void Prewarm()
        {
            if (_isPrewarmed) return;

            int targetCount = _initialPoolSize;
            if (_maximumPoolSize > 0)
                targetCount = Mathf.Min(targetCount, _maximumPoolSize);

            while (_allPassengers.Count < targetCount)
            {
                PassengerUIVisual passenger = CreatePassengerInstance();
                if (passenger == null) break;

                StorePassenger(passenger);
            }

            _isPrewarmed = true;
        }

        public bool TryGet(out PassengerUIVisual passenger)
        {
            if (_availablePassengers.Count > 0)
            {
                passenger = _availablePassengers.Pop();
                _availableLookup.Remove(passenger);
                passenger.MarkAsInUse();
                passenger.ResetForReuse();
                return true;
            }

            if (!CanExpand())
            {
                passenger = null;
                return false;
            }

            passenger = CreatePassengerInstance();
            if (passenger == null)
                return false;

            passenger.MarkAsInUse();
            passenger.ResetForReuse();
            _allPassengers.Add(passenger);
            return true;
        }

        public void Release(PassengerUIVisual passenger)
        {
            if (passenger == null) return;
            if (passenger.IsPooled) return;
            if (_availableLookup.Contains(passenger)) return;

            passenger.MarkAsPooled();
            passenger.ResetForReuse();
            passenger.transform.SetParent(GetContainerTransform(), false);
            passenger.gameObject.SetActive(false);

            _availablePassengers.Push(passenger);
            _availableLookup.Add(passenger);
        }

        private PassengerUIVisual CreatePassengerInstance()
        {
            if (_passengerPrefab == null) return null;

            PassengerUIVisual passenger = Instantiate(
                _passengerPrefab,
                GetContainerTransform(),
                false);

            passenger.SetPool(this);
            passenger.MarkAsPooled();
            passenger.ResetForReuse();
            passenger.gameObject.SetActive(false);
            _allPassengers.Add(passenger);
            return passenger;
        }

        private void StorePassenger(PassengerUIVisual passenger)
        {
            if (passenger == null) return;

            passenger.transform.SetParent(GetContainerTransform(), false);
            passenger.gameObject.SetActive(false);
            _availablePassengers.Push(passenger);
            _availableLookup.Add(passenger);
        }

        private bool CanExpand()
        {
            return _maximumPoolSize <= 0 || _allPassengers.Count < _maximumPoolSize;
        }

        private Transform GetContainerTransform()
        {
            return _container != null ? _container : transform;
        }
    }
}
