using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;

namespace SnackAttack.Interaction
{
    public class CrowdChaosOverlay : MonoBehaviour
    {
        [SerializeField] private Image _tintOverlay;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _countdownNumber;
        [SerializeField] private TMP_Text _optionsText;

        private CanvasGroup _canvasGroup;
        private VotingSettingsSO _settings;
        private bool _isCountdown;
        private bool _isLive;
        private float _countdownTimer;

        // Smooth tint alpha (PyGame lerps towards target)
        private float _currentTintAlpha;
        private bool _chaosVisualActive;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Hide();
        }

        public void Initialize(VotingSettingsSO settings)
        {
            _settings = settings;
            _currentTintAlpha = 0f;
        }

        public void StartCountdown(float duration)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _isCountdown = true;
            _isLive = false;
            _chaosVisualActive = true;
            _countdownTimer = duration;

            if (_titleText != null)
            {
                _titleText.gameObject.SetActive(true);
                _titleText.text = "CROWD CHAOS IN";
                _titleText.color = _settings?.countdownTitleColor ?? new Color(255f / 255f, 235f / 255f, 235f / 255f);
            }
            if (_countdownNumber != null)
            {
                _countdownNumber.gameObject.SetActive(true);
                _countdownNumber.color = _settings?.countdownNumberColor ?? new Color(255f / 255f, 110f / 255f, 110f / 255f);
            }
            if (_optionsText != null)
                _optionsText.gameObject.SetActive(false);
        }

        public void StartLive(string[] options)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _isCountdown = false;
            _isLive = true;
            _chaosVisualActive = true;

            if (_titleText != null)
            {
                _titleText.gameObject.SetActive(true);
                _titleText.text = "CROWD CHAOS LIVE";
                _titleText.color = _settings?.liveTitleColor ?? new Color(255f / 255f, 180f / 255f, 180f / 255f);
            }
            if (_countdownNumber != null)
                _countdownNumber.gameObject.SetActive(false);

            if (_optionsText != null && options != null && options.Length > 0)
            {
                _optionsText.gameObject.SetActive(true);
                string joined = "";
                for (int i = 0; i < options.Length; i++)
                {
                    if (i > 0) joined += "   "; // PyGame uses 3 spaces
                    joined += $"!{options[i]}";
                }
                _optionsText.text = joined;
                _optionsText.color = _settings?.optionsTextColor ?? new Color(255f / 255f, 230f / 255f, 200f / 255f);
            }
        }

        public void Hide()
        {
            _isCountdown = false;
            _isLive = false;
            _chaosVisualActive = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Smoothly animate tint alpha (PyGame: lerps with dt * 6.0)
            float targetAlpha = _chaosVisualActive
                ? (_settings != null ? _settings.chaosTintAlpha : 75f / 255f)
                : 0f;
            float lerpSpeed = _settings?.tintAlphaLerpSpeed ?? 6f;
            _currentTintAlpha += (targetAlpha - _currentTintAlpha) * Mathf.Min(1f, dt * lerpSpeed);
            UpdateTint();

            if (_isCountdown)
            {
                _countdownTimer -= dt;
                // PyGame: countdown_value = max(1, int(timer) + 1)
                int displayVal = Mathf.Max(1, (int)_countdownTimer + 1);
                if (_countdownNumber != null)
                {
                    _countdownNumber.text = displayVal.ToString();

                    // Scale pulse on countdown number
                    // PyGame: pulse_scale = 1.0 + 0.08 * abs(ticks%400 - 200)/200
                    if (_settings != null)
                    {
                        float cycleSec = _settings.countdownScalePulseCycleMs / 1000f;
                        float ticks = Time.time * 1000f;
                        float cycleMs = _settings.countdownScalePulseCycleMs;
                        float halfCycle = cycleMs * 0.5f;
                        float s = _settings.countdownScaleMin
                            + (_settings.countdownScaleMax - _settings.countdownScaleMin)
                            * Mathf.Abs((ticks % cycleMs) - halfCycle) / halfCycle;
                        _countdownNumber.transform.localScale = new Vector3(s, s, 1f);
                    }
                }
            }
        }

        private void UpdateTint()
        {
            if (_tintOverlay == null) return;

            // PyGame: pulse = 0.85 + 0.15 * abs(ticks%500 - 250)/250
            float ticks = Time.time * 1000f;
            float cycleMs = _settings != null ? _settings.pulseCycleMs : 500f;
            float halfCycle = cycleMs * 0.5f;
            float pulse = 0.85f + 0.15f * Mathf.Abs((ticks % cycleMs) - halfCycle) / halfCycle;

            // PyGame: alpha = int(tint_alpha * pulse), capped at maxTintAlpha
            float alpha = _currentTintAlpha * pulse;
            float maxAlpha = _settings?.maxTintAlpha ?? 140f / 255f;
            alpha = Mathf.Clamp(alpha, 0f, maxAlpha);

            var c = _settings != null ? _settings.chaosTintColor : new Color(220f / 255f, 40f / 255f, 40f / 255f);
            c.a = alpha;
            _tintOverlay.color = c;
        }
    }
}
