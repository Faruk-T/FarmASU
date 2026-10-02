using System;
using UnityEngine;

namespace FarmASU.Core
{
    /// <summary>
    /// Pure C# runtime state model for game time.
    /// Separated from static configuration and presentation.
    /// Serialized directly for future save/load without interface bloat.
    /// </summary>
    [Serializable]
    public class GameTimeState
    {
        [Tooltip("Current calendar day (1-indexed).")]
        [SerializeField] private int _day = 1;

        [Tooltip("Current hour of the day (0-23).")]
        [SerializeField] private int _hour = 6;

        [Tooltip("Current minute of the hour (0-59).")]
        [SerializeField] private int _minute = 0;

        public GameTimeState()
        {
            _day = 1;
            _hour = 6;
            _minute = 0;
        }

        public GameTimeState(int day, int hour, int minute)
        {
            _day = Mathf.Max(1, day);
            _hour = Mathf.Clamp(hour, 0, 23);
            _minute = Mathf.Clamp(minute, 0, 59);
        }

        public int Day
        {
            get => _day;
            set => _day = Mathf.Max(1, value);
        }

        public int Hour
        {
            get => _hour;
            set => _hour = Mathf.Clamp(value, 0, 23);
        }

        public int Minute
        {
            get => _minute;
            set => _minute = Mathf.Clamp(value, 0, 59);
        }

        /// <summary>
        /// Total elapsed game minutes calculated from Day 1 at 00:00.
        /// Useful for linear delta time calculations across days.
        /// </summary>
        public int TotalGameMinutes => ((_day - 1) * 1440) + (_hour * 60) + _minute;

        /// <summary>
        /// Creates an isolated deep copy of the current state.
        /// </summary>
        public GameTimeState Clone()
        {
            return new GameTimeState(_day, _hour, _minute);
        }

        public override string ToString()
        {
            return $"Day {_day} - {_hour:D2}:{_minute:D2}";
        }
    }
}
