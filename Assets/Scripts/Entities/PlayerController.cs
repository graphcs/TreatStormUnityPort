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
        private float _baseMoveSpeed = 3.5f;
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
        private float _leashEffectDuration = 8f;
        private float _leashExtendFraction = 0.15f;
        private float _leashYankFraction = 0.35f;
        private float _arenaWidth;

        // Flight
        private float _flightHeightFraction = 0.35f;
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
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _boxCollider;

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

        // Collar offset from player transform center (PyGame: +70px right, +100px down from top-left)
        // With centered pivot on ~130px sprite: x = (70 - 65) / 100 = 0.05, y = (65 - 100) / 100 = -0.35
        public Vector2 CollarOffset => new(_facingRight ? 0.05f : -0.05f, -0.35f);

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _boxCollider = GetComponent<BoxCollider2D>();
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

        private float CharacterWidth => characterData != null ? characterData.hitboxSize.x / 100f : 1f;

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

            _leashBaseMinX = bounds.xMin;
            _leashBaseMaxX = bounds.xMax - CharacterWidth;
            _leashMinX = _leashBaseMinX;
            _leashMaxX = _leashBaseMaxX;

            // Position at center of arena
            transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
            _restingY = transform.position.y;
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
            if (crossMax.HasValue)
                _leashMaxX = crossMax.Value;
            else
                _leashMaxX = _leashBaseMaxX + _arenaWidth * _leashExtendFraction;

            _leashEffectTimer = _leashEffectDuration;
        }

        public void YankLeash()
        {
            float minRange = CharacterWidth * 2f;
            _leashMaxX = Mathf.Max(
                _leashBaseMaxX - _arenaWidth * _leashYankFraction,
                _leashMinX + minRange
            );
            _leashEffectTimer = _leashEffectDuration;
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
            // Position
            transform.position = new Vector3(_arenaBounds.center.x, _arenaBounds.center.y, 0f);
            _restingY = transform.position.y;
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

            float charSpeed = characterData != null ? characterData.baseSpeed : 1f;
            float speed = _baseMoveSpeed * charSpeed * _speedMultiplier;

            _velocity.x = input.x * speed;
            _velocity.y = CanMoveVertical ? input.y * speed : 0f;

            // Dampen upward velocity near flight ceiling
            if (CanMoveVertical && horizontalOnly && _velocity.y > 0f)
            {
                // In Unity, positive Y is up, so upward velocity is positive
                float ceiling = GetFlightCeiling();
                float margin = 30f / 100f; // Convert PyGame px margin to Unity units
                float posY = transform.position.y;
                if (posY >= ceiling - margin)
                {
                    float damp = Mathf.Max(0f, (ceiling - posY) / margin);
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
            if (horizontalOnly && !_hasBoosting)
            {
                float diff = _restingY - transform.position.y;
                if (Mathf.Abs(diff) > 0.01f)
                    _velocity.y = diff * 5f;
                else
                {
                    var pos = transform.position;
                    pos.y = _restingY;
                    transform.position = pos;
                    _velocity.y = 0f;
                }
            }

            // Apply velocity
            Vector3 newPos = transform.position + (Vector3)_velocity * dt;

            // Clamp X to leash bounds
            newPos.x = Mathf.Clamp(newPos.x, _leashMinX, _leashMaxX);

            // Clamp Y to arena bounds
            // In Unity, Y increases upward. arenaBounds.yMin is bottom, yMax is top.
            float minY = _arenaBounds.yMin;
            float maxY = _arenaBounds.yMax;
            if (horizontalOnly && CanMoveVertical)
                maxY = GetFlightCeiling();
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

            transform.position = newPos;
        }

        private float GetFlightCeiling()
        {
            // How high the dog can fly: fraction of resting-to-top distance
            float maxLift = (_arenaBounds.yMax - _restingY) * _flightHeightFraction;
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
            if (_hasBoosting)
            {
                _flightTime += dt;
                _flightHoverOffset = Mathf.Sin(_flightTime * 3f) * 6f / 100f;
                _flightLiftOffset += (-0.15f - _flightLiftOffset) * Mathf.Min(1f, dt * 5f);

                // Tilt based on velocity
                float targetTilt = 0f;
                if (_velocity.y > 0.5f)
                    targetTilt = 8f;  // nose-up (positive Y = up in Unity)
                else if (_velocity.y < -0.5f)
                    targetTilt = -8f; // nose-down

                if (_velocity.x != 0f)
                {
                    float lean = _velocity.x > 0f ? 4f : -4f;
                    if (!_facingRight) lean = -lean;
                    targetTilt += lean * 0.3f;
                }

                _flightTiltAngle += (targetTilt - _flightTiltAngle) * Mathf.Min(1f, dt * 8f);
            }
            else
            {
                _flightTime = 0f;
                _flightHoverOffset *= Mathf.Max(0f, 1f - dt * 6f);
                _flightLiftOffset *= Mathf.Max(0f, 1f - dt * 6f);
                _flightTiltAngle *= Mathf.Max(0f, 1f - dt * 8f);
            }
        }
    }
}
