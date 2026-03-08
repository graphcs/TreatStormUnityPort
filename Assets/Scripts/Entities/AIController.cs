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
                    x = Random.Range(bounds.xMin + 20f, bounds.xMax - 20f);
                    y = _player.RectTransform.anchoredPosition.y;
                }
                else
                {
                    x = Random.Range(bounds.xMin + 10f, bounds.xMax - 10f);
                    y = Random.Range(bounds.yMin + 10f, bounds.yMax - 10f);
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
            score -= Mathf.Abs(dx) * 0.5f;

            // Vertical reachability
            if (!_player.CanMoveVertical)
            {
                float dy = snack.RectTransform.anchoredPosition.y - _player.RectTransform.anchoredPosition.y;
                if (dy > 50f)
                    score -= 200f;
                if (dy <= 0f)
                    score += 100f;
            }

            // Penalty avoidance
            if (snack.SnackData.pointValue < 0 && _avoidsPenalties)
                score -= 300f;

            // Power-up targeting
            if (_targetsPowerups && snack.SnackData.effect.HasEffect)
            {
                switch (snack.SnackData.effect.type)
                {
                    case EffectType.SpeedBoost:
                        score += 100f;
                        break;
                    case EffectType.Invincibility:
                        score += 150f;
                        break;
                    case EffectType.Boost:
                        score += 100f;
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

            // Reached threshold (PyGame: 5px)
            if (distance < 5f)
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
