using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;
using SnackAttack.Entities;

namespace SnackAttack.Effects
{
    public class WingsEffect
    {
        private PlayerController _player;
        private RectTransform _behindContainer;
        private RectTransform _frontContainer;
        private RectTransform _worldContainer;

        // Wing images
        private Image _leftWing;
        private Image _rightWing;
        private RectTransform _leftWingRect;
        private RectTransform _rightWingRect;

        // Flight glow
        private UICircleDrawer _glowDrawer;

        // Trail particles
        private UIParticlePool _trailPool;

        private float _time;
        private bool _wingsActive;

        // Cached params
        private float _flapSpeed;
        private float _flapAmplitude;
        private float _wingWidth;
        private float _wingHeight;
        private float _shoulderYFrac;
        private float _shoulderXFrac;
        private Sprite _wingUpSprite;
        private Sprite _wingDownSprite;
        private Color _glowOuter;
        private Color _glowInner;
        private float _glowPulseSpeed;
        private float _glowRadiusFrac;
        private int _trailRate;
        private float _trailLifetime;

        public void Initialize(RectTransform behindContainer, RectTransform frontContainer,
            RectTransform worldContainer, PlayerController player)
        {
            _player = player;
            _behindContainer = behindContainer;
            _frontContainer = frontContainer;
            _worldContainer = worldContainer;

            var visuals = GameManager.Instance?.PowerUpVisuals;
            if (visuals != null)
            {
                var w = visuals.wings;
                _flapSpeed = w.flapSpeed > 0f ? w.flapSpeed : 8f;
                _flapAmplitude = w.flapAmplitude > 0f ? w.flapAmplitude : 1f;
                _wingWidth = w.wingWidth > 0f ? w.wingWidth : 38f;
                _wingHeight = w.wingHeight > 0f ? w.wingHeight : 50f;
                _shoulderYFrac = w.shoulderOffsetYFraction > 0f ? w.shoulderOffsetYFraction : 0.05f;
                _shoulderXFrac = w.shoulderOffsetXFraction > 0f ? w.shoulderOffsetXFraction : 0.24f;
                _wingUpSprite = w.wingUpSprite;
                _wingDownSprite = w.wingDownSprite;
                _glowOuter = w.flightGlowOuter.a > 0f ? w.flightGlowOuter : new Color(1f, 215f / 255f, 80f / 255f, 35f / 255f);
                _glowInner = w.flightGlowInner.a > 0f ? w.flightGlowInner : new Color(1f, 230f / 255f, 120f / 255f, 55f / 255f);
                _glowPulseSpeed = w.flightGlowPulseSpeed > 0f ? w.flightGlowPulseSpeed : 4f;
                _glowRadiusFrac = w.flightGlowRadiusFraction > 0f ? w.flightGlowRadiusFraction : 0.7f;
                _trailRate = w.trailParticleRate > 0 ? w.trailParticleRate : 9;
                _trailLifetime = w.trailParticleLifetime > 0f ? w.trailParticleLifetime : 0.6f;
            }
            else
            {
                _flapSpeed = 8f;
                _flapAmplitude = 1f;
                _wingWidth = 38f;
                _wingHeight = 50f;
                _shoulderYFrac = 0.05f;
                _shoulderXFrac = 0.24f;
                _glowOuter = new Color(1f, 215f / 255f, 80f / 255f, 35f / 255f);
                _glowInner = new Color(1f, 230f / 255f, 120f / 255f, 55f / 255f);
                _glowPulseSpeed = 4f;
                _glowRadiusFrac = 0.7f;
                _trailRate = 9;
                _trailLifetime = 0.6f;
            }

            // Create wing images (front container, visible in front of sprite)
            _leftWing = CreateWingImage("LeftWing", _frontContainer);
            _leftWingRect = _leftWing.GetComponent<RectTransform>();
            _rightWing = CreateWingImage("RightWing", _frontContainer);
            _rightWingRect = _rightWing.GetComponent<RectTransform>();
            SetWingsVisible(false);

            // Glow drawer (behind sprite)
            var glowGo = new GameObject("FlightGlow");
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.SetParent(_behindContainer, false);
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            _glowDrawer = glowGo.AddComponent<UICircleDrawer>();
            _glowDrawer.raycastTarget = false;

            // Trail particles (world-space persistence)
            _trailPool = new UIParticlePool();
            _trailPool.Initialize(_worldContainer, 100);
        }

