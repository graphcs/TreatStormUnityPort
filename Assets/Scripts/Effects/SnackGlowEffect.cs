using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;

namespace SnackAttack.Effects
{
    public class SnackGlowEffect : MonoBehaviour
    {
        private UICircleDrawer _glowDrawer;
        private Image[] _sparkles;
        private Color _snackColor;
        private float _time;

        // Cached params
        private float _glowRadius;
        private float _glowPulseSpeed;
        private float _glowBaseAlpha;
        private float _glowPulseAlpha;
        private int _sparkleCount;
        private float _sparkleOrbitSpeed;
        private float _sparkleOrbitRadius;
        private float _sparkleSize;
        private bool _beamEnabled;
        private float _beamWidth;
        private float _beamHeight;
        private float _beamAlpha;

        public void Initialize(Color snackColor)
        {
            _snackColor = snackColor;

            var visuals = GameManager.Instance?.PowerUpVisuals;
            if (visuals != null)
            {
                var g = visuals.snackGlow;
                _glowRadius = g.glowRadiusPadding > 0f ? 36f + g.glowRadiusPadding : 48f;
                _glowPulseSpeed = g.glowPulseSpeed > 0f ? g.glowPulseSpeed : 3.5f;
                _glowBaseAlpha = g.glowBaseAlpha > 0f ? g.glowBaseAlpha : 40f / 255f;
                _glowPulseAlpha = g.glowPulseAlpha > 0f ? g.glowPulseAlpha : 30f / 255f;
                _sparkleCount = g.sparkleCount > 0 ? g.sparkleCount : 6;
                _sparkleOrbitSpeed = g.sparkleOrbitSpeed > 0f ? g.sparkleOrbitSpeed : 2f;
                _sparkleOrbitRadius = g.sparkleOrbitRadius > 0f ? g.sparkleOrbitRadius : 20f;
                _sparkleSize = g.sparkleSize > 0f ? g.sparkleSize : 3f;
                _beamEnabled = g.beamEnabled;
                _beamWidth = g.beamWidth > 0f ? g.beamWidth : 4f;
                _beamHeight = g.beamHeight > 0f ? g.beamHeight : 30f;
                _beamAlpha = g.beamAlpha > 0f ? g.beamAlpha : 60f / 255f;
            }
            else
            {
                _glowRadius = 48f;
                _glowPulseSpeed = 3.5f;
                _glowBaseAlpha = 40f / 255f;
                _glowPulseAlpha = 30f / 255f;
                _sparkleCount = 6;
                _sparkleOrbitSpeed = 2f;
                _sparkleOrbitRadius = 20f;
                _sparkleSize = 3f;
                _beamEnabled = true;
                _beamWidth = 4f;
                _beamHeight = 30f;
                _beamAlpha = 60f / 255f;
            }

            // Create glow drawer as first child (behind snack image)
            var glowGo = new GameObject("SnackGlow");
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.SetParent(transform, false);
            glowRect.SetAsFirstSibling();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(_glowRadius * 3f, _glowRadius * 3f);
            glowRect.anchoredPosition = Vector2.zero;

            _glowDrawer = glowGo.AddComponent<UICircleDrawer>();
            _glowDrawer.raycastTarget = false;

            // Create sparkles
            _sparkles = new Image[_sparkleCount];
            for (int i = 0; i < _sparkleCount; i++)
            {
                var sparkGo = new GameObject($"SnackSparkle_{i}");
                var sparkRect = sparkGo.AddComponent<RectTransform>();
                sparkRect.SetParent(transform, false);
                sparkRect.SetSiblingIndex(1); // After glow, before snack image
                sparkRect.sizeDelta = new Vector2(_sparkleSize, _sparkleSize);

                var img = sparkGo.AddComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = false;
                _sparkles[i] = img;
            }
        }

        private void Update()
        {
            _time += Time.deltaTime;

            float pulse = Mathf.Sin(_time * _glowPulseSpeed * Mathf.PI);

            // Glow
            _glowDrawer.Clear();

            float alpha = _glowBaseAlpha + pulse * _glowPulseAlpha;
            var glowColor = _snackColor;
            glowColor.a = alpha;
            float radius = _glowRadius + pulse * 4f;
            _glowDrawer.AddFilledCircle(Vector2.zero, radius, glowColor);

            // Beam
            if (_beamEnabled)
            {
                var beamColor = _snackColor;
                beamColor.a = _beamAlpha + pulse * (20f / 255f);
                _glowDrawer.AddBeam(Vector2.zero, _beamWidth, _beamHeight, beamColor);
            }

            _glowDrawer.Rebuild();

            // Sparkles
            float orbitRadius = _sparkleOrbitRadius + pulse * 3f;
            for (int i = 0; i < _sparkleCount; i++)
            {
                float angle = _time * _sparkleOrbitSpeed + (i * Mathf.PI * 2f / _sparkleCount);
                float x = Mathf.Cos(angle) * orbitRadius;
                float y = Mathf.Sin(angle) * orbitRadius;

                var rect = _sparkles[i].GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(x, y);
            }
        }

        private void OnDestroy()
        {
            // Children are destroyed automatically with parent GO
        }
    }
}
