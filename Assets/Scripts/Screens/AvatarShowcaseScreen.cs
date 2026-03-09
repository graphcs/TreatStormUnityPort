using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using Utilities.Inputs;

namespace SnackAttack.Screens
{
    public class AvatarShowcaseScreen : BaseScreen
    {
        [Header("Showcase References")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _darkOverlay;
        [SerializeField] private Image _glowEffect;
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _breedText;
        [SerializeField] private Image _statsPanelBg;
        [SerializeField] private TMP_Text[] _statLabels;
        [SerializeField] private Image[] _statBarBGs;
        [SerializeField] private Image[] _statBarFills;
        [SerializeField] private TMP_Text[] _statPcts;
        [SerializeField] private Image _runPreview;
        [SerializeField] private TMP_Text _runLabel;
        [SerializeField] private Image _eatPreview;
        [SerializeField] private TMP_Text _eatLabel;
        [SerializeField] private TMP_Text _backText;
        [SerializeField] private Image _selectIndicator;
        [SerializeField] private TMP_Text _footer;

        // State
        private CharacterSO _character;
        private bool _active;
        private bool _inputCooldown;
        private bool _backHovered;
        private float _glowTimer;
        private float _runAnimTimer;
        private float _eatAnimTimer;
        private int _runFrame;
        private int _eatFrame;
        private float _entranceTimer;
        private bool _entranceDone;

        // Stat animation
        private float[] _statTargets;
        private float[] _statCurrent;
        private float _statAnimTimer;
        private bool _statsAnimDone;

        // Cached
        private UIColorsSO _colors;
        private ControlsSO _controls;

        private static readonly string[] StatNames = { "Speed", "Agility", "Appetite", "Charm" };

        public override void OnEnter(Dictionary<string, object> data)
        {
            base.OnEnter(data);

            _colors = GM.UIColors;
            _controls = GM.Controls;
            _character = data != null && data.ContainsKey("character") ? (CharacterSO)data["character"] : null;

            if (_character == null)
            {
                ChangeState(GameState.CharacterSelect);
                return;
            }

            SetupDisplay();
            StartEntrance();

            _inputCooldown = true;
            _active = true;
        }

        public override void OnExit()
        {
            _active = false;
            base.OnExit();
        }

        private void SetupDisplay()
        {
            if (_portrait != null)
            {
                _portrait.sprite = _character.portrait;
                _portrait.preserveAspect = true;
            }

            if (_nameText != null)
                _nameText.text = _character.displayName.ToUpper();

            if (_breedText != null)
            {
                string breed = _character.breed;
                if (string.IsNullOrEmpty(breed) || breed.Length > 40)
                    breed = "Custom Champion";
                _breedText.text = breed;
            }

            // Calculate stats
            float speed = _character.baseSpeed;
            float speedPct = Mathf.InverseLerp(0.6f, 1.4f, speed);
            float agilityPct = Mathf.Clamp01(speed * 0.9f + 0.1f - 0.6f) / 0.8f;
            float appetitePct = 0.85f;
            float charmPct = 0.7f;

            _statTargets = new[] { speedPct, agilityPct, appetitePct, charmPct };
            _statCurrent = new float[4];

            for (int i = 0; i < 4 && i < _statLabels.Length; i++)
            {
                if (_statLabels[i] != null) _statLabels[i].text = StatNames[i];
                if (_statBarFills[i] != null) _statBarFills[i].fillAmount = 0f;
                if (_statPcts[i] != null) _statPcts[i].text = "0%";
            }

            _statsAnimDone = false;
            _statAnimTimer = 0f;

            _runFrame = 0;
            _eatFrame = 0;
            _runAnimTimer = 0f;
            _eatAnimTimer = 0f;
        }

        private void StartEntrance()
        {
            _entranceTimer = 0f;
            _entranceDone = false;

            // Start portrait and stats panel off-screen
            if (_portrait != null)
            {
                var rect = _portrait.rectTransform;
                rect.anchoredPosition += new Vector2(0, -50f);
            }
            if (_statsPanelBg != null)
            {
                var rect = _statsPanelBg.rectTransform;
                rect.anchoredPosition += new Vector2(0, -80f);
            }
        }

        private void Update()
        {
            if (!_active || !InputsManager.Started) return;

            if (_inputCooldown)
            {
                _inputCooldown = false;
                return;
            }

            UpdateEntrance();
            UpdateGlow();
            UpdateAnimations();
            UpdateStatBars();
            HandleInput();
            HandleMouse();
        }

        private void UpdateEntrance()
        {
            if (_entranceDone) return;

            _entranceTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_entranceTimer / 0.8f);

            // Ease-out-back: t' = 1 + 2.7 * (t-1)^3 + 1.7 * (t-1)^2
            float tm1 = t - 1f;
            float eased = 1f + 2.7f * tm1 * tm1 * tm1 + 1.7f * tm1 * tm1;

            if (_portrait != null)
            {
                var rect = _portrait.rectTransform;
                // Animate from -50 offset back to 0
                float yOffset = Mathf.Lerp(-50f, 0f, eased);
                var pos = rect.anchoredPosition;
                // We set it relative each frame since we only added -50 initially
                rect.anchoredPosition = new Vector2(pos.x, GetPortraitBaseY() + yOffset + 50f);
            }

            if (_statsPanelBg != null)
            {
                float statsT = Mathf.Clamp01((_entranceTimer - 0.15f) / 0.65f);
                float stm1 = statsT - 1f;
                float statsEased = statsT <= 0f ? 0f : 1f + 2.7f * stm1 * stm1 * stm1 + 1.7f * stm1 * stm1;
                var rect = _statsPanelBg.rectTransform;
                float yOffset = Mathf.Lerp(-80f, 0f, statsEased);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, GetStatsPanelBaseY() + yOffset + 80f);
            }

