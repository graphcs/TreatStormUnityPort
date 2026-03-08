using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Entities
{
    public enum LeashState
    {
        Normal,
        Extended,
        Yanked
    }

    public class PlayerController : MonoBehaviour
    {
        [System.Serializable]
        public struct ActiveEffect
        {
            public EffectType type;
            public float magnitude;
            public float duration;
            public float timeRemaining;
        }

        [Header("Configuration")]
        [SerializeField] private CharacterSO characterData;
        [SerializeField] private int playerNumber = 1;
        [SerializeField] private bool horizontalOnly = false;

        // Movement
        private Vector2 _moveInput;
        private Vector2 _velocity;
        private bool _facingRight;
        private bool _isMoving;

        // Score
        private int _score;

        // Effects
        private readonly List<ActiveEffect> _activeEffects = new();

        // Leash
        private float _leashBaseMinX;
        private float _leashBaseMaxX;
        private float _leashMinX;
        private float _leashMaxX;
        private float _leashEffectTimer;
        private float _arenaWidth;

        // Flight
        private float _restingY;
        private float _flightHoverOffset;
        private float _flightLiftOffset;
        private float _flightTiltAngle;
        private float _flightTime;

        // Arena
        private Rect _arenaBounds;
        private bool _arenaInitialized;

        // Per-frame cached state
        private bool _hasBoosting;
        private float _speedMultiplier;

        // Cached components
        private RectTransform _rectTransform;

        // Cached settings
        private GameSettingsSO _settings;

        // Public read-only accessors
        public int PlayerNumber => playerNumber;
        public int Score => _score;
        public bool IsInvincible => HasEffect(EffectType.Invincibility);
        public bool IsMoving => _isMoving;
        public bool FacingRight => _facingRight;
        public Vector2 Velocity => _velocity;
        public float FlightHoverOffset => _flightHoverOffset;
        public float FlightLiftOffset => _flightLiftOffset;
        public float FlightTiltAngle => _flightTiltAngle;
        public CharacterSO CharacterData => characterData;
        public IReadOnlyList<ActiveEffect> ActiveEffects => _activeEffects;
        public bool HorizontalOnly => horizontalOnly;
        public Rect ArenaBounds => _arenaBounds;
        public bool CanMoveVertical => !horizontalOnly || _hasBoosting;
        public RectTransform RectTransform => _rectTransform;

        // Collar offset from player center (PyGame: +70px right, +100px down from top-left)
        // With centered pivot on ~130px sprite: x = 70 - 65 = 5, y = 65 - 100 = -35
        public Vector2 CollarOffset => new(_facingRight ? 5f : -5f, -35f);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _facingRight = (playerNumber == 1);
        }

        private void Update()
        {
            if (!_arenaInitialized) return;

            float dt = Time.deltaTime;

            // Cache per-frame values to avoid repeated list scans
            _hasBoosting = HasEffect(EffectType.Boost);
            _speedMultiplier = GetSpeedMultiplier();

            ProcessMovement(dt);
            UpdateLeashTimer(dt);
            UpdateEffects(dt);
            UpdateFlightState(dt);
        }

        private float VisualSize => characterData != null ? characterData.gameplaySize : 216f;

        private GameSettingsSO Settings
        {
            get
            {
                if (_settings == null)
                    _settings = GameManager.Instance?.GameSettings;
                return _settings;
            }
        }

        // --- Public API ---

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        public void Configure(CharacterSO character, int number, bool horizontal)
        {
            characterData = character;
            playerNumber = number;
            horizontalOnly = horizontal;
            _facingRight = (number == 1);
        }

        public void InitializeArena(Rect bounds)
        {
            _arenaBounds = bounds;
            _arenaWidth = bounds.width;
            _arenaInitialized = true;

            // Center-pivot: clamp center so edges stay inside arena
            float halfSize = VisualSize * 0.5f;
            _leashBaseMinX = bounds.xMin + halfSize;
            _leashBaseMaxX = bounds.xMax - halfSize;
            _leashMinX = _leashBaseMinX;
            _leashMaxX = _leashBaseMaxX;

            float groundYOffset = Settings != null ? Settings.playerGroundYOffset : 230f;
            float startX = bounds.xMin + bounds.width * 0.5f;
            float startY = bounds.yMin + groundYOffset - halfSize;
            _rectTransform.anchoredPosition = new Vector2(startX, startY);
            _restingY = startY;
        }

        public bool ApplyEffect(EffectType type, float magnitude, float duration)
        {
            // Invincibility blocks penalty effects
            if (IsInvincible && (type == EffectType.Slow || type == EffectType.Chaos))
                return false;

            var effect = new ActiveEffect
            {
                type = type,
                magnitude = magnitude,
                duration = duration,
                timeRemaining = duration
            };
            _activeEffects.Add(effect);

            EventBus.Emit(GameEvent.PowerUpActivated, new Dictionary<string, object>
            {
                { "playerId", playerNumber },
                { "effectType", type },
                { "magnitude", magnitude },
                { "duration", duration }
            });
            return true;
        }

        public bool ApplyEffect(EffectDefinition def)
        {
            if (def.HasEffect)
                return ApplyEffect(def.type, def.magnitude, def.duration);
            return false;
        }

        public bool HasEffect(EffectType type)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].type == type) return true;
            }
            return false;
        }

        public float GetSpeedMultiplier()
        {
            float multiplier = 1f;
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var t = _activeEffects[i].type;
                if (t == EffectType.SpeedBoost || t == EffectType.Slow || t == EffectType.Boost)
                    multiplier *= _activeEffects[i].magnitude;
            }
            return multiplier;
        }

        public float GetScoreMultiplier()
        {
            float multiplier = 1f;
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].type == EffectType.Boost)
                    multiplier *= _activeEffects[i].magnitude;
            }
            return multiplier;
        }

        public void AddScore(int points)
        {
            _score += points;
            if (_score < 0) _score = 0;

            EventBus.Emit(GameEvent.ScoreChanged, new Dictionary<string, object>
            {
                { "playerId", playerNumber },
                { "score", _score }
            });
        }

        public void ExtendLeash(float? crossMax = null)
        {
            float extendFraction = Settings != null ? Settings.leashExtendFraction : 0.15f;
            float effectDuration = Settings != null ? Settings.leashEffectDuration : 8f;

            if (crossMax.HasValue)
                _leashMaxX = crossMax.Value;
            else
                _leashMaxX = _leashBaseMaxX + _arenaWidth * extendFraction;

            _leashEffectTimer = effectDuration;
        }

        public void YankLeash()
        {
            float yankFraction = Settings != null ? Settings.leashYankFraction : 0.35f;
            float effectDuration = Settings != null ? Settings.leashEffectDuration : 8f;

            float minRange = VisualSize * 2f;
            _leashMaxX = Mathf.Max(
                _leashBaseMaxX - _arenaWidth * yankFraction,
                _leashMinX + minRange
            );
            _leashEffectTimer = effectDuration;
        }

        public void ResetLeash()
        {
            _leashMinX = _leashBaseMinX;
            _leashMaxX = _leashBaseMaxX;
            _leashEffectTimer = 0f;
        }

        public LeashState GetLeashState()
        {
            if (_leashEffectTimer <= 0f) return LeashState.Normal;
            if (_leashMaxX > _leashBaseMaxX) return LeashState.Extended;
            if (_leashMaxX < _leashBaseMaxX) return LeashState.Yanked;
            return LeashState.Normal;
        }

        public void ResetForNewRound()
        {
            float groundYOffset = Settings != null ? Settings.playerGroundYOffset : 230f;
            float startX = _arenaBounds.xMin + _arenaBounds.width * 0.5f;
            float halfSize = VisualSize * 0.5f;
            float startY = _arenaBounds.yMin + groundYOffset - halfSize;
            _rectTransform.anchoredPosition = new Vector2(startX, startY);
            _restingY = startY;
            _velocity = Vector2.zero;
            _moveInput = Vector2.zero;

            // Score
            _score = 0;

            // Effects
            _activeEffects.Clear();

            // Leash
            ResetLeash();

            // Flight
            _flightHoverOffset = 0f;
            _flightLiftOffset = 0f;
            _flightTiltAngle = 0f;
            _flightTime = 0f;

            // Facing
            _facingRight = (playerNumber == 1);
        }

        // --- Internal Logic ---

        private void ProcessMovement(float dt)
        {
            Vector2 input = _moveInput;

            // Flip controls if chaos active
            if (HasEffect(EffectType.Chaos))
                input = -input;

            // Zero vertical if not allowed
            if (!CanMoveVertical)
                input.y = 0f;

            // Normalize diagonal when vertical is allowed
            if (CanMoveVertical && input.x != 0f && input.y != 0f)
                input = input.normalized;

            float baseMoveSpeed = Settings != null ? Settings.baseMoveSpeed : 350f;
            float charSpeed = characterData != null ? characterData.baseSpeed : 1f;
            float speed = baseMoveSpeed * charSpeed * _speedMultiplier;

            _velocity.x = input.x * speed;
            _velocity.y = CanMoveVertical ? input.y * speed : 0f;

            // Dampen upward velocity near flight ceiling
            float ceilingMargin = Settings != null ? Settings.flightCeilingMargin : 30f;
            if (CanMoveVertical && horizontalOnly && _velocity.y > 0f)
            {
                float ceiling = GetFlightCeiling();
                float posY = _rectTransform.anchoredPosition.y;
                if (posY >= ceiling - ceilingMargin)
                {
                    float damp = Mathf.Max(0f, (ceiling - posY) / ceilingMargin);
                    _velocity.y *= damp;
                }
            }

            // Update facing direction
            if (input.x > 0f)
                _facingRight = true;
            else if (input.x < 0f)
                _facingRight = false;

            _isMoving = input.x != 0f || (CanMoveVertical && input.y != 0f);

            // Return-to-ground spring when horizontalOnly and not boosting
            float springForce = Settings != null ? Settings.flightSpringForce : 5f;
            if (horizontalOnly && !_hasBoosting)
            {
                float diff = _restingY - _rectTransform.anchoredPosition.y;
                if (Mathf.Abs(diff) > 1f)
                    _velocity.y = diff * springForce;
                else
                {
                    var pos = _rectTransform.anchoredPosition;
                    pos.y = _restingY;
                    _rectTransform.anchoredPosition = pos;
                    _velocity.y = 0f;
                }
            }

            // Apply velocity
            Vector2 newPos = _rectTransform.anchoredPosition + _velocity * dt;

            // Clamp X to leash bounds
            newPos.x = Mathf.Clamp(newPos.x, _leashMinX, _leashMaxX);

            // Clamp Y to arena bounds (center-pivot: inset by half visual size)
            float halfSize = VisualSize * 0.5f;
            float minY = _arenaBounds.yMin + halfSize;
            float maxY = _arenaBounds.yMax - halfSize;
            if (horizontalOnly && CanMoveVertical)
                maxY = GetFlightCeiling();
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

            _rectTransform.anchoredPosition = newPos;
        }

        private float GetFlightCeiling()
        {
            float heightFraction = Settings != null ? Settings.flightHeightFraction : 0.35f;
            float topOfSprite = _restingY + VisualSize * 0.5f;
            float maxLift = (_arenaBounds.yMax - topOfSprite) * heightFraction;
            return _restingY + maxLift;
        }

        private void UpdateLeashTimer(float dt)
        {
            if (_leashEffectTimer > 0f)
            {
                _leashEffectTimer -= dt;
                if (_leashEffectTimer <= 0f)
                    ResetLeash();
            }
        }

        private void UpdateEffects(float dt)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.timeRemaining -= dt;

                if (effect.timeRemaining <= 0f)
                {
                    _activeEffects.RemoveAt(i);

                    EventBus.Emit(GameEvent.PowerUpExpired, new Dictionary<string, object>
                    {
                        { "playerId", playerNumber },
                        { "effectType", effect.type }
                    });

                    if (effect.type == EffectType.Chaos)
                    {
                        EventBus.Emit(GameEvent.ChaosEnded, new Dictionary<string, object>
                        {
                            { "playerId", playerNumber }
                        });
                    }
                }
                else
                {
                    _activeEffects[i] = effect;
                }
            }
        }

        private void UpdateFlightState(float dt)
        {
            float hoverFreq = Settings != null ? Settings.hoverFrequency : 3f;
            float hoverAmp = Settings != null ? Settings.hoverAmplitude : 6f;
            float liftTarget = Settings != null ? Settings.flightLiftTarget : -15f;
            float tiltThreshold = Settings != null ? Settings.tiltVelocityThreshold : 50f;
            float tiltAng = Settings != null ? Settings.tiltAngle : 8f;
            float lateralLean = Settings != null ? Settings.lateralLeanMultiplier : 4f;
            float leanTilt = Settings != null ? Settings.leanToTiltRatio : 0.3f;
            float tiltSmooth = Settings != null ? Settings.tiltSmoothingRate : 8f;
            float hoverDecay = Settings != null ? Settings.hoverDecayRate : 6f;
            float liftDecay = Settings != null ? Settings.liftDecayRate : 6f;
            float tiltDecay = Settings != null ? Settings.tiltDecayRate : 8f;

            if (_hasBoosting)
            {
                _flightTime += dt;
                _flightHoverOffset = Mathf.Sin(_flightTime * hoverFreq) * hoverAmp;
                _flightLiftOffset += (liftTarget - _flightLiftOffset) * Mathf.Min(1f, dt * Settings.flightSpringForce);

                // Tilt based on velocity
                float targetTilt = 0f;
                if (_velocity.y > tiltThreshold)
                    targetTilt = tiltAng;
                else if (_velocity.y < -tiltThreshold)
                    targetTilt = -tiltAng;

                if (_velocity.x != 0f)
                {
                    float lean = _velocity.x > 0f ? lateralLean : -lateralLean;
                    if (!_facingRight) lean = -lean;
                    targetTilt += lean * leanTilt;
                }

                _flightTiltAngle += (targetTilt - _flightTiltAngle) * Mathf.Min(1f, dt * tiltSmooth);
            }
            else
            {
                _flightTime = 0f;
                _flightHoverOffset *= Mathf.Max(0f, 1f - dt * hoverDecay);
                _flightLiftOffset *= Mathf.Max(0f, 1f - dt * liftDecay);
                _flightTiltAngle *= Mathf.Max(0f, 1f - dt * tiltDecay);
            }
        }
    }
}
