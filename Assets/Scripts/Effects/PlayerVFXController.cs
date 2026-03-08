using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Entities;

namespace SnackAttack.Effects
{
    public class PlayerVFXController : MonoBehaviour
    {
        private PlayerController _player;
        private CharacterAnimator _animator;

        // Containers
        private RectTransform _vfxBehind;
        private RectTransform _vfxFront;
        private RectTransform _worldContainer;

        // Sub-effects
        private PickupFlashEffect _pickupFlash;
        private StatusIndicatorEffect _statusIndicator;
        private AuraEffect _aura;
        private WingsEffect _wings;
        private SpeedStreakEffect _speedStreak;
        private SteamParticleEffect _steam;

        private bool _initialized;

        private void Start()
        {
            _player = GetComponent<PlayerController>();
            _animator = GetComponent<CharacterAnimator>();

            if (_player == null) return;

            // Find world container (GameplayRoot = parent)
            _worldContainer = transform.parent as RectTransform;

            // Create VFX_Behind as first child (before SpriteDisplay)
            var behindGo = new GameObject("VFX_Behind");
            _vfxBehind = behindGo.AddComponent<RectTransform>();
            _vfxBehind.SetParent(transform, false);
            _vfxBehind.SetAsFirstSibling();
            _vfxBehind.anchorMin = new Vector2(0.5f, 0.5f);
            _vfxBehind.anchorMax = new Vector2(0.5f, 0.5f);
            _vfxBehind.sizeDelta = Vector2.zero;
            _vfxBehind.anchoredPosition = Vector2.zero;

            // Create VFX_Front after SpriteDisplay (sibling index 2)
            var frontGo = new GameObject("VFX_Front");
            _vfxFront = frontGo.AddComponent<RectTransform>();
            _vfxFront.SetParent(transform, false);
            _vfxFront.SetSiblingIndex(2);
            _vfxFront.anchorMin = new Vector2(0.5f, 0.5f);
            _vfxFront.anchorMax = new Vector2(0.5f, 0.5f);
            _vfxFront.sizeDelta = Vector2.zero;
            _vfxFront.anchoredPosition = Vector2.zero;

            InitializeEffects();

            // Subscribe to events
            EventBus.Subscribe(GameEvent.PowerUpActivated, OnPowerUpActivated);

            _initialized = true;
        }

        private void InitializeEffects()
        {
            // Pickup flash (front)
            _pickupFlash = new PickupFlashEffect();
            _pickupFlash.Initialize(_vfxFront);

            // Status indicators (front)
            _statusIndicator = new StatusIndicatorEffect();
            _statusIndicator.Initialize(_vfxFront, _player);

            // Aura (behind)
            _aura = new AuraEffect();
            _aura.Initialize(_vfxBehind, _player);

            // Wings (behind glow + front wings)
            _wings = new WingsEffect();
            _wings.Initialize(_vfxBehind, _vfxFront, _worldContainer, _player);

            // Speed streaks (behind afterimages + world particles)
            _speedStreak = new SpeedStreakEffect();
            _speedStreak.Initialize(_vfxBehind, _worldContainer, _player, _animator);

            // Steam (front)
            _steam = new SteamParticleEffect();
            _steam.Initialize(_vfxFront, _player);
        }

        private void LateUpdate()
        {
            if (!_initialized) return;

            float dt = Time.deltaTime;

            _pickupFlash.Update(dt);
            _statusIndicator.Update(dt);
            _aura.Update(dt);
            _wings.Update(dt);
            _speedStreak.Update(dt);
            _steam.Update(dt);
        }

        private void OnPowerUpActivated(EventData data)
        {
            if (data.payload == null || _player == null) return;

            if (data.payload.TryGetValue("playerId", out var idObj) && (int)idObj == _player.PlayerNumber)
            {
                if (data.payload.TryGetValue("effectType", out var typeObj))
                {
                    var effectType = (EffectType)typeObj;
                    _pickupFlash.Trigger(Vector2.zero, effectType);
                }
            }
        }

        public void ResetForNewRound()
        {
            _pickupFlash?.Reset();
            _statusIndicator?.Reset();
            _aura?.Reset();
            _wings?.Reset();
            _speedStreak?.Reset();
            _steam?.Reset();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe(GameEvent.PowerUpActivated, OnPowerUpActivated);

            _pickupFlash?.Destroy();
            _statusIndicator?.Destroy();
            _aura?.Destroy();
            _wings?.Destroy();
            _speedStreak?.Destroy();
            _steam?.Destroy();
        }
    }
}