        private Image CreateWingImage(string name, RectTransform parent)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(_wingWidth * 2f, _wingHeight * 2f);

            var img = go.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public void Update(float dt)
        {
            if (_player == null) return;

            bool hasBoosting = _player.HasEffect(EffectType.Boost);

            if (hasBoosting)
            {
                _time += dt;

                if (!_wingsActive)
                {
                    _wingsActive = true;
                    SetWingsVisible(true);
                }

                UpdateWingFlap();
                UpdateFlightGlow();
                UpdateTrailParticles(dt);
            }
            else if (_wingsActive)
            {
                _wingsActive = false;
                SetWingsVisible(false);
                _glowDrawer.Clear();
                _time = 0f;
            }

            // Always update trail pool (particles persist after effect ends)
            _trailPool.UpdateAll(dt);
        }

        private void UpdateWingFlap()
        {
            float gpSize = _player.CharacterData != null ? _player.CharacterData.gameplaySize : 216f;
            float shoulderY = gpSize * _shoulderYFrac;
            float shoulderX = gpSize * _shoulderXFrac;

            float sine = Mathf.Sin(_time * _flapSpeed);
            bool wingUp = sine > 0f;
            Sprite wingSprite = wingUp ? _wingUpSprite : _wingDownSprite;

            // Fallback if no sprites assigned
            if (wingSprite != null)
            {
                _leftWing.sprite = wingSprite;
                _rightWing.sprite = wingSprite;
            }

            bool facingRight = _player.FacingRight;

            // Left wing
            _leftWingRect.anchoredPosition = new Vector2(-shoulderX, shoulderY);
            var lScale = _leftWingRect.localScale;
            lScale.x = facingRight ? -1f : 1f;
            _leftWingRect.localScale = lScale;

            // Right wing
            _rightWingRect.anchoredPosition = new Vector2(shoulderX, shoulderY);
            var rScale = _rightWingRect.localScale;
            rScale.x = facingRight ? 1f : -1f;
            _rightWingRect.localScale = rScale;
        }

        private void UpdateFlightGlow()
        {
            _glowDrawer.Clear();

            float gpSize = _player.CharacterData != null ? _player.CharacterData.gameplaySize : 216f;
            float maxDim = gpSize;
            float outerRadius = maxDim * _glowRadiusFrac;
            float pulse = Mathf.Sin(_time * _glowPulseSpeed) * 0.15f + 0.85f;

            var outerColor = _glowOuter;
            outerColor.a *= pulse;
            _glowDrawer.AddFilledCircle(Vector2.zero, outerRadius, outerColor);

            var innerColor = _glowInner;
            innerColor.a *= pulse;
            _glowDrawer.AddFilledCircle(Vector2.zero, outerRadius * 0.55f, innerColor);

            _glowDrawer.Rebuild();
        }

        private void UpdateTrailParticles(float dt)
        {
            if (!_player.IsMoving) return;

            // Convert player local pos to world container pos
            var playerRect = _player.RectTransform;
            Vector2 playerPos = playerRect.anchoredPosition;

            int emitCount = Mathf.CeilToInt(_trailRate * dt * 60f);
            for (int i = 0; i < emitCount; i++)
            {
                float px = playerPos.x + Random.Range(-10f, 10f);
                float py = playerPos.y + Random.Range(5f, 15f);
                float vx = Random.Range(-12f, 12f);
                float vy = Random.Range(-50f, -25f);

                _trailPool.Emit(
                    new Vector2(px, py),
                    new Vector2(vx, vy),
                    _trailLifetime,
                    Random.Range(3f, 7f),
                    new Color(1f, 240f / 255f, 150f / 255f)
                );
            }
        }

        private void SetWingsVisible(bool visible)
        {
            if (_leftWing != null) _leftWing.gameObject.SetActive(visible);
            if (_rightWing != null) _rightWing.gameObject.SetActive(visible);
        }

        public void Reset()
        {
            _wingsActive = false;
            _time = 0f;
            SetWingsVisible(false);
            if (_glowDrawer != null) _glowDrawer.Clear();
            _trailPool?.ClearAll();
        }

        public void Destroy()
        {
            if (_leftWing != null) Object.Destroy(_leftWing.gameObject);
            if (_rightWing != null) Object.Destroy(_rightWing.gameObject);
            if (_glowDrawer != null) Object.Destroy(_glowDrawer.gameObject);
            _trailPool?.DestroyAll();
        }
    }
}
