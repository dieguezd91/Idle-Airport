using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace IdleAirport.GameCore
{
    public sealed class ScannerStationUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _scannerPoint;

        [Header("Settings")]
        [SerializeField] private float _processingDuration = 1f;
        [SerializeField] private int _maxHeldCount = 5;

        [Header("Mode")]
        [SerializeField] private bool _isAutoScanner;

        private readonly List<PassengerUIVisual> _heldPassengers = new();
        private PassengerUIVisual _activeAutoPassenger;
        private bool _isBusy;

        public bool IsBusy => _isAutoScanner && _isBusy;
        public int HeldCount => _heldPassengers.Count;
        public bool CanAcceptMore => _isAutoScanner || _heldPassengers.Count < _maxHeldCount;
        public float ProcessingDuration => _processingDuration;
        public bool IsOperational => isActiveAndEnabled && gameObject.activeInHierarchy;
        public bool HasManualPassengerReady => !_isAutoScanner && _heldPassengers.Count > 0;
        public Vector3 FeedbackWorldPosition => _scannerPoint != null ? _scannerPoint.position : transform.position;

        public bool TryHoldPassenger(PassengerUIVisual passenger)
        {
            if (passenger == null) return false;
            if (_isAutoScanner) return false;
            if (_heldPassengers.Count >= _maxHeldCount) return false;

            passenger.transform.SetParent(transform, true);
            passenger.SetPositionImmediate(_scannerPoint.anchoredPosition + new Vector2(0, -_heldPassengers.Count * 45f));
            _heldPassengers.Add(passenger);
            return true;
        }

        public bool TryReleaseOneHeldPassenger(out PassengerUIVisual passenger)
        {
            if (_heldPassengers.Count == 0)
            {
                passenger = null;
                return false;
            }

            passenger = _heldPassengers[0];
            _heldPassengers.RemoveAt(0);

            for (int i = 0; i < _heldPassengers.Count; i++)
                _heldPassengers[i].MoveTo(_scannerPoint.anchoredPosition + new Vector2(0, -(i + 1) * 45f));

            return true;
        }

        public bool TryStartAutoProcessing(PassengerUIVisual passenger, Action<PassengerUIVisual> onCompleted)
        {
            if (passenger == null) return false;
            if (!IsOperational) return false;
            if (!_isAutoScanner || _isBusy) return false;

            StartCoroutine(AutoProcessRoutine(passenger, onCompleted));
            return true;
        }

        public void SetAutoProcessingDuration(float duration)
        {
            if (!_isAutoScanner) return;

            _processingDuration = Mathf.Max(0.01f, duration);
        }

        public int RecycleHeldPassengers()
        {
            int count = _heldPassengers.Count;

            for (int i = 0; i < _heldPassengers.Count; i++)
                _heldPassengers[i].Recycle();

            _heldPassengers.Clear();
            return count;
        }

        public bool CancelAutoProcessing()
        {
            if (!_isAutoScanner || !_isBusy || _activeAutoPassenger == null) return false;

            StopAllCoroutines();
            _activeAutoPassenger.Recycle();
            _activeAutoPassenger = null;
            _isBusy = false;
            return true;
        }

        private IEnumerator AutoProcessRoutine(PassengerUIVisual passenger, Action<PassengerUIVisual> onCompleted)
        {
            _isBusy = true;
            _activeAutoPassenger = passenger;

            passenger.transform.SetParent(transform, true);
            passenger.SetPositionImmediate(_scannerPoint.anchoredPosition);

            yield return new WaitForSeconds(_processingDuration);

            onCompleted?.Invoke(passenger);
            if (_activeAutoPassenger == passenger)
                _activeAutoPassenger = null;
            _isBusy = false;
        }
    }
}
