using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleAirport.GameCore.Prestige
{
    public sealed class AirportPrestigeVisualApplier : MonoBehaviour
    {
        [SerializeField] private AirportPrestigeService _prestigeService;
        [SerializeField] private RectTransform _boardingAreaRect;
        [SerializeField] private WaitingRoomUIController _waitingRoom;
        [SerializeField] private PassengerProcessor _passengerProcessor;
        [SerializeField] private List<AirportPrestigePalette> _palettes = new();
        [SerializeField] private float _paletteTransitionDuration = 0.25f;
        [SerializeField] private List<AirportPrestigeAreaView> _areaViews = new();
        [SerializeField] private RectTransform _entranceRoot;
        [SerializeField] private RectTransform _securityRoot;
        [SerializeField] private RectTransform _shopsRoot;
        [SerializeField] private RectTransform _boardingRoot;
        private Coroutine _paletteTransition;

        private void Awake()
        {
            ResolveService();
            if (_prestigeService == null || !SetupAreaViews()) enabled = false;
        }

        private void OnEnable()
        {
            if (_prestigeService == null)
            {
                Debug.LogError($"[{nameof(AirportPrestigeVisualApplier)}] Missing {nameof(AirportPrestigeService)} reference.", this);
                enabled = false;
                return;
            }
            _prestigeService.PrestigeCompleted -= HandlePrestigeCompleted;
            _prestigeService.PrestigeCompleted += HandlePrestigeCompleted;
            ApplyPrestigeVisuals(_prestigeService.PrestigeCount, false);
        }

        private void OnDisable()
        {
            CancelRunningTransition();
            if (_prestigeService != null) _prestigeService.PrestigeCompleted -= HandlePrestigeCompleted;
        }

        private bool SetupAreaViews()
        {
            if (_areaViews == null) _areaViews = new List<AirportPrestigeAreaView>();
            if (_areaViews.Count == 0)
            {
                RectTransform[] roots = { _entranceRoot, _securityRoot, _shopsRoot, _boardingRoot };
                for (int i = 0; i < roots.Length; i++)
                {
                    AirportPrestigeAreaView view = roots[i] != null
                        ? roots[i].GetComponentInChildren<AirportPrestigeAreaView>(true)
                        : null;
                    if (view == null)
                    {
                        Debug.LogError($"[{nameof(AirportPrestigeVisualApplier)}] Area view {i} is not configured on the structural area prefab.", this);
                        return false;
                    }
                    _areaViews.Add(view);
                }
            }
            return _areaViews.Count == 4;
        }

        private void ApplyPrestigeVisuals(int prestigeCount, bool animate)
        {
            if (_passengerProcessor != null) _passengerProcessor.RefreshManualScannerUnlocks(prestigeCount);
            if (_waitingRoom != null) _waitingRoom.ApplyPrestigeBoardingLayout(prestigeCount > 0);
            AirportPrestigePalette palette = ResolvePalette(prestigeCount);
            if (palette == null || _areaViews == null || _areaViews.Count != 4) return;
            CancelRunningTransition();
            if (!animate || _paletteTransitionDuration <= 0f) { ApplyPaletteImmediate(palette); return; }
            for (int i = 0; i < _areaViews.Count; i++) _areaViews[i].BeginTransition();
            _paletteTransition = StartCoroutine(TransitionAreaPalettes(palette, _paletteTransitionDuration));
        }

        private void HandlePrestigeCompleted(int prestigeCount, double multiplier) { ApplyPrestigeVisuals(prestigeCount, true); }

        private void ApplyPaletteImmediate(AirportPrestigePalette palette)
        {
            AirportPrestigeAreaPalette[] areas = { palette.Entrance, palette.Security, palette.Shops, palette.Boarding };
            for (int i = 0; i < _areaViews.Count; i++) _areaViews[i].ApplyImmediate(areas[i]);
        }

        private IEnumerator TransitionAreaPalettes(AirportPrestigePalette palette, float duration)
        {
            AirportPrestigeAreaPalette[] areas = { palette.Entrance, palette.Security, palette.Shops, palette.Boarding };
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < _areaViews.Count; i++) _areaViews[i].ApplyTransition(areas[i], t);
                yield return null;
            }
            for (int i = 0; i < _areaViews.Count; i++) _areaViews[i].ApplyImmediate(areas[i]);
            _paletteTransition = null;
        }

        private AirportPrestigePalette ResolvePalette(int prestigeCount)
        {
            if (_palettes == null || _palettes.Count == 0) return null;
            return _palettes[Mathf.Max(0, prestigeCount) % _palettes.Count];
        }

        private void CancelRunningTransition()
        {
            if (_paletteTransition == null) return;
            StopCoroutine(_paletteTransition);
            _paletteTransition = null;
        }

        private void ResolveService()
        {
            if (_prestigeService == null) _prestigeService = GetComponent<AirportPrestigeService>();
        }
    }
}
