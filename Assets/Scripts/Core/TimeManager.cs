using System;
using UnityEngine;

namespace FarmASU.Core
{
    /// <summary>
    /// Central game clock engine for FarmASU.
    /// Manages calendar progression, configurable time scaling, pause/resume,
    /// and dispatches discrete minute/hour/day change events.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Static designer configuration for time scaling and initial calendar.")]
        [SerializeField] private GameTimeConfig _config;

        [Header("Runtime State (Debug Only)")]
        [Tooltip("Active calendar state. Initialized from config and updated deterministically.")]
        [SerializeField] private GameTimeState _currentState;

        [Tooltip("Whether the game clock is currently paused.")]
        [SerializeField] private bool _isPaused = false;

        [Tooltip("Current speed multiplier (1x, 2x, 5x, etc.).")]
        [SerializeField] private float _speedMultiplier = 1.0f;

        // Internal fractional accumulator to preserve partial frame times
        private float _fractionalMinuteAccumulator;

        // Time Events
        /// <summary>Invoked whenever the minute advances. Parameters: (minute 0-59, hour 0-23)</summary>
        public event Action<int, int> OnMinuteChanged;

        /// <summary>Invoked whenever the hour advances. Parameters: (hour 0-23, day 1+)</summary>
        public event Action<int, int> OnHourChanged;

        /// <summary>Invoked whenever midnight passes and a new day starts. Parameter: (day 1+)</summary>
        public event Action<int> OnDayChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[TimeManager] Duplicate instance detected on {gameObject.name}. Destroying duplicate component.");
                Destroy(this);
                return;
            }

            Instance = this;
            InitializeState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_isPaused)
            {
                return;
            }

            AdvanceRealSeconds(Time.deltaTime);
        }

        /// <summary>
        /// Initializes calendar state from the assigned configuration.
        /// </summary>
        public void InitializeState()
        {
            if (_config != null)
            {
                _currentState = new GameTimeState(_config.InitialDay, _config.InitialHour, _config.InitialMinute);
                _speedMultiplier = _config.DefaultSpeedMultiplier;
            }
            else
            {
                _currentState = new GameTimeState(1, 6, 0);
                _speedMultiplier = 1.0f;
            }

            _fractionalMinuteAccumulator = 0.0f;
        }

        /// <summary>
        /// Injects real elapsed seconds and converts them to game minutes via the configured ratio.
        /// Fully frame-rate independent and handles arbitrary time deltas cleanly.
        /// </summary>
        public void AdvanceRealSeconds(float realSeconds)
        {
            if (realSeconds <= 0.0f)
            {
                return;
            }

            float secondsPerMinute = (_config != null) ? _config.RealSecondsPerGameMinute : 1.0f;
            float scaledSeconds = realSeconds * _speedMultiplier;

            _fractionalMinuteAccumulator += scaledSeconds;

            while (_fractionalMinuteAccumulator >= secondsPerMinute)
            {
                _fractionalMinuteAccumulator -= secondsPerMinute;
                StepOneMinute();
            }
        }

        /// <summary>
        /// Steps exactly one calendar minute forward and triggers relevant events.
        /// </summary>
        private void StepOneMinute()
        {
            int nextMinute = _currentState.Minute + 1;
            bool hourChanged = false;
            bool dayChanged = false;

            if (nextMinute >= 60)
            {
                nextMinute = 0;
                int nextHour = _currentState.Hour + 1;

                if (nextHour >= 24)
                {
                    nextHour = 0;
                    _currentState.Day++;
                    dayChanged = true;
                }

                _currentState.Hour = nextHour;
                hourChanged = true;
            }

            _currentState.Minute = nextMinute;

            // Dispatch events in hierarchical order: Day -> Hour -> Minute
            if (dayChanged)
            {
                OnDayChanged?.Invoke(_currentState.Day);
            }

            if (hourChanged)
            {
                OnHourChanged?.Invoke(_currentState.Hour, _currentState.Day);
            }

            OnMinuteChanged?.Invoke(_currentState.Minute, _currentState.Hour);
        }

        #region Public Control API

        /// <summary>
        /// Current calendar day (1-indexed).
        /// </summary>
        public int Day => _currentState != null ? _currentState.Day : 1;

        /// <summary>
        /// Current hour of the day (0-23).
        /// </summary>
        public int Hour => _currentState != null ? _currentState.Hour : 6;

        /// <summary>
        /// Current minute of the hour (0-59).
        /// </summary>
        public int Minute => _currentState != null ? _currentState.Minute : 0;

        /// <summary>
        /// Returns whether the game clock is currently paused.
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Returns the active speed multiplier.
        /// </summary>
        public float SpeedMultiplier => _speedMultiplier;

        /// <summary>
        /// Returns an isolated copy of the current calendar state.
        /// </summary>
        public GameTimeState CurrentState => _currentState != null ? _currentState.Clone() : new GameTimeState();

        /// <summary>
        /// Pauses the game clock. Time will not advance.
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// Resumes the game clock from where it was paused.
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Sets pause state directly.
        /// </summary>
        public void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;
        }

        /// <summary>
        /// Sets the speed multiplier. Clamped between 0.1x and 100.0x.
        /// Rejects non-positive values.
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            if (multiplier <= 0.0f)
            {
                Debug.LogWarning($"[TimeManager] Invalid speed multiplier '{multiplier}'. Speed must be strictly positive.");
                return;
            }

            _speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 100.0f);
        }

        /// <summary>
        /// Directly sets calendar state (used for save loading, fast-forwarding, or tests).
        /// </summary>
        public void SetState(GameTimeState newState)
        {
            if (newState == null) return;
            _currentState = newState.Clone();
            _fractionalMinuteAccumulator = 0.0f;
        }

        /// <summary>
        /// Sets the configuration asset at runtime if needed.
        /// </summary>
        public void SetConfig(GameTimeConfig config)
        {
            _config = config;
        }

        #endregion
    }
}
