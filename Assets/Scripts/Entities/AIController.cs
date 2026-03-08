using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Gameplay;

namespace SnackAttack.Entities
{
    public class AIController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AIDifficultySO difficultyConfig;

        [Header("References")]
        [SerializeField] private Arena arena;

        // AI parameters (cached from SO)
        private float _reactionDelay;
        private float _decisionAccuracy;
        private float _pathfindingEfficiency;
        private bool _avoidsPenalties;
        private bool _targetsPowerups;

        // Scoring weights (cached from SO)
        private float _distanceWeight;
        private float _verticalReachThreshold;
        private float _unreachablePenalty;
        private float _reachableBonus;
        private float _penaltyAvoidanceWeight;
        private float _boostBonus;
        private float _invincibilityBonus;
        private float _speedBonus;
        private float _reachThreshold;
        private float _wanderPadding;
        private float _wanderPaddingVertical;

        // State
        private float _decisionTimer;
        private FallingSnack _currentTarget;
        private Vector2 _targetPosition;
        private bool _hasTarget;

        // Reusable collection
        private readonly List<FallingSnack> _filteredSnacks = new();

        // Components
        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        public void Configure(AIDifficultySO difficulty, Arena arenaRef)
        {
            difficultyConfig = difficulty;
            arena = arenaRef;

            _reactionDelay = difficulty.reactionDelayMs / 1000f;
            _decisionAccuracy = difficulty.decisionAccuracy;
            _pathfindingEfficiency = difficulty.pathfindingEfficiency;
            _avoidsPenalties = difficulty.avoidsPenalties;
            _targetsPowerups = difficulty.targetsPowerups;

            // Cache scoring weights
            _distanceWeight = difficulty.distanceWeight;
            _verticalReachThreshold = difficulty.verticalReachThreshold;
            _unreachablePenalty = difficulty.unreachablePenalty;
            _reachableBonus = difficulty.reachableBonus;
            _penaltyAvoidanceWeight = difficulty.penaltyAvoidanceWeight;
            _boostBonus = difficulty.boostBonus;
            _invincibilityBonus = difficulty.invincibilityBonus;
            _speedBonus = difficulty.speedBonus;
            _reachThreshold = difficulty.reachThreshold;
            _wanderPadding = difficulty.wanderPadding;
            _wanderPaddingVertical = difficulty.wanderPaddingVertical;

            // Disable human input handler on the same GameObject
            var inputHandler = GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
                inputHandler.enabled = false;
        }

        public void SetArena(Arena arenaRef)
        {
            arena = arenaRef;
        }

        public void ResetForNewRound()
        {
            _currentTarget = null;
            _targetPosition = Vector2.zero;
            _hasTarget = false;
            _decisionTimer = 0f;
        }

        private void Update()
        {
            if (arena == null) return;

            _decisionTimer -= Time.deltaTime;

            if (_decisionTimer <= 0f)
            {
                MakeDecision();
                _decisionTimer = _reactionDelay;
            }

            // Validate current target
            if (_currentTarget != null && !_currentTarget.IsActive)
            {
                _currentTarget = null;
                _hasTarget = false;
            }

            if (_hasTarget)
            {
                // Update target position if tracking a live snack
                if (_currentTarget != null)
                    _targetPosition = _currentTarget.RectTransform.anchoredPosition;

                MoveTowardTarget();
            }
            else
            {
                _player.SetMoveInput(Vector2.zero);
            }
        }

        private void MakeDecision()
        {
            var allSnacks = arena.ActiveSnacks;

            _filteredSnacks.Clear();
            for (int i = 0; i < allSnacks.Count; i++)
            {
                if (allSnacks[i] != null && allSnacks[i].IsActive)
                    _filteredSnacks.Add(allSnacks[i]);
            }

            if (_filteredSnacks.Count == 0)
            {
                // Wander to random point
                _currentTarget = null;
                Rect bounds = arena.Bounds;
                float x, y;

                if (!_player.CanMoveVertical)
                {
                    x = Random.Range(bounds.xMin + _wanderPadding, bounds.xMax - _wanderPadding);
                    y = _player.RectTransform.anchoredPosition.y;
                }
                else
                {
                    x = Random.Range(bounds.xMin + _wanderPaddingVertical, bounds.xMax - _wanderPaddingVertical);
                    y = Random.Range(bounds.yMin + _wanderPaddingVertical, bounds.yMax - _wanderPaddingVertical);
                }

                _targetPosition = new Vector2(x, y);
                _hasTarget = true;
                return;
            }

            // Score each snack and pick the best
            float bestScore = float.MinValue;
            FallingSnack bestSnack = null;

            for (int i = 0; i < _filteredSnacks.Count; i++)
            {
                float score = EvaluateSnack(_filteredSnacks[i]);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSnack = _filteredSnacks[i];
                }
            }

            // Decision accuracy: sometimes pick random instead of best
            if (Random.value > _decisionAccuracy && _filteredSnacks.Count > 1)
                bestSnack = _filteredSnacks[Random.Range(0, _filteredSnacks.Count)];

            _currentTarget = bestSnack;
            _targetPosition = bestSnack.RectTransform.anchoredPosition;
            _hasTarget = true;
        }

        private float EvaluateSnack(FallingSnack snack)
        {
            float score = snack.SnackData.pointValue;

            // Distance penalty (horizontal weighted)
            float dx = snack.RectTransform.anchoredPosition.x - _player.RectTransform.anchoredPosition.x;
            score -= Mathf.Abs(dx) * _distanceWeight;

            // Vertical reachability
            if (!_player.CanMoveVertical)
            {
                float dy = snack.RectTransform.anchoredPosition.y - _player.RectTransform.anchoredPosition.y;
                if (dy > _verticalReachThreshold)
                    score -= _unreachablePenalty;
                if (dy <= 0f)
                    score += _reachableBonus;
            }

            // Penalty avoidance
            if (snack.SnackData.pointValue < 0 && _avoidsPenalties)
                score -= _penaltyAvoidanceWeight;

            // Power-up targeting
            if (_targetsPowerups && snack.SnackData.effect.HasEffect)
            {
                switch (snack.SnackData.effect.type)
                {
                    case EffectType.SpeedBoost:
                        score += _speedBonus;
                        break;
                    case EffectType.Invincibility:
                        score += _invincibilityBonus;
                        break;
                    case EffectType.Boost:
                        score += _boostBonus;
                        break;
                }
            }

            return score;
        }

        private void MoveTowardTarget()
        {
            Vector2 myPos = _player.RectTransform.anchoredPosition;
            Vector2 dir = _targetPosition - myPos;
            float distance = dir.magnitude;

            if (distance < _reachThreshold)
            {
                _player.SetMoveInput(Vector2.zero);
                return;
            }

            dir.Normalize();

            if (!_player.CanMoveVertical)
                dir.y = 0f;

            // Pathfinding noise
            if (Random.value > _pathfindingEfficiency)
            {
                dir.x += Random.Range(-0.3f, 0.3f);
                if (_player.CanMoveVertical)
                    dir.y += Random.Range(-0.3f, 0.3f);
                float mag = dir.magnitude;
                if (mag > 0f)
                {
                    dir.x /= mag;
                    dir.y /= mag;
                }
            }

            _player.SetMoveInput(dir);
        }
    }
}
