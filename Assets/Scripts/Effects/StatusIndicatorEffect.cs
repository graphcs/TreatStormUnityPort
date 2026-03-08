using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;
using SnackAttack.Entities;

namespace SnackAttack.Effects
{
    public class StatusIndicatorEffect
    {
        private struct Indicator
        {
            public EffectType type;
            public GameObject root;
            public Image barBg;
            public Image barFill;
            public Image icon;
            public RectTransform iconRect;
        }

        private RectTransform _container;
        private PlayerController _player;
        private readonly List<Indicator> _indicators = new();
        private float _time;
        private Sprite _whiteSprite;

        // Cached params
        private float _barWidth;
        private float _barHeight;
        private float _barOffsetY;
        private float _iconSize;
        private float _iconOffsetY;
        private float _iconBobSpeed;
        private float _iconBobAmplitude;
        private float _stackingSpacing;
        private Color _barBgColor;

        public void Initialize(RectTransform container, PlayerController player)
        {
            _container = container;
            _player = player;

            var visuals = GameManager.Instance?.PowerUpVisuals;
            if (visuals != null)
            {
                _whiteSprite = visuals.whiteSprite;
                _barWidth = visuals.statusIndicator.barWidth > 0f ? visuals.statusIndicator.barWidth : 50f;
                _barHeight = visuals.statusIndicator.barHeight > 0f ? visuals.statusIndicator.barHeight : 6f;
                _barOffsetY = visuals.statusIndicator.barOffsetY != 0f ? visuals.statusIndicator.barOffsetY : 12f;
                _iconSize = visuals.statusIndicator.iconSize > 0f ? visuals.statusIndicator.iconSize : 24f;
                _iconOffsetY = visuals.statusIndicator.iconOffsetY != 0f ? visuals.statusIndicator.iconOffsetY : 38f;
                _iconBobSpeed = visuals.statusIndicator.iconBobSpeed > 0f ? visuals.statusIndicator.iconBobSpeed : 2f;
                _iconBobAmplitude = visuals.statusIndicator.iconBobAmplitude > 0f ? visuals.statusIndicator.iconBobAmplitude : 3f;
                _stackingSpacing = visuals.statusIndicator.stackingSpacing > 0f ? visuals.statusIndicator.stackingSpacing : 44f;
                _barBgColor = visuals.statusIndicator.barBackgroundColor.a > 0f
                    ? visuals.statusIndicator.barBackgroundColor
                    : new Color(0f, 0f, 0f, 140f / 255f);
            }
            else
            {
                _barWidth = 50f;
                _barHeight = 6f;
                _barOffsetY = 12f;
                _iconSize = 24f;
                _iconOffsetY = 38f;
                _iconBobSpeed = 2f;
                _iconBobAmplitude = 3f;
                _stackingSpacing = 44f;
                _barBgColor = new Color(0f, 0f, 0f, 140f / 255f);
            }
        }

        public void Update(float dt)
        {
            if (_player == null) return;

            _time += dt;
            SyncIndicators();
            UpdatePositions();
        }

        private void SyncIndicators()
        {
            var activeEffects = _player.ActiveEffects;

            // Remove indicators for expired effects
            for (int i = _indicators.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int j = 0; j < activeEffects.Count; j++)
                {
                    if (activeEffects[j].type == _indicators[i].type)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Object.Destroy(_indicators[i].root);
                    _indicators.RemoveAt(i);
                }
            }

            // Add indicators for new effects
            for (int j = 0; j < activeEffects.Count; j++)
            {
                var effect = activeEffects[j];
                if (effect.type == EffectType.None) continue;

                bool exists = false;
                for (int i = 0; i < _indicators.Count; i++)
                {
                    if (_indicators[i].type == effect.type)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    CreateIndicator(effect.type);
            }

            // Update fill amounts
            for (int i = 0; i < _indicators.Count; i++)
            {
                var ind = _indicators[i];
                for (int j = 0; j < activeEffects.Count; j++)
                {
                    if (activeEffects[j].type == ind.type)
                    {
                        float fill = activeEffects[j].duration > 0f
                            ? activeEffects[j].timeRemaining / activeEffects[j].duration
                            : 0f;
                        ind.barFill.fillAmount = Mathf.Clamp01(fill);
                        break;
                    }
                }
            }
        }

        private void CreateIndicator(EffectType type)
        {
            Color effectColor = AuraEffect.GetEffectColor(type);

            var root = new GameObject($"StatusInd_{type}");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.SetParent(_container, false);
            rootRect.sizeDelta = new Vector2(_barWidth, _stackingSpacing);

            // Bar background
            var barBgGo = new GameObject("BarBg");
            var barBgRect = barBgGo.AddComponent<RectTransform>();
            barBgRect.SetParent(rootRect, false);
            barBgRect.sizeDelta = new Vector2(_barWidth, _barHeight);
            barBgRect.anchoredPosition = Vector2.zero;
            var barBg = barBgGo.AddComponent<Image>();
            barBg.color = _barBgColor;
            barBg.raycastTarget = false;
            if (_whiteSprite != null) barBg.sprite = _whiteSprite;

            // Bar fill — child of barBg, stretch to match parent, uses Image.Type.Filled
            var barFillGo = new GameObject("BarFill");
            var barFillRect = barFillGo.AddComponent<RectTransform>();
            barFillRect.SetParent(barBgRect, false);
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            var barFill = barFillGo.AddComponent<Image>();
            barFill.color = effectColor;
            barFill.raycastTarget = false;
            if (_whiteSprite != null) barFill.sprite = _whiteSprite;
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = 1f;

            // Icon
            var iconGo = new GameObject("Icon");
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.SetParent(rootRect, false);
            iconRect.sizeDelta = new Vector2(_iconSize, _iconSize);
            iconRect.anchoredPosition = new Vector2(0f, _iconOffsetY - _barOffsetY);
            var icon = iconGo.AddComponent<Image>();
            icon.color = effectColor;
            icon.raycastTarget = false;
            if (_whiteSprite != null) icon.sprite = _whiteSprite;

            _indicators.Add(new Indicator
            {
                type = type,
                root = root,
                barBg = barBg,
                barFill = barFill,
                icon = icon,
                iconRect = iconRect
            });
        }

        private void UpdatePositions()
        {
            float gpSize = _player.CharacterData != null ? _player.CharacterData.gameplaySize : 216f;
            float topY = gpSize * 0.5f;

            for (int i = 0; i < _indicators.Count; i++)
            {
                var ind = _indicators[i];
                var rootRect = ind.root.GetComponent<RectTransform>();
                float yPos = topY + _barOffsetY + i * _stackingSpacing;
                rootRect.anchoredPosition = new Vector2(0f, yPos);

                // Icon bob
                float bob = Mathf.Sin(_time * _iconBobSpeed * Mathf.PI * 2f) * _iconBobAmplitude;
                ind.iconRect.anchoredPosition = new Vector2(0f, _iconOffsetY - _barOffsetY + bob);
            }
        }

        public void Reset()
        {
            foreach (var ind in _indicators)
            {
                if (ind.root != null)
                    Object.Destroy(ind.root);
            }
            _indicators.Clear();
            _time = 0f;
        }

        public void Destroy()
        {
            Reset();
        }
    }
}
