using FarmASU.Core;
using FarmASU.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmASU.UI
{
    /// <summary>
    /// Displays a sleek, floating HUD interaction prompt when the player is near an interactable object.
    /// Shows the required key [E] and the contextual interaction description.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractionDetector _detector;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _actionText;
        [SerializeField] private TextMeshProUGUI _keyBadgeText;
        [SerializeField] private Image _promptBackground;

        [Header("Animation")]
        [SerializeField] private float _fadeSpeed = 12.0f;

        private bool _isShowing;

        private void Start()
        {
            if (_detector == null)
            {
                _detector = FindAnyObjectByType<PlayerInteractionDetector>();
            }

            if (_detector != null)
            {
                _detector.OnTargetChanged += OnTargetChanged;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            if (_detector != null)
            {
                _detector.OnTargetChanged -= OnTargetChanged;
            }
        }

        private void Update()
        {
            // Smooth fade in / out
            if (_canvasGroup != null)
            {
                float targetAlpha = _isShowing ? 1.0f : 0.0f;
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime * _fadeSpeed);
            }
        }

        private void OnTargetChanged(IInteractable target)
        {
            if (target != null && target.CanInteract(gameObject))
            {
                string prompt = target.InteractionPrompt;
                if (!string.IsNullOrEmpty(prompt))
                {
                    if (_actionText != null)
                    {
                        // Clean prompt text (e.g. remove trailing [E] if present in string)
                        string cleaned = prompt.Replace("[E]", "").Trim();
                        _actionText.text = cleaned;
                    }

                    if (_keyBadgeText != null)
                    {
                        _keyBadgeText.text = "E";
                    }

                    _isShowing = true;
                    return;
                }
            }

            _isShowing = false;
        }
    }
}
