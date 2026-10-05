using UnityEngine;
using Unity.Cinemachine;

namespace FarmASU.Player
{
    /// <summary>
    /// Manages the orientation and positioning of the camera follow target.
    /// Orbit rotations (yaw and pitch) are applied here so Cinemachine can track it cleanly
    /// without inheriting the player character's body rotation (preventing spinning feedback loops).
    /// </summary>
    public class PlayerCameraTarget : MonoBehaviour
    {
        [Header("Target Offsets")]
        [Tooltip("Height offset from player pivot (chest/shoulder level).")]
        [SerializeField] private float _targetHeight = 1.4f;

        [Header("Sensitivity Settings")]
        [Tooltip("Horizontal look sensitivity (mouse/gamepad).")]
        [SerializeField] private float _mouseSensitivityX = 0.15f;

        [Tooltip("Vertical look sensitivity (mouse/gamepad).")]
        [SerializeField] private float _mouseSensitivityY = 0.12f;

        [Tooltip("Gamepad look sensitivity multiplier.")]
        [SerializeField] private float _gamepadMultiplier = 120.0f;

        [Header("Pitch Limits")]
        [Tooltip("Minimum vertical angle (looking up from below).")]
        [SerializeField] private float _minPitch = -25.0f;

        [Tooltip("Maximum vertical angle (looking down from above).")]
        [SerializeField] private float _maxPitch = 60.0f;

        [Header("Invert Options")]
        [SerializeField] private bool _invertY = false;

        private float _yaw;
        private float _pitch = 15.0f; // Default pleasant downward viewing angle
        private Transform _targetToFollow;

        public void SetTarget(Transform target)
        {
            _targetToFollow = target;
        }

        private void Awake()
        {
            // Initialize yaw with current target rotation
            _yaw = transform.eulerAngles.y;
            _pitch = 15.0f;

            if (transform.parent != null)
            {
                _targetToFollow = transform.parent;
                // Detach from parent so the character's body rotation never twists the camera target!
                transform.SetParent(null);
            }
            else if (_targetToFollow == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _targetToFollow = player.transform;
                }
            }

            UpdateRotation();
        }

        private void Start()
        {
            // Ensure Cinemachine is tracking THIS target and NOT the rotating character mesh/root
            var vcam = FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                if (vcam.Target.TrackingTarget == null || (_targetToFollow != null && vcam.Target.TrackingTarget == _targetToFollow))
                {
                    vcam.Target.TrackingTarget = transform;
                    vcam.Target.LookAtTarget = null;
                    vcam.Target.CustomLookAtTarget = false;
                    Debug.Log("[PlayerCameraTarget] Automatically connected Cinemachine TrackingTarget to CameraTarget.");
                }
            }
        }

        private void LateUpdate()
        {
            if (_targetToFollow == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _targetToFollow = player.transform;
                }
                else
                {
                    return; // Wait safely without destroying self
                }
            }

            // Maintain stable height offset relative to tracked character transform
            transform.position = _targetToFollow.position + Vector3.up * _targetHeight;

            // Always enforce world rotation every frame so character body rotation never drags camera
            UpdateRotation();
        }

        /// <summary>
        /// Feeds look input vector into target rotation angles.
        /// </summary>
        /// <param name="lookInput">Delta look input (X: horizontal, Y: vertical).</param>
        /// <param name="isGamepad">Whether input is from an analog stick requiring delta time scaling.</param>
        public void ProcessLookInput(Vector2 lookInput, bool isGamepad = false)
        {
            if (lookInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float multiplier = isGamepad ? (_gamepadMultiplier * Time.deltaTime) : 1.0f;
            float inputX = lookInput.x * _mouseSensitivityX * multiplier;
            float inputY = lookInput.y * _mouseSensitivityY * multiplier;

            if (_invertY)
            {
                inputY = -inputY;
            }

            _yaw += inputX;
            _pitch -= inputY;

            // Clamp vertical orbit angle so camera never goes beneath ground or flips upside down
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            // Wrap yaw angle to maintain clean float precision
            if (_yaw < 0f) _yaw += 360f;
            if (_yaw >= 360f) _yaw -= 360f;

            UpdateRotation();
        }

        private void UpdateRotation()
        {
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);
        }

        /// <summary>
        /// Exposes current yaw rotation angle for orientation alignment.
        /// </summary>
        public float Yaw => _yaw;

        /// <summary>
        /// Exposes current pitch angle for debugging or vertical offset logic.
        /// </summary>
        public float Pitch => _pitch;
    }
}
