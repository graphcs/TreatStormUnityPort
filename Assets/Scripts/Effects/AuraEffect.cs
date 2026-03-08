using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;
using SnackAttack.Entities;

namespace SnackAttack.Effects
{
    public class AuraEffect
    {
        private struct AuraInstance
        {
            public EffectType type;
            public float ringIndex;
            public Image[] sparkles;
        }

        private UICircleDrawer _drawer;
        private RectTransform _container;
        private PlayerController _player;
        private readonly List<AuraInstance> _auras = new();
        private float _time;

        // Cached params
        private float _pulseSpeed;
        private float _baseRadiusPadding;
        private float _pulseAmplitude;
        private float _baseAlpha;
        private float _ringWidth;
        private int _sparkleCount;
        private float _sparkleSpeed;
        private float _sparkleSize;

        public void Initialize(RectTransform container, PlayerController player)
        {
            _container = container;
            _player = player;

            var go = new GameObject("AuraDrawer");
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
                _pulseSpeed = visuals.aura.pulseSpeed > 0f ? visuals.aura.pulseSpeed : 3f;
                _baseRadiusPadding = visuals.aura.baseRadiusPadding > 0f ? visuals.aura.baseRadiusPadding : 15f;
                _pulseAmplitude = visuals.aura.pulseAmplitude > 0f ? visuals.aura.pulseAmplitude : 8f;
                _baseAlpha = visuals.aura.baseAlpha > 0f ? visuals.aura.baseAlpha : 50f / 255f;
                _ringWidth = visuals.aura.ringWidth > 0f ? visuals.aura.ringWidth : 3f;
                _sparkleCount = visuals.aura.sparkleCount > 0 ? visuals.aura.sparkleCount : 8;
                _sparkleSpeed = visuals.aura.sparkleSpeed > 0f ? visuals.aura.sparkleSpeed : 2.5f;
                _sparkleSize = visuals.aura.sparkleSize > 0f ? visuals.aura.sparkleSize : 4f;
            }
            else
            {
                _pulseSpeed = 3f;
                _baseRadiusPadding = 15f;
                _pulseAmplitude = 8f;
                _baseAlpha = 50f / 255f;
                _ringWidth = 3f;
                _sparkleCount = 8;
                _sparkleSpeed = 2.5f;
                _sparkleSize = 4f;
            }
        }

        public void Update(float dt)
        {
            if (_player == null) return;

            _time += dt;

            // Sync aura instances with active effects
            SyncAuras();

            // Draw rings
            _drawer.Clear();

            float gpSize = _player.CharacterData != null ? _player.CharacterData.gameplaySize : 216f;
            float baseRadius = gpSize * 0.5f + _baseRadiusPadding;

            for (int i = 0; i < _auras.Count; i++)
            {
                var aura = _auras[i];
                float concentricOffset = i * 12f;
                float radius = baseRadius + concentricOffset;
                float pulse = Mathf.Sin(_time * _pulseSpeed * Mathf.PI) * _pulseAmplitude;
                radius += pulse;

                Color color = GetEffectColor(aura.type);
                float alphaVariation = Mathf.Sin(_time * _pulseSpeed * Mathf.PI) * (20f / 255f);
                color.a = _baseAlpha + alphaVariation;

                _drawer.AddRing(Vector2.zero, radius, _ringWidth, color);

                // Update sparkles
                if (aura.sparkles != null)
                {
                    for (int s = 0; s < aura.sparkles.Length; s++)
                    {
                        float angle = _time * _sparkleSpeed + (s * Mathf.PI * 2f / aura.sparkles.Length);
                        float orbitH = radius * 1.0f;
                        float orbitV = radius * 0.7f;
                        float x = Mathf.Cos(angle) * orbitH;
                        float y = Mathf.Sin(angle) * orbitV;

                        var sparkleRect = aura.sparkles[s].GetComponent<RectTransform>();
                        sparkleRect.anchoredPosition = new Vector2(x, y);

                        float sparkleAlpha = (200f + 55f * Mathf.Sin(angle * 3f)) / 255f;
                        var sparkleColor = color;
                        sparkleColor.a = sparkleAlpha;
                        aura.sparkles[s].color = sparkleColor;
                    }
                }
            }

            _drawer.Rebuild();
        }

