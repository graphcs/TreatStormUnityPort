using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Entities
{
    /// <summary>
    /// Code-driven animation controller that swaps SpriteRenderer.sprite
    /// based on PlayerController state. Mirrors PyGame AnimationController.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public enum AnimationState
        {
            Idle,
            Run,
            Eat,
            ChiliReaction,
            FaceCamera,
            FaceCameraFlight
        }

        // Timing constants (from PyGame SpriteSheetLoader)
        private const float RunFrameDuration = 0.1f;       // 10 FPS
        private const float EatFrameDuration = 0.12f;       // ~8.3 FPS
        private const float EatAnimationDuration = 0.4f;    // Total eat time
        private const float FaceCameraFrameDuration = 0.1f;  // Face camera timing

        [SerializeField] private PlayerController playerController;

        // State
        private AnimationState _state = AnimationState.Idle;
        private AnimationState? _manualOverride;
        private int _currentFrame;
        private float _frameTimer;
        private float _eatTimer;
        private float _chiliFrameDuration;
        private float _chiliTimer;

        // Cached
        private SpriteRenderer _spriteRenderer;
        private CharacterSO _characterData;
        private Sprite[] _currentFrames;
        private Sprite[] _cachedFaceCameraFlightFrames;
        private Sprite[] _cachedPortraitFrames;

        // Public accessors
        public AnimationState CurrentState => _manualOverride ?? _state;
        public bool IsEating => _state == AnimationState.Eat;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (playerController != null)
            {
                _characterData = playerController.CharacterData;
                if (_characterData != null)
                {
                    if (_characterData.faceCameraFlight != null)
                        _cachedFaceCameraFlightFrames = new[] { _characterData.faceCameraFlight };
                    if (_characterData.portrait != null)
                        _cachedPortraitFrames = new[] { _characterData.portrait };
                }
            }
        }

        private void LateUpdate()
        {
            if (_characterData == null || playerController == null) return;

            float dt = Time.deltaTime;

            UpdateState(dt);
            UpdateFlip();

            // Cache frames once per LateUpdate to avoid redundant lookups
            _currentFrames = GetFramesForState(_manualOverride ?? _state);
            ApplySprite();
            ApplyFlightVisuals();
        }

        // --- Public API ---

        /// <summary>
        /// Trigger the eat animation (on snack collection).
        /// </summary>
        public void TriggerEatAnimation()
        {
            if (_manualOverride.HasValue) return;

            _state = AnimationState.Eat;
            _eatTimer = EatAnimationDuration;
            _currentFrame = 0;
            _frameTimer = 0f;
        }

        /// <summary>
        /// Trigger the chili reaction animation for a given duration.
        /// </summary>
        public void TriggerChiliAnimation(float duration)
        {
            int frameCount = _characterData.chiliReactionSprites != null
                ? _characterData.chiliReactionSprites.Length
                : 3;
            _chiliFrameDuration = frameCount > 0 ? duration / frameCount : 0.5f;

            _chiliTimer = duration;
            _manualOverride = AnimationState.ChiliReaction;
            _state = AnimationState.ChiliReaction;
            _currentFrame = 0;
            _frameTimer = 0f;
        }

        /// <summary>
        /// Set a manual animation state override (for cutscenes/intros).
        /// Pass null to clear the override.
        /// </summary>
        public void SetManualState(AnimationState? state)
        {
            _manualOverride = state;
            if (state.HasValue)
            {
                _state = state.Value;
                _currentFrame = 0;
                _frameTimer = 0f;
            }
        }

        /// <summary>
        /// Reset animation to idle state.
        /// </summary>
        public void ResetAnimation()
        {
            _state = AnimationState.Idle;
            _manualOverride = null;
            _currentFrame = 0;
            _frameTimer = 0f;
            _eatTimer = 0f;
            _chiliTimer = 0f;
        }

        // --- Internal ---

        private void UpdateState(float dt)
        {
            // Manual override takes priority
            if (_manualOverride.HasValue)
            {
                var overrideState = _manualOverride.Value;

                if (overrideState == AnimationState.ChiliReaction)
                {
                    _chiliTimer -= dt;
                    if (_chiliTimer <= 0f)
                    {
                        _manualOverride = null;
                        _state = playerController.IsMoving ? AnimationState.Run : AnimationState.Idle;
                        _currentFrame = 0;
                        _frameTimer = 0f;
                        return;
                    }
                }

                if (overrideState == AnimationState.ChiliReaction
                    || overrideState == AnimationState.FaceCamera
                    || overrideState == AnimationState.FaceCameraFlight)
                {
                    AdvanceFrame(dt, GetFrameDuration(overrideState));
                }
                return;
            }

            // Eat animation takes priority
            if (_state == AnimationState.Eat)
            {
                _eatTimer -= dt;
                if (_eatTimer <= 0f)
                {
                    _state = playerController.IsMoving ? AnimationState.Run : AnimationState.Idle;
                    _currentFrame = 0;
                    _frameTimer = 0f;
                }
                else
                {
                    AdvanceFrame(dt, EatFrameDuration);
                }
                return;
            }

            // Normal state transitions
            var newState = playerController.IsMoving ? AnimationState.Run : AnimationState.Idle;
            if (newState != _state)
            {
                _state = newState;
                _currentFrame = 0;
                _frameTimer = 0f;
            }

            if (_state == AnimationState.Run)
                AdvanceFrame(dt, RunFrameDuration);
        }

        private void AdvanceFrame(float dt, float frameDuration)
        {
            if (frameDuration <= 0f) return;

            _frameTimer += dt;
            if (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                var frames = GetFramesForState(_manualOverride ?? _state);
                if (frames != null && frames.Length > 0)
                    _currentFrame = (_currentFrame + 1) % frames.Length;
            }
        }

        private float GetFrameDuration(AnimationState state)
        {
            return state switch
            {
                AnimationState.Run => RunFrameDuration,
                AnimationState.Eat => EatFrameDuration,
                AnimationState.ChiliReaction => _chiliFrameDuration,
                AnimationState.FaceCamera => FaceCameraFrameDuration,
                AnimationState.FaceCameraFlight => FaceCameraFrameDuration,
                _ => 0f
            };
        }

        private Sprite[] GetFramesForState(AnimationState state)
        {
            if (_characterData == null) return null;

            return state switch
            {
                AnimationState.Idle => _characterData.runSprites,
                AnimationState.Run => _characterData.runSprites,
                AnimationState.Eat => _characterData.eatSprites,
                AnimationState.ChiliReaction => _characterData.chiliReactionSprites,
                AnimationState.FaceCamera => _cachedPortraitFrames ?? _characterData.runSprites,
                AnimationState.FaceCameraFlight => _cachedFaceCameraFlightFrames ?? _characterData.runSprites,
                _ => _characterData.runSprites
            };
        }

        private void UpdateFlip()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = !playerController.FacingRight;
        }

        private void ApplySprite()
        {
            var frames = _currentFrames;
            if (frames == null || frames.Length == 0) return;

            // Idle always shows first frame of run
            if (_state == AnimationState.Idle && !_manualOverride.HasValue)
            {
                _spriteRenderer.sprite = frames[0];
                return;
            }

            int frameIndex = Mathf.Min(_currentFrame, frames.Length - 1);
            _spriteRenderer.sprite = frames[frameIndex];
        }

        private void ApplyFlightVisuals()
        {
            // Apply hover bob + lift offset from PlayerController flight state
            // These offsets are applied via a local position offset on the sprite
            float hoverOffset = playerController.FlightHoverOffset;
            float liftOffset = playerController.FlightLiftOffset;
            float totalYOffset = hoverOffset + liftOffset;

            // Apply tilt rotation
            float tilt = playerController.FlightTiltAngle;

            // Only apply if non-trivial
            if (Mathf.Abs(totalYOffset) > 0.001f || Mathf.Abs(tilt) > 0.5f)
            {
                // Flight visual offset is applied to the sprite's local transform
                // We offset the sprite visually without affecting the collider position
                var localPos = _spriteRenderer.transform.localPosition;
                localPos.y = totalYOffset;
                _spriteRenderer.transform.localPosition = localPos;
                _spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }
            else
            {
                _spriteRenderer.transform.localPosition = Vector3.zero;
                _spriteRenderer.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
