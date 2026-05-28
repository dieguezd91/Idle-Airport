using System.Collections;
using System.Collections.Generic;
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
        private bool _isBusy;

        public bool IsBusy => _isAutoScanner && _isBusy;
        public int HeldCount => _heldPassengers.Count;
        public bool CanAcceptMore => _isAutoScanner || _heldPassengers.Count < _maxHeldCount;

        public void ProcessPassenger(PassengerUIVisual passenger, WaitingRoomUIController waitingRoom)
        {
            if (passenger == null) return;

            if (_isAutoScanner)
            {
                if (waitingRoom == null) return;
                if (!waitingRoom.HasCapacity) return;
                StartCoroutine(AutoProcessRoutine(passenger, waitingRoom));
            }
            else
            {
                passenger.transform.SetParent(transform, true);
                passenger.MoveTo(_scannerPoint.anchoredPosition + new Vector2(0, -_heldPassengers.Count * 45f));
                _heldPassengers.Add(passenger);
            }
        }

        public bool TryReleaseOneToWaitingRoom(WaitingRoomUIController waitingRoom)
        {
            if (waitingRoom == null) return false;
            if (_heldPassengers.Count == 0) return false;
            if (!waitingRoom.HasCapacity) return false;

            PassengerUIVisual passenger = _heldPassengers[0];
            _heldPassengers.RemoveAt(0);

            for (int i = 0; i < _heldPassengers.Count; i++)
                _heldPassengers[i].MoveTo(_scannerPoint.anchoredPosition + new Vector2(0, -(i + 1) * 45f));

            return waitingRoom.TryReceivePassenger(passenger);
        }

        private IEnumerator AutoProcessRoutine(PassengerUIVisual passenger, WaitingRoomUIController waitingRoom)
        {
            _isBusy = true;

            passenger.transform.SetParent(transform, true);
            passenger.MoveTo(_scannerPoint.anchoredPosition);

            yield return new WaitForSeconds(_processingDuration);

            waitingRoom.TryReceivePassenger(passenger);
            _isBusy = false;
        }
    }
}
