using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Entities;

namespace SnackAttack.Gameplay
{
    public class SnackCollector : MonoBehaviour
    {
        const float StolenBonusMultiplier = 1.5f;

        private PlayerController _player;
        private CharacterAnimator _animator;
        private Arena _arena;
        private bool _inOpponentArena;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _animator = GetComponent<CharacterAnimator>();
        }

        public void SetArena(Arena arena)
        {
            _arena = arena;
        }

        public void SetInOpponentArena(bool inOpponent)
        {
            _inOpponentArena = inOpponent;
        }

        private void Update()
        {
            if (_arena == null || _player == null) return;

            // Build player rect from anchoredPosition, shrunk 40px/side (PyGame inflate(-80,-80))
            Vector2 playerPos = _player.RectTransform.anchoredPosition;
            float charSize = _player.CharacterData != null ? _player.CharacterData.gameplaySize : 216f;
            float halfSize = charSize * 0.5f;
            float shrink = 40f;
            Rect playerRect = new Rect(
                playerPos.x - halfSize + shrink,
                playerPos.y - halfSize + shrink,
                charSize - shrink * 2f,
                charSize - shrink * 2f
            );

            var snacks = _arena.ActiveSnacks;
            for (int i = snacks.Count - 1; i >= 0; i--)
            {
                var snack = snacks[i];
                if (snack == null || !snack.IsActive) continue;

                Rect snackRect = snack.GetCollisionRect();
                if (playerRect.Overlaps(snackRect))
                {
                    CollectSnack(snack, _inOpponentArena);
                }
            }
        }

        private void CollectSnack(FallingSnack snack, bool stolen)
        {
            Vector3 snackPosition = (Vector3)snack.RectTransform.anchoredPosition;

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
