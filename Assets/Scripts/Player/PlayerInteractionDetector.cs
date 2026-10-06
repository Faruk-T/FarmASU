using System;
using FarmASU.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmASU.Player
{
    /// <summary>
    /// Scans the player's immediate physical surroundings for interactable objects.
    /// Uses sphere overlap with forward viewing cone filtering to select the optimal target.
    /// Responds to the Player.Interact input action.
    /// </summary>
    public class PlayerInteractionDetector : MonoBehaviour
    {
        [Header("Detection Geometry")]
        [Tooltip("Maximum interaction radius in meters.")]
        [SerializeField] private float _interactionRadius = 2.2f;

        [Tooltip("Forward offset of the detection sphere center relative to the player.")]
        [SerializeField] private float _forwardOffset = 0.6f;

        [Tooltip("Height offset of the detection sphere center from player feet.")]
        [SerializeField] private float _heightOffset = 0.5f;

        [Tooltip("Maximum viewing angle cone in degrees (half-angle from player forward).")]
        [Range(30f, 90f)]
        [SerializeField] private float _maxViewingAngle = 65.0f;

        [Header("Layer Filtering")]
        [Tooltip("Layer mask for interactable physics colliders.")]
        [SerializeField] private LayerMask _interactableLayer = ~0;

        [Header("Debug Display")]
        [Tooltip("Whether to draw an OnGUI interaction prompt on screen.")]
        [SerializeField] private bool _showDebugPrompt = true;

        private readonly Collider[] _overlapResults = new Collider[16];
        private IInteractable _currentTarget;
        private FarmASUInputActions _inputActions;

        /// <summary>
        /// Currently targeted interactable object, or null if none in range.
        /// </summary>
        public IInteractable CurrentTarget => _currentTarget;

        /// <summary>
        /// Event fired whenever the active interactable target changes.
        /// </summary>
        public event Action<IInteractable> OnTargetChanged;

        private void Awake()
        {
            _inputActions = new FarmASUInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Enable();
            _inputActions.Player.Interact.performed += OnInteractInput;
        }

        private void OnDisable()
        {
            _inputActions.Player.Interact.performed -= OnInteractInput;
            _inputActions.Disable();
            SetTarget(null);
        }

        private void Update()
        {
            ScanForInteractables();
        }

        /// <summary>
        /// Performs sphere overlap and determines the highest priority interactable.
        /// </summary>
        public void ScanForInteractables()
        {
            Vector3 scanCenter = transform.position + (Vector3.up * _heightOffset) + (transform.forward * _forwardOffset);
            int hitCount = Physics.OverlapSphereNonAlloc(scanCenter, _interactionRadius, _overlapResults, _interactableLayer, QueryTriggerInteraction.Collide);

            IInteractable bestTarget = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapResults[i];
                if (col == null || col.gameObject == gameObject)
                {
                    continue;
                }

                // Search for IInteractable on collider or its root
                IInteractable interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract(gameObject))
                {
                    continue;
                }

                // Direction and angle from player facing vector
                Vector3 toTarget = col.transform.position - transform.position;
                toTarget.y = 0.0f; // Horizontal projection
                float horizontalDist = toTarget.magnitude;

                if (horizontalDist < 0.001f)
                {
                    // Directly on top of player
                    bestTarget = interactable;
                    break;
                }

                Vector3 targetDir = toTarget / horizontalDist;
                float dot = Vector3.Dot(transform.forward, targetDir);
                float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

                if (angle > _maxViewingAngle)
                {
                    continue;
                }

                // Weighted score: prefer closer objects, penalize wider angles slightly
                float score = horizontalDist * (1.0f + (1.0f - dot) * 0.5f);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = interactable;
                }
            }

            SetTarget(bestTarget);
        }

        private void SetTarget(IInteractable newTarget)
        {
            if (_currentTarget != newTarget)
            {
                _currentTarget = newTarget;
                OnTargetChanged?.Invoke(_currentTarget);
            }
        }

        private void OnInteractInput(InputAction.CallbackContext context)
        {
            TriggerInteract();
        }

        /// <summary>
        /// Interacts with the current target if available.
        /// Can also be called directly by tests or AI.
        /// </summary>
        public bool TriggerInteract()
        {
            if (_currentTarget != null && _currentTarget.CanInteract(gameObject))
            {
                // Trigger pickup bending animation on character
                PlayerController pc = GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.PlayPickUpAnimation();
                }
                else
                {
                    Animator anim = GetComponentInChildren<Animator>();
                    if (anim != null) anim.SetTrigger("PickUp");
                }

                _currentTarget.Interact(gameObject);
                // Immediately re-scan to update prompt if target was destroyed or consumed
                ScanForInteractables();
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            if (!_showDebugPrompt || _currentTarget == null)
            {
                return;
            }

            string promptText = _currentTarget.InteractionPrompt;
            if (string.IsNullOrEmpty(promptText))
            {
                return;
            }

            // Draw a clean centered prompt label near the bottom-middle of the screen
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;

            float width = 300f;
            float height = 38f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 120f;

            GUI.Box(new Rect(x, y, width, height), promptText, style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.3f);
            Vector3 center = transform.position + (Vector3.up * _heightOffset) + (transform.forward * _forwardOffset);
            Gizmos.DrawWireSphere(center, _interactionRadius);

            // Draw viewing cone boundaries
            Gizmos.color = Color.yellow;
            Vector3 leftBoundary = Quaternion.Euler(0, -_maxViewingAngle, 0) * transform.forward * _interactionRadius;
            Vector3 rightBoundary = Quaternion.Euler(0, _maxViewingAngle, 0) * transform.forward * _interactionRadius;
            Gizmos.DrawRay(transform.position + Vector3.up * _heightOffset, leftBoundary);
            Gizmos.DrawRay(transform.position + Vector3.up * _heightOffset, rightBoundary);
        }
    }
}
