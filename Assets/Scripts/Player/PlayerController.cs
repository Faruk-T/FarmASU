using FarmASU.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmASU.Player
{
    /// <summary>
    /// Core third-person player controller.
    /// Handles camera-relative physical locomotion, smooth rotation, acceleration, and grounding.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Tuning")]
        [Tooltip("Maximum movement speed in meters per second.")]
        [SerializeField] private float _moveSpeed = 4.5f;

        [Tooltip("Acceleration rate towards maximum speed.")]
        [SerializeField] private float _acceleration = 14.0f;

        [Tooltip("Deceleration rate when stopping.")]
        [SerializeField] private float _deceleration = 18.0f;

        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float _rotationSpeed = 720.0f;

        [Header("Grounding & Gravity")]
        [Tooltip("Gravity acceleration applied when falling.")]
        [SerializeField] private float _gravity = -18.0f;

        [Tooltip("Constant downward force applied while grounded to stick to uneven terrain.")]
        [SerializeField] private float _groundedStickForce = -2.5f;

        [Header("Component References")]
        [Tooltip("Reference to the camera follow target script.")]
        [SerializeField] private PlayerCameraTarget _cameraTarget;

        [Tooltip("Main camera transform used for directional reference. Defaults to Camera.main.")]
        [SerializeField] private Transform _cameraTransform;

        // Internal State
        private CharacterController _characterController;
        private FarmASUInputActions _inputActions;
        private float _currentSpeed;
        private float _verticalVelocity;
        private Vector2 _moveInput;
        private Vector2 _lookInput;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            if (_cameraTarget == null)
            {
                _cameraTarget = GetComponentInChildren<PlayerCameraTarget>();
            }

            _inputActions = new FarmASUInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            _inputActions.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            ReadInput();
            HandleLook();
            HandleMovement();
        }

        private void ReadInput()
        {
            _moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
            _lookInput = _inputActions.Player.Look.ReadValue<Vector2>();

            // Normalize diagonal movement vector to prevent faster movement on diagonals
            if (_moveInput.sqrMagnitude > 1.0f)
            {
                _moveInput.Normalize();
            }
        }

        private void HandleLook()
        {
            if (_cameraTarget == null)
            {
                return;
            }

            bool isGamepad = Gamepad.current != null &&
                             Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.001f;

            _cameraTarget.ProcessLookInput(_lookInput, isGamepad);
        }

        private void HandleMovement()
        {
            // 1. Resolve camera-relative horizontal movement direction
            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;

            if (_cameraTransform != null)
            {
                camForward = _cameraTransform.forward;
                camForward.y = 0.0f;
                camForward.Normalize();

                camRight = _cameraTransform.right;
                camRight.y = 0.0f;
                camRight.Normalize();
            }

            Vector3 targetDirection = (camForward * _moveInput.y + camRight * _moveInput.x);
            bool hasMovementInput = _moveInput.sqrMagnitude > 0.01f;

            // 2. Smoothly accelerate / decelerate speed
            float targetSpeed = hasMovementInput ? _moveSpeed : 0.0f;
            float accelRate = targetSpeed > _currentSpeed ? _acceleration : _deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accelRate * Time.deltaTime);

            // 3. Rotate character towards movement direction
            if (hasMovementInput && targetDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }

            // 4. Grounding and Gravity calculation
            if (_characterController.isGrounded)
            {
                _verticalVelocity = _groundedStickForce;
            }
            else
            {
                _verticalVelocity += _gravity * Time.deltaTime;
            }

            // 5. Construct final physical motion
            Vector3 horizontalVelocity = transform.forward * _currentSpeed;
            Vector3 motion = (horizontalVelocity * Time.deltaTime) + (Vector3.up * (_verticalVelocity * Time.deltaTime));

            _characterController.Move(motion);
        }

        /// <summary>
        /// Exposes current horizontal movement speed for animation or debugging.
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>
        /// Exposes grounded status directly from the CharacterController.
        /// </summary>
        public bool IsGrounded => _characterController.isGrounded;
    }
}
