using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Entities;

namespace SnackAttack.Effects
{
    public class SteamParticleEffect
    {
        private PlayerController _player;
        private UIParticlePool _pool;
        private bool _active;

        // Cached params
        private int _emitRate;
        private float _lifetime;
        private Vector2 _sizeRange;
        private float _growthRate;
        private float _horizontalSpread;
        private float _verticalOffset;
        private Vector2 _velocityYRange;
        private float _velocityXRange;

        public void Initialize(RectTransform container, PlayerController player)
        {
            _player = player;

            var visuals = GameManager.Instance?.PowerUpVisuals;
            if (visuals != null && visuals.steam.enabled)
            {
                var s = visuals.steam;
                _emitRate = s.emitRate > 0 ? s.emitRate : 2;
                _lifetime = s.lifetime > 0f ? s.lifetime : 0.8f;
                _sizeRange = s.sizeRange.y > 0f ? s.sizeRange : new Vector2(6f, 12f);
                _growthRate = s.growthRate > 0f ? s.growthRate : 8f;
                _horizontalSpread = s.horizontalSpread > 0f ? s.horizontalSpread : 20f;
                _verticalOffset = s.verticalOffset > 0f ? s.verticalOffset : 10f;
                _velocityYRange = s.velocityYRange.y > 0f ? s.velocityYRange : new Vector2(40f, 60f);
                _velocityXRange = s.velocityXRange > 0f ? s.velocityXRange : 15f;
            }
            else
            {
                _emitRate = 2;
                _lifetime = 0.8f;
                _sizeRange = new Vector2(6f, 12f);
                _growthRate = 8f;
                _horizontalSpread = 20f;
                _verticalOffset = 10f;
                _velocityYRange = new Vector2(40f, 60f);
                _velocityXRange = 15f;
            }

            _pool = new UIParticlePool();
            _pool.Initialize(container, 60);
        }

        public void Update(float dt)
        {
            if (_player == null) return;

            bool hasChaos = _player.HasEffect(EffectType.Chaos);

            if (hasChaos)
            {
                _active = true;
                EmitSteam(dt);
            }
            else if (_active)
            {
                _active = false;
            }

            _pool.UpdateAll(dt);
        }

        private void EmitSteam(float dt)
        {
            int emitCount = Mathf.CeilToInt(_emitRate * dt * 60f);
            emitCount = Mathf.Min(emitCount, 3);

            for (int i = 0; i < emitCount; i++)
            {
                float px = Random.Range(-_horizontalSpread, _horizontalSpread);
                float py = _verticalOffset;
                float vx = Random.Range(-_velocityXRange, _velocityXRange);
                float vy = Random.Range(_velocityYRange.x, _velocityYRange.y);
                float size = Random.Range(_sizeRange.x, _sizeRange.y);

                _pool.Emit(
                    new Vector2(px, py),
                    new Vector2(vx, vy),
                    _lifetime,
                    size,
                    Color.white,
                    _growthRate
                );
            }
        }

        public void Reset()
        {
            _active = false;
            _pool?.ClearAll();
        }

        public void Destroy()
        {
            _pool?.DestroyAll();
        }
    }
}
