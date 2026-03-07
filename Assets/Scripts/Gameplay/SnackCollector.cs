using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Entities;

namespace SnackAttack.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class SnackCollector : MonoBehaviour
    {
        const float StolenBonusMultiplier = 1.5f;

        private PlayerController _player;
        private CharacterAnimator _animator;
        private Rigidbody2D _rb;
        private bool _inOpponentArena;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _animator = GetComponent<CharacterAnimator>();
            _rb = GetComponent<Rigidbody2D>();

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        public void SetInOpponentArena(bool inOpponent)
        {
            _inOpponentArena = inOpponent;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<FallingSnack>(out var snack)) return;
            if (!snack.IsActive) return;

            CollectSnack(snack, _inOpponentArena);
        }

        private void CollectSnack(FallingSnack snack, bool stolen)
        {
            Vector3 snackPosition = snack.transform.position;

            SnackSO data = snack.Collect();
            if (data == null) return;

            float points = data.pointValue;
            if (stolen) points *= StolenBonusMultiplier;
            points *= _player.GetScoreMultiplier();

            _player.AddScore(Mathf.RoundToInt(points));

            if (data.effect.HasEffect)
            {
                bool effectApplied = _player.ApplyEffect(data.effect);

                if (effectApplied && data.effect.type == EffectType.Chaos)
                {
                    _animator.TriggerChiliAnimation(data.effect.duration);

                    EventBus.Emit(GameEvent.ChaosTriggered, new Dictionary<string, object>
                    {
                        { "playerId", _player.PlayerNumber },
                        { "duration", data.effect.duration }
                    });
                    EventBus.Emit(GameEvent.PenaltyApplied, new Dictionary<string, object>
                    {
                        { "playerId", _player.PlayerNumber },
                        { "effectType", data.effect.type },
                        { "duration", data.effect.duration }
                    });
                }
                else if (effectApplied && data.effect.type == EffectType.Slow)
                {
                    _animator.TriggerEatAnimation();

                    EventBus.Emit(GameEvent.PenaltyApplied, new Dictionary<string, object>
                    {
                        { "playerId", _player.PlayerNumber },
                        { "effectType", data.effect.type },
                        { "duration", data.effect.duration }
                    });
                }
                else
                {
                    _animator.TriggerEatAnimation();
                }
            }
            else
            {
                _animator.TriggerEatAnimation();
            }

            EventBus.Emit(GameEvent.PointPopupRequested, new Dictionary<string, object>
            {
                { "playerId", _player.PlayerNumber },
                { "points", Mathf.RoundToInt(points) },
                { "position", snackPosition },
                { "stolen", stolen },
                { "snackId", data.id }
            });
        }
    }
}
