using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleAirport.GameCore.Prestige
{
    public sealed class PrestigeScannerVisualPassengerLoop : MonoBehaviour
    {
        [SerializeField] private PassengerQueueUIController _passengerQueue;
        [SerializeField] private RectTransform _entryPoint;
        [SerializeField] private RectTransform _scanPoint;
        [SerializeField] private RectTransform _exitPoint;
        [SerializeField] private RectTransform _passengerRoot;
        [SerializeField] private GameObject _passengerVisualPrefab;
        [SerializeField] private int _visiblePassengerCount = 2;
        [SerializeField] private float _spawnInterval = 0.8f;
        [SerializeField] private float _moveDuration = 1.2f;
        [SerializeField] private bool _playOnEnable = true;

        private readonly Queue<RectTransform> _availablePassengers = new();
        private readonly List<RectTransform> _allPassengers = new();
        private Coroutine _spawnRoutine;

        private void Awake()
        {
            if (_passengerQueue == null)
                _passengerQueue = FindFirstObjectByType<PassengerQueueUIController>();

            EnsurePassengerRoot();
            BuildPool();
        }

        private void OnEnable()
        {
            HideAllPassengers();

            if (_playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            StopLoop();
            HideAllPassengers();
        }

        public void Play()
        {
            if (_spawnRoutine != null)
                return;

            EnsurePassengerRoot();
            BuildPool();
            _spawnRoutine = StartCoroutine(SpawnLoop());
        }

        public void StopLoop()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            StopAllCoroutines();
        }

        private IEnumerator SpawnLoop()
        {
            while (isActiveAndEnabled)
            {
                if (!HasVisibleQueuePassengers())
                {
                    yield return new WaitForSeconds(Mathf.Max(0.05f, _spawnInterval));
                    continue;
                }

                RectTransform passenger = GetPassengerFromPool();
                if (passenger != null)
                {
                    StartCoroutine(AnimatePassenger(passenger));
                }

                yield return new WaitForSeconds(Mathf.Max(0.05f, _spawnInterval));
            }
        }

        private IEnumerator AnimatePassenger(RectTransform passenger)
        {
            passenger.gameObject.SetActive(true);
            passenger.SetParent(GetPassengerRoot(), false);
            passenger.anchoredPosition = GetLocalPoint(_entryPoint);

            yield return MovePassenger(passenger, GetLocalPoint(_entryPoint), GetLocalPoint(_scanPoint), _moveDuration * 0.45f);
            yield return new WaitForSeconds(0.12f);
            yield return MovePassenger(passenger, GetLocalPoint(_scanPoint), GetLocalPoint(_exitPoint), _moveDuration * 0.55f);

            passenger.gameObject.SetActive(false);
            _availablePassengers.Enqueue(passenger);
        }

        private IEnumerator MovePassenger(RectTransform passenger, Vector2 from, Vector2 to, float duration)
        {
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                passenger.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }

            passenger.anchoredPosition = to;
        }

        private bool HasVisibleQueuePassengers()
        {
            return _passengerQueue == null || _passengerQueue.ActivePassengerCount > 0;
        }

        private RectTransform GetPassengerFromPool()
        {
            if (_availablePassengers.Count == 0)
                return null;

            return _availablePassengers.Dequeue();
        }

        private void BuildPool()
        {
            int targetCount = Mathf.Max(1, _visiblePassengerCount);
            while (_allPassengers.Count < targetCount)
            {
                RectTransform passenger = CreatePassengerVisual();
                if (passenger == null)
                    return;

                passenger.gameObject.SetActive(false);
                _allPassengers.Add(passenger);
                _availablePassengers.Enqueue(passenger);
            }
        }

        private RectTransform CreatePassengerVisual()
        {
            GameObject visual;
            if (_passengerVisualPrefab != null)
            {
                visual = Instantiate(_passengerVisualPrefab, GetPassengerRoot(), false);
            }
            else
            {
                visual = new GameObject("DecorativePassenger", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                visual.transform.SetParent(GetPassengerRoot(), false);
                UnityEngine.UI.Image image = visual.GetComponent<UnityEngine.UI.Image>();
                image.raycastTarget = false;
                image.color = new Color(0.96f, 0.78f, 0.32f, 1f);
            }

            visual.name = "DecorativePassenger";
            RectTransform rectTransform = visual.GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = visual.AddComponent<RectTransform>();

            if (rectTransform.sizeDelta == Vector2.zero)
                rectTransform.sizeDelta = new Vector2(18f, 18f);

            return rectTransform;
        }

        private void HideAllPassengers()
        {
            _availablePassengers.Clear();
            for (int i = 0; i < _allPassengers.Count; i++)
            {
                RectTransform passenger = _allPassengers[i];
                if (passenger == null)
                    continue;

                passenger.gameObject.SetActive(false);
                _availablePassengers.Enqueue(passenger);
            }
        }

        private void EnsurePassengerRoot()
        {
            if (_passengerRoot != null)
                return;

            RectTransform rectTransform = transform as RectTransform;
            _passengerRoot = rectTransform;
        }

        private RectTransform GetPassengerRoot()
        {
            EnsurePassengerRoot();
            return _passengerRoot;
        }

        private Vector2 GetLocalPoint(RectTransform point)
        {
            if (point == null)
                return Vector2.zero;

            return point.anchoredPosition;
        }
    }
}