            if (t >= 1f) _entranceDone = true;
        }

        private float GetPortraitBaseY()
        {
            // Will be set by editor tool; default centered upper area
            return -280f;
        }

        private float GetStatsPanelBaseY()
        {
            return -580f;
        }

        private void UpdateGlow()
        {
            if (_glowEffect == null) return;

            _glowTimer += Time.unscaledDeltaTime * 2f;
            float alpha = 0.3f + 0.2f * Mathf.Sin(_glowTimer * Mathf.PI);
            Color gold = _colors != null ? _colors.showcaseAccentGold : new Color32(255, 200, 60, 255);
            _glowEffect.color = new Color(gold.r, gold.g, gold.b, alpha);
        }

        private void UpdateAnimations()
        {
            // Run animation
            if (_runPreview != null && _character.runSprites != null && _character.runSprites.Length > 0)
            {
                _runAnimTimer += Time.unscaledDeltaTime;
                if (_runAnimTimer >= 0.12f)
                {
                    _runAnimTimer -= 0.12f;
                    _runFrame = (_runFrame + 1) % _character.runSprites.Length;
                    _runPreview.sprite = _character.runSprites[_runFrame];
                }
            }

            // Eat animation
            if (_eatPreview != null && _character.eatSprites != null && _character.eatSprites.Length > 0)
            {
                _eatAnimTimer += Time.unscaledDeltaTime;
                if (_eatAnimTimer >= 0.15f)
                {
                    _eatAnimTimer -= 0.15f;
                    _eatFrame = (_eatFrame + 1) % _character.eatSprites.Length;
                    _eatPreview.sprite = _character.eatSprites[_eatFrame];
                }
            }
        }

        private void UpdateStatBars()
        {
            if (_statsAnimDone || _statTargets == null) return;

            _statAnimTimer += Time.unscaledDeltaTime;
            bool allDone = true;

            for (int i = 0; i < 4 && i < _statBarFills.Length; i++)
            {
                float delay = i * 0.2f;
                float t = Mathf.Clamp01((_statAnimTimer - delay) / 0.8f);

                // Ease-out-cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _statCurrent[i] = _statTargets[i] * eased;

                if (_statBarFills[i] != null)
                    _statBarFills[i].fillAmount = _statCurrent[i];
                if (_statPcts[i] != null)
                    _statPcts[i].text = $"{Mathf.RoundToInt(_statCurrent[i] * 100)}%";

                if (t < 1f) allDone = false;
            }

            if (allDone) _statsAnimDone = true;
        }

        private void HandleInput()
        {
            string cancelAction = _controls != null ? _controls.cancelAction : "Cancel";
            string submitAction = _controls != null ? _controls.submitAction : "Submit";

            if (InputsManager.InputDown(cancelAction) || InputsManager.InputDown(submitAction))
            {
                GoBack();
            }
        }

        private void HandleMouse()
        {
            Vector2 mousePos = InputsManager.InputMousePosition();
            bool prevBack = _backHovered;
            _backHovered = false;

            if (_backText != null)
            {
                var backRect = _backText.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(backRect, mousePos, null))
                {
                    _backHovered = true;
                    if (InputsManager.InputMouseButtonUp(0))
                        GoBack();
                }
            }

            if (_backHovered != prevBack)
            {
                Color normal = _colors != null ? _colors.showcaseBackNormal : new Color32(147, 76, 48, 255);
                Color hover = _colors != null ? _colors.showcaseBackHover : new Color32(200, 120, 70, 255);
                if (_backText != null) _backText.color = _backHovered ? hover : normal;
                if (_selectIndicator != null) _selectIndicator.enabled = _backHovered;
            }
        }

        private new void GoBack()
        {
            PlaySound("select");
            ChangeState(GameState.CharacterSelect);
        }
    }
}
