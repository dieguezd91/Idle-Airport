using UnityEngine;

namespace IdleAirport.GameCore.Prestige
{
    public sealed class AirportPrestigeVisualApplier : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardingAreaRect;
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private PassengerProcessor _passengerProcessor;

        private void Awake()
        {
            AutoWireReferences();
            ApplyPrestigeVisuals(0);
        }

        public void ApplyPrestigeVisuals(int prestigeCount)
        {
            AutoWireReferences();

            if (_passengerProcessor != null)
                _passengerProcessor.RefreshManualScannerUnlocks(prestigeCount);

            if (_waitingRoom != null)
                _waitingRoom.ApplyPrestigeBoardingLayout(prestigeCount > 0);
        }

        private void AutoWireReferences()
        {
            if (_passengerProcessor == null)
                _passengerProcessor = FindFirstObjectByType<PassengerProcessor>();

            if (_waitingRoom == null)
                _waitingRoom = FindFirstObjectByType<WaitingRoomUIController>();

            if (_boardingAreaRect == null)
            {
                GameObject boarding = GameObject.Find("BoardingLoungeSection");
                if (boarding != null)
                    _boardingAreaRect = boarding.GetComponent<RectTransform>();

                if (_boardingAreaRect == null && _waitingRoom != null)
                    _boardingAreaRect = _waitingRoom.GetComponent<RectTransform>();
            }
        }
    }
}