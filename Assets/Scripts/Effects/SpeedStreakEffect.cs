using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;
using SnackAttack.Entities;

namespace SnackAttack.Effects
{
    public class SpeedStreakEffect
    {
        private PlayerController _player;
        private CharacterAnimator _animator;
        private RectTransform _behindContainer;
        private RectTransform _worldContainer;

        // Afterimages
        private Image[] _afterimages;
        private RectTransform[] _afterimageRects;
        private Vector2[] _positionHistory;
        private int _historyHead;
        private int _historyCount;
        private const int MaxHistory = 18;
        private float _sampleTimer;
        private const float SampleInterval = 0.03f;

        // Streak + speed particles (world-space)
        private UIParticlePool _streakPool;
        private UIParticlePool _speedPool;

        private bool _active;
        private float _baseAlpha;

        // Cached params
        private int _afterimageCount;
        private Color _streakColorBoost;
        private Color _streakColorSpeed;
        private int _streakRate;
        private float _streakLifetime;
        private Vector2 _streakWidthRange;
        private int _particleRate;
        private float _particleLifetime;
        private Vector2 _particleSizeRange;

        public void Initialize(RectTransform behindContainer, RectTransform worldContainer,
            PlayerController player, CharacterAnimator animator)
        {
            _player = player;
            _animator = animator;
            _behindContainer = behindContainer;
            _worldContainer = worldContainer;

            var visuals = GameManager.Instance?.PowerUpVisuals;
            if (visuals != null)
            {
                var s = visuals.speedStreak;
                _afterimageCount = s.afterimageCount > 0 ? s.afterimageCount : 3;
                _baseAlpha = s.afterimageBaseAlpha > 0f ? s.afterimageBaseAlpha : 90f / 255f;
                _streakColorBoost = s.streakColorBoost.a > 0f ? s.streakColorBoost : new Color(100f / 255f, 200f / 255f, 1f);
                _streakColorSpeed = s.streakColorSpeed.a > 0f ? s.streakColorSpeed : new Color(1f, 200f / 255f, 80f / 255f);
                _streakWidthRange = s.streakWidthRange.y > 0f ? s.streakWidthRange : new Vector2(40f, 80f);
                _streakRate = s.streakRate > 0 ? s.streakRate : 3;
                _streakLifetime = s.streakLifetime > 0f ? s.streakLifetime : 0.25f;
                _particleRate = s.particleRate > 0 ? s.particleRate : 5;
                _particleLifetime = s.particleLifetime > 0f ? s.particleLifetime : 0.4f;
                _particleSizeRange = s.particleSizeRange.y > 0f ? s.particleSizeRange : new Vector2(3f, 7f);
            }
            else
            {
                _afterimageCount = 3;
                _baseAlpha = 90f / 255f;
                _streakColorBoost = new Color(100f / 255f, 200f / 255f, 1f);
                _streakColorSpeed = new Color(1f, 200f / 255f, 80f / 255f);
                _streakWidthRange = new Vector2(40f, 80f);
                _streakRate = 3;
                _streakLifetime = 0.25f;
                _particleRate = 5;
                _particleLifetime = 0.4f;
                _particleSizeRange = new Vector2(3f, 7f);
            }

            // Create afterimage Images (behind sprite)
            _afterimages = new Image[_afterimageCount];
            _afterimageRects = new RectTransform[_afterimageCount];
            float gpSize = player.CharacterData != null ? player.CharacterData.gameplaySize : 216f;

            for (int i = 0; i < _afterimageCount; i++)
            {
                var go = new GameObject($"Afterimage_{i}");
                var rect = go.AddComponent<RectTransform>();
                rect.SetParent(_behindContainer, false);
                rect.sizeDelta = new Vector2(gpSize, gpSize);

                var img = go.AddComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0f);
                go.SetActive(false);

                _afterimages[i] = img;
                _afterimageRects[i] = rect;
            }

            _positionHistory = new Vector2[MaxHistory];

            // Streak + speed pools (world-space)
            _streakPool = new UIParticlePool();
            _streakPool.Initialize(_worldContainer, 50);
            _speedPool = new UIParticlePool();
            _speedPool.Initialize(_worldContainer, 80);
        }

        public void Update(float dt)
        {
            if (_player == null) return;

            bool hasSpeed = _player.HasEffect(EffectType.SpeedBoost) || _player.HasEffect(EffectType.Boost);

            if (hasSpeed)
            {
                if (!_active)
                {
                    _active = true;
                    _historyHead = 0;
                    _historyCount = 0;
                    _sampleTimer = 0f;
                }

                // Sample position
                _sampleTimer += dt;
                if (_sampleTimer >= SampleInterval)
                {
                    _sampleTimer -= SampleInterval;
                    _positionHistory[_historyHead] = _player.RectTransform.anchoredPosition;
                    _historyHead = (_historyHead + 1) % MaxHistory;
                    if (_historyCount < MaxHistory) _historyCount++;
                }

                UpdateAfterimages();
                if (_player.IsMoving)
                {
                    EmitStreaks(dt);
                    EmitSpeedParticles(dt);
                }
            }
            else if (_active)
            {
                _active = false;
                HideAfterimages();
            }

            // Always update pools
            _streakPool.UpdateAll(dt);
            _speedPool.UpdateAll(dt);
        }

        private void UpdateAfterimages()
        {
            if (_animator == null) return;

            Sprite currentSprite = _animator.CurrentSprite;
            float decayRate = 300f / 255f;

            for (int i = 0; i < _afterimageCount; i++)
            {
                int historyIndex = i * (_historyCount / Mathf.Max(1, _afterimageCount));
                if (historyIndex >= _historyCount)
                {
                    _afterimages[i].gameObject.SetActive(false);
                    continue;
                }

                int actualIndex = (_historyHead - 1 - historyIndex + MaxHistory) % MaxHistory;
                if (actualIndex < 0) actualIndex += MaxHistory;

                _afterimages[i].gameObject.SetActive(true);
                if (currentSprite != null)
                    _afterimages[i].sprite = currentSprite;

                // Position relative to player (afterimage is in behind container which is child of player)
                Vector2 playerPos = _player.RectTransform.anchoredPosition;
                Vector2 histPos = _positionHistory[actualIndex];
                _afterimageRects[i].anchoredPosition = histPos - playerPos;

                // Flip to match player
                var scale = _afterimageRects[i].localScale;
                scale.x = _player.FacingRight ? 1f : -1f;
                _afterimageRects[i].localScale = scale;

                // Alpha: gradually transparent — e.g. for 3 images: 0=75%, 1=50%, 2=25%
                float alpha = (float)(_afterimageCount - i) / (_afterimageCount + 1);
                _afterimages[i].color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private void EmitStreaks(float dt)
        {
            bool isBoosting = _player.HasEffect(EffectType.Boost);
            Color streakColor = isBoosting ? _streakColorBoost : _streakColorSpeed;

            Vector2 playerPos = _player.RectTransform.anchoredPosition;
            float direction = _player.FacingRight ? -1f : 1f;

            int emitCount = Mathf.CeilToInt(_streakRate * dt * 60f);
            for (int i = 0; i < emitCount; i++)
            {
                float width = Random.Range(_streakWidthRange.x, _streakWidthRange.y);
                float py = playerPos.y + Random.Range(-30f, 30f);

                _streakPool.Emit(
                    new Vector2(playerPos.x, py),
                    new Vector2(500f * direction, 0f),
                    _streakLifetime,
                    new Vector2(width, 3f),
                    streakColor
                );
            }
        }

        private void EmitSpeedParticles(float dt)
        {
            Vector2 playerPos = _player.RectTransform.anchoredPosition;
            float direction = _player.FacingRight ? -1f : 1f;

            int emitCount = Mathf.CeilToInt(_particleRate * dt * 60f);
            for (int i = 0; i < emitCount; i++)
            {
                float size = Random.Range(_particleSizeRange.x, _particleSizeRange.y);
                float speed = Random.Range(100f, 250f);
                float vy = Random.Range(-40f, 40f);

                _speedPool.Emit(
                    playerPos,
                    new Vector2(speed * direction, vy),
                    _particleLifetime,
                    size,
                    Color.white
                );
            }
        }

        private void HideAfterimages()
        {
            for (int i = 0; i < _afterimageCount; i++)
            {
                if (_afterimages[i] != null)
                    _afterimages[i].gameObject.SetActive(false);
            }
        }

        public void Reset()
        {
            _active = false;
            _historyCount = 0;
            _historyHead = 0;
            HideAfterimages();
            _streakPool?.ClearAll();
            _speedPool?.ClearAll();
        }

        public void Destroy()
        {
            for (int i = 0; i < _afterimageCount; i++)
            {
                if (_afterimages[i] != null)
                    Object.Destroy(_afterimages[i].gameObject);
            }
            _streakPool?.DestroyAll();
            _speedPool?.DestroyAll();
        }
    }
}
