using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;

namespace SnackAttack.Entities
{
    /// <summary>
    /// Code-driven animation controller that swaps Image.sprite on a child GO
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

        [SerializeField] private PlayerController playerController;

        // Cached timing values (from GameSettingsSO)
        private float _runFrameDuration;
        private float _eatFrameDuration;
        private float _eatAnimationDuration;
        private float _faceCameraFrameDuration;

        // State
        private AnimationState _state = AnimationState.Idle;
        private AnimationState? _manualOverride;
        private int _currentFrame;
        private float _frameTimer;
        private float _eatTimer;
        private float _chiliFrameDuration;
        private float _chiliTimer;

        // Cached
        private Image _image;
        private RectTransform _imageRect;
        private CharacterSO _characterData;
        private Sprite[] _currentFrames;
        private Sprite[] _cachedFaceCameraFlightFrames;
        private Sprite[] _cachedPortraitFrames;

        // Public accessors
        public AnimationState CurrentState => _manualOverride ?? _state;
        public bool IsEating => _state == AnimationState.Eat;

        private void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            // Cache timing from SO
            var settings = GameManager.Instance?.GameSettings;
            _runFrameDuration = settings != null ? settings.runFrameDuration : 0.1f;
            _eatFrameDuration = settings != null ? settings.eatFrameDuration : 0.12f;
            _eatAnimationDuration = settings != null ? settings.eatAnimDuration : 0.4f;
            _faceCameraFrameDuration = settings != null ? settings.faceCameraFrameDuration : 0.1f;

            // Create child GO "SpriteDisplay" with Image
            var displayGo = new GameObject("SpriteDisplay");
            _imageRect = displayGo.AddComponent<RectTransform>();
            _imageRect.SetParent(transform, false);
            _imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            _imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            _imageRect.pivot = new Vector2(0.5f, 0.5f);
            _imageRect.anchoredPosition = Vector2.zero;

            _image = displayGo.AddComponent<Image>();
            _image.preserveAspect = true;
            _image.raycastTarget = false;
        }

        private void Start()
        {
            if (playerController != null)
            {
                _characterData = playerController.CharacterData;
                if (_characterData != null)
                {
                    // Set size from gameplay size (216 or 173)
                    float gpSize = _characterData.gameplaySize;
                    _imageRect.sizeDelta = new Vector2(gpSize, gpSize);

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

        public void TriggerEatAnimation()
        {
            if (_manualOverride.HasValue) return;

            _state = AnimationState.Eat;
            _eatTimer = _eatAnimationDuration;
            _currentFrame = 0;
            _frameTimer = 0f;
        }

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
                    AdvanceFrame(dt, _eatFrameDuration);
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
                AdvanceFrame(dt, _runFrameDuration);
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
                AnimationState.Run => _runFrameDuration,
                AnimationState.Eat => _eatFrameDuration,
                AnimationState.ChiliReaction => _chiliFrameDuration,
                AnimationState.FaceCamera => _faceCameraFrameDuration,
                AnimationState.FaceCameraFlight => _faceCameraFrameDuration,
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
            if (_imageRect != null)
            {
                var scale = _imageRect.localScale;
                scale.x = playerController.FacingRight ? 1f : -1f;
                _imageRect.localScale = scale;
            }
        }

        private void ApplySprite()
        {
            var frames = _currentFrames;
            if (frames == null || frames.Length == 0) return;

            // Idle always shows first frame of run
            if (_state == AnimationState.Idle && !_manualOverride.HasValue)
            {
                _image.sprite = frames[0];
                return;
            }

            int frameIndex = Mathf.Min(_currentFrame, frames.Length - 1);
            _image.sprite = frames[frameIndex];
        }

        private void ApplyFlightVisuals()
        {
            float hoverOffset = playerController.FlightHoverOffset;
            float liftOffset = playerController.FlightLiftOffset;
            float totalYOffset = hoverOffset + liftOffset;

            float tilt = playerController.FlightTiltAngle;

            if (Mathf.Abs(totalYOffset) > 0.1f || Mathf.Abs(tilt) > 0.5f)
            {
                var localPos = _imageRect.anchoredPosition;
                localPos.y = totalYOffset;
                _imageRect.anchoredPosition = localPos;
                _imageRect.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }
            else
            {
                _imageRect.anchoredPosition = Vector2.zero;
                _imageRect.localRotation = Quaternion.identity;
            }
        }
    }
}
