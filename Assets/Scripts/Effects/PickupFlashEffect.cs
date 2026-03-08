using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Effects
{
    public class PickupFlashEffect
    {
        private UICircleDrawer _drawer;
        private bool _active;
        private float _elapsed;
        private float _duration;
        private float _maxRadius;
        private float _maxAlpha;
        private Color _color;
        private Vector2 _center;

        public void Initialize(RectTransform container)
        {
            var go = new GameObject("PickupFlash");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(container, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _drawer = go.AddComponent<UICircleDrawer>();
            _drawer.raycastTarget = false;

            var visuals = GameManager.Instance?.PowerUpVisuals;
            if (visuals != null)
            {
                _duration = visuals.pickupFlash.duration > 0f ? visuals.pickupFlash.duration : 0.3f;
                _maxRadius = visuals.pickupFlash.ringMaxRadius > 0f ? visuals.pickupFlash.ringMaxRadius : 60f;
                _maxAlpha = visuals.pickupFlash.maxAlpha > 0f ? visuals.pickupFlash.maxAlpha : 80f / 255f;
            }
            else
            {
                _duration = 0.3f;
                _maxRadius = 60f;
                _maxAlpha = 80f / 255f;
            }
        }

        public void Trigger(Vector2 center, EffectType effectType)
        {
            _active = true;
            _elapsed = 0f;
            _center = center;
            _color = AuraEffect.GetEffectColor(effectType);
        }

        public void Update(float dt)
        {
            if (!_active)
            {
                return;
            }

            _elapsed += dt;
            if (_elapsed >= _duration)
            {
                _active = false;
                _drawer.Clear();
                return;
            }

            float t = _elapsed / _duration;
            float radius = t * _maxRadius;
            float alpha = _maxAlpha * (1f - t);

            _drawer.Clear();

            // Ring
            var ringColor = _color;
            ringColor.a = alpha;
            _drawer.AddRing(_center, radius, 3f, ringColor);

            // Inner fill at half alpha
            var fillColor = _color;
            fillColor.a = alpha * 0.5f;
            _drawer.AddFilledCircle(_center, radius * 0.8f, fillColor);

            _drawer.Rebuild();
        }

        public void Reset()
        {
            _active = false;
            if (_drawer != null) _drawer.Clear();
        }

        public void Destroy()
        {
            if (_drawer != null)
                Object.Destroy(_drawer.gameObject);
        }
    }
}
