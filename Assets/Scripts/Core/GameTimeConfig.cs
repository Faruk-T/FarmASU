using UnityEngine;

namespace FarmASU.Core
{
    /// <summary>
    /// Static design configuration for the Game Clock.
    /// This ScriptableObject holds read-only designer parameters and must NEVER mutate at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "GameTimeConfig", menuName = "FarmASU/Config/GameTimeConfig")]
    public class GameTimeConfig : ScriptableObject
    {
        [Header("Time Scale Ratio")]
        [Tooltip("How many real seconds correspond to one game minute. Default: 1.0 (1 real sec = 1 game min).")]
        [SerializeField] private float _realSecondsPerGameMinute = 1.0f;

        [Header("Initial Calendar Settings")]
        [Tooltip("Starting day number when a new game starts. Must be 1 or higher.")]
        [SerializeField] private int _initialDay = 1;

        [Tooltip("Starting hour (0-23). Default is 6 (06:00 dawn).")]
        [Range(0, 23)]
        [SerializeField] private int _initialHour = 6;

        [Tooltip("Starting minute (0-59). Default is 0.")]
        [Range(0, 59)]
        [SerializeField] private int _initialMinute = 0;

        [Header("Default Playback")]
        [Tooltip("Default speed multiplier applied at launch. Must be positive.")]
        [SerializeField] private float _defaultSpeedMultiplier = 1.0f;

        // Read-only accessors enforcing validation
        public float RealSecondsPerGameMinute => Mathf.Max(0.01f, _realSecondsPerGameMinute);
        public int InitialDay => Mathf.Max(1, _initialDay);
        public int InitialHour => Mathf.Clamp(_initialHour, 0, 23);
        public int InitialMinute => Mathf.Clamp(_initialMinute, 0, 59);
        public float DefaultSpeedMultiplier => Mathf.Max(0.1f, _defaultSpeedMultiplier);
    }
}