        private void SyncAuras()
        {
            var activeEffects = _player.ActiveEffects;

            // Remove auras for expired effects
            for (int i = _auras.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int j = 0; j < activeEffects.Count; j++)
                {
                    if (activeEffects[j].type == _auras[i].type)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    DestroyAura(_auras[i]);
                    _auras.RemoveAt(i);
                }
            }

            // Add auras for new effects
            for (int j = 0; j < activeEffects.Count; j++)
            {
                var type = activeEffects[j].type;
                if (type == EffectType.None) continue;

                bool exists = false;
                for (int i = 0; i < _auras.Count; i++)
                {
                    if (_auras[i].type == type)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var aura = new AuraInstance { type = type };
                    aura.sparkles = new Image[_sparkleCount];
                    Color sparkColor = GetEffectColor(type);

                    for (int s = 0; s < _sparkleCount; s++)
                    {
                        var go = new GameObject($"Sparkle_{type}_{s}");
                        var rect = go.AddComponent<RectTransform>();
                        rect.SetParent(_container, false);
                        rect.sizeDelta = new Vector2(_sparkleSize, _sparkleSize);

                        var img = go.AddComponent<Image>();
                        img.color = sparkColor;
                        img.raycastTarget = false;
                        aura.sparkles[s] = img;
                    }

                    _auras.Add(aura);
                }
            }
        }

        private void DestroyAura(AuraInstance aura)
        {
            if (aura.sparkles != null)
            {
                foreach (var sparkle in aura.sparkles)
                {
                    if (sparkle != null)
                        Object.Destroy(sparkle.gameObject);
                }
            }
        }

        public void Reset()
        {
            for (int i = _auras.Count - 1; i >= 0; i--)
                DestroyAura(_auras[i]);
            _auras.Clear();
            if (_drawer != null) _drawer.Clear();
            _time = 0f;
        }

        public void Destroy()
        {
            Reset();
            if (_drawer != null) Object.Destroy(_drawer.gameObject);
        }

        public static Color GetEffectColor(EffectType type)
        {
            var visuals = GameManager.Instance?.PowerUpVisuals;
            if (visuals != null)
            {
                return type switch
                {
                    EffectType.Boost => visuals.aura.colorBoost.a > 0f ? visuals.aura.colorBoost : new Color(80f / 255f, 160f / 255f, 1f),
                    EffectType.SpeedBoost => visuals.aura.colorSpeedBoost.a > 0f ? visuals.aura.colorSpeedBoost : new Color(1f, 200f / 255f, 80f / 255f),
                    EffectType.Invincibility => visuals.aura.colorInvincibility.a > 0f ? visuals.aura.colorInvincibility : new Color(1f, 1f, 200f / 255f),
                    EffectType.Chaos => visuals.aura.colorChaos.a > 0f ? visuals.aura.colorChaos : new Color(1f, 60f / 255f, 60f / 255f),
                    EffectType.Slow => visuals.aura.colorSlow.a > 0f ? visuals.aura.colorSlow : new Color(60f / 255f, 180f / 255f, 60f / 255f),
                    _ => Color.white
                };
            }

            return type switch
            {
                EffectType.Boost => new Color(80f / 255f, 160f / 255f, 1f),
                EffectType.SpeedBoost => new Color(1f, 200f / 255f, 80f / 255f),
                EffectType.Invincibility => new Color(1f, 1f, 200f / 255f),
                EffectType.Chaos => new Color(1f, 60f / 255f, 60f / 255f),
                EffectType.Slow => new Color(60f / 255f, 180f / 255f, 60f / 255f),
                _ => Color.white
            };
        }
    }
}
