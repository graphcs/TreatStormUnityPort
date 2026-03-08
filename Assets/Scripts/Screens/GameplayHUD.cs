using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Gameplay;
using Utilities.Inputs;

namespace SnackAttack.Screens
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Menu Bar")]
        [SerializeField] private TMP_Text _roundText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _winsText;

        [Header("P1 Score")]
        [SerializeField] private TMP_Text _p1Name;
        [SerializeField] private TMP_Text _p1ScoreLabel;
        [SerializeField] private TMP_Text _p1ScoreValue;

        [Header("P2 Score")]
        [SerializeField] private TMP_Text _p2Name;
        [SerializeField] private TMP_Text _p2ScoreLabel;
        [SerializeField] private TMP_Text _p2ScoreValue;

        [Header("Countdown")]
        [SerializeField] private TMP_Text _countdownText;

        [Header("Popups")]
        [SerializeField] private RectTransform _popupContainer;

        [Header("Placeholders")]
        [SerializeField] private GameObject _announcementGroup;
        [SerializeField] private GameObject _votingMeterPlaceholder;

        [Header("Pause Overlay")]
        [SerializeField] private GameObject _pauseOverlay;
        [SerializeField] private TMP_Text _pauseTitle;
        [SerializeField] private TMP_Text _pauseResume;
        [SerializeField] private TMP_Text _pauseQuit;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset _daydreamFont;

        private CanvasGroup _canvasGroup;
        private RoundManager _roundManager;
        private bool _initialized;
        private bool _isPaused;
        public bool IsPaused => _isPaused;

        // GO! display
        private float _goTimer;
        private bool _showingGo;
        private RoundPhase _lastPhase;

        // Popup tracking
        private readonly List<PointPopup> _activePopups = new();

        // Cached settings
        private GameSettingsSO _settings;
        private UIColorsSO _colors;
        private UILayoutSO _layout;
        private ControlsSO _controls;

        private struct PointPopup
        {
            public GameObject go;
            public TMP_Text text;
            public RectTransform rect;
            public float timer;
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Start hidden
            SetVisible(false);
        }

        public void Initialize(RoundManager rm)
        {
            _roundManager = rm;
            _initialized = true;
            _lastPhase = RoundPhase.Inactive;
            _showingGo = false;
            _goTimer = 0f;
            _isPaused = false;
            if (_pauseOverlay != null) _pauseOverlay.SetActive(false);

            // Cache settings
            var gm = GameManager.Instance;
            _settings = gm.GameSettings;
            _colors = gm.UIColors;
            _layout = gm.UILayout;
            _controls = gm.Controls;

            // Set player names
            var p1 = rm.Player1;
            var p2 = rm.Player2;

            if (p1 != null && p1.CharacterData != null)
                _p1Name.text = p1.CharacterData.displayName;
            else
                _p1Name.text = "Player 1";

            _p1ScoreLabel.text = "score";
            _p1ScoreValue.text = "0";

            if (p2 != null)
            {
                if (p2.CharacterData != null)
                    _p2Name.text = p2.CharacterData.displayName;
                else
                    _p2Name.text = "Player 2";

                _p2ScoreLabel.text = "score";
                _p2ScoreValue.text = "0";
                SetScoreGroupVisible(_p2Name, _p2ScoreLabel, _p2ScoreValue, true);
            }
            else
            {
                SetScoreGroupVisible(_p2Name, _p2ScoreLabel, _p2ScoreValue, false);
            }

            // Set initial round/wins
            UpdateRoundText();
            UpdateWinsText();

            // Subscribe to events
            EventBus.Subscribe(GameEvent.ScoreChanged, OnScoreChanged);
            EventBus.Subscribe(GameEvent.PointPopupRequested, OnPointPopupRequested);

            // Clear old popups
            ClearPopups();
        }

        public void Show()
        {
            SetVisible(true);
            _countdownText.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (_isPaused) Resume();

            SetVisible(false);

            // Unsubscribe
            EventBus.Unsubscribe(GameEvent.ScoreChanged, OnScoreChanged);
            EventBus.Unsubscribe(GameEvent.PointPopupRequested, OnPointPopupRequested);

            ClearPopups();
            _initialized = false;
        }

        private void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
            gameObject.SetActive(visible);
        }

        private void Update()
        {
            if (!_initialized || _roundManager == null)
                return;

            string cancelAction = _controls != null ? _controls.cancelAction : "Cancel";
            string submitAction = _controls != null ? _controls.submitAction : "Submit";
            string quitAction = _controls != null ? _controls.quitAction : "Quit";

            // Pause input (Input.GetKeyDown works at timeScale=0)
            if (InputsManager.InputDown(cancelAction))
            {
                if (_isPaused) Resume();
                else Pause();
                return;
            }

            if (_isPaused)
            {
                if (InputsManager.InputDown(submitAction)) Resume();
                else if (InputsManager.InputDown(quitAction)) QuitToMenu();
                return; // skip all HUD updates while paused
            }

            var phase = _roundManager.CurrentPhase;

            // Detect phase transition from Countdown to Active -> show "GO!"
            float goDisplayDuration = _settings != null ? _settings.goDisplayDuration : 0.5f;
            if (_lastPhase == RoundPhase.Countdown && phase == RoundPhase.Active)
            {
                _showingGo = true;
                _goTimer = goDisplayDuration;
                _countdownText.text = "GO!";
                _countdownText.gameObject.SetActive(true);
            }
            _lastPhase = phase;

            // Update based on phase
            switch (phase)
            {
                case RoundPhase.Countdown:
                    UpdateCountdownDisplay();
                    break;

                case RoundPhase.Active:
                    UpdateActiveDisplay();
                    break;

                case RoundPhase.RoundEnd:
                    UpdateRoundEndDisplay();
                    break;
            }

            // Update GO! timer
            if (_showingGo)
            {
                _goTimer -= Time.deltaTime;
                if (_goTimer <= 0f)
                {
                    _showingGo = false;
                    _countdownText.gameObject.SetActive(false);
                }
            }

            // Update popups
            UpdatePopups();
        }

        private void UpdateCountdownDisplay()
        {
            int value = _roundManager.CountdownValue;
            if (value > 0)
            {
                _countdownText.text = value.ToString();
                _countdownText.gameObject.SetActive(true);
            }

            _timerText.gameObject.SetActive(false);
            UpdateRoundText();
            UpdateWinsText();
        }

        private void UpdateActiveDisplay()
        {
            if (!_showingGo)
                _countdownText.gameObject.SetActive(false);

            // Timer — only show during active round (matches PyGame)
            _timerText.gameObject.SetActive(true);
            _timerText.text = $"{Mathf.CeilToInt(_roundManager.RoundTimer)}s";

            UpdateRoundText();
            UpdateWinsText();
            UpdateScoreDisplay();
        }

        private void UpdateRoundEndDisplay()
        {
            _countdownText.gameObject.SetActive(false);
            _timerText.text = "0s";
            UpdateWinsText();
            UpdateScoreDisplay();
        }

        private void UpdateRoundText()
        {
            _roundText.text = $"round {_roundManager.CurrentRound}";
        }

        private void UpdateWinsText()
        {
            if (_roundManager.Mode == "single_dog")
            {
                _winsText.text = $"round wins {_roundManager.P1RoundWins}";
                _winsText.fontSize = 20;
            }
            else
            {
                int p1w = _roundManager.P1RoundWins;
                int p2w = _roundManager.P2RoundWins;
                _winsText.text = $"<size=28>{p1w}</size>  <size=14>vs</size>  <size=28>{p2w}</size>";
            }
        }

        private void UpdateScoreDisplay()
        {
            var p1 = _roundManager.Player1;
            var p2 = _roundManager.Player2;

            if (p1 != null)
                _p1ScoreValue.text = p1.Score.ToString();

            if (p2 != null)
                _p2ScoreValue.text = p2.Score.ToString();
        }

        // --- Events ---

        private void OnScoreChanged(EventData data)
        {
            UpdateScoreDisplay();
        }

        private void OnPointPopupRequested(EventData data)
        {
            int points = (int)data["points"];
            Vector3 position = (Vector3)data["position"];
            SpawnPopup(points, position);
        }

        // --- Point Popups ---

        private void SpawnPopup(int points, Vector3 worldPosition)
        {
            if (_popupContainer == null)
                return;

            float popupDuration = _settings != null ? _settings.popupDuration : 1.0f;
            int popupFontSize = _settings != null ? _settings.popupFontSize : 24;
            Vector2 popupSize = _layout != null ? _layout.popupSize : new Vector2(200f, 50f);
            float outlineWidth = _layout != null ? _layout.outlineWidth : 0.25f;
            Color positiveColor = _colors != null ? _colors.popupPositiveColor : new Color32(81, 180, 71, 255);
            Color negativeColor = _colors != null ? _colors.popupNegativeColor : new Color32(222, 97, 91, 255);
            Color outlineColor = _colors != null ? _colors.popupOutlineColor : Color.white;

            var go = new GameObject("PointPopup");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(_popupContainer, false);

            // Entities emit center-origin coords (GameplayRoot anchor 0.5,0.5).
            // Convert to top-left origin for UICanvas popup placement.
            float halfRefW = _settings != null ? _settings.referenceWidth * 0.5f : 600f;
            float halfRefH = _settings != null ? _settings.referenceHeight * 0.5f : 500f;
            float canvasX = worldPosition.x + halfRefW;
            float canvasY = worldPosition.y - halfRefH;

            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(canvasX, canvasY);
            rect.sizeDelta = popupSize;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = points >= 0 ? $"+{points}" : $"{points}";
            tmp.font = _daydreamFont;
            tmp.fontSize = popupFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            tmp.color = points >= 0 ? positiveColor : negativeColor;
            tmp.outlineWidth = outlineWidth;
            tmp.outlineColor = outlineColor;

            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            _activePopups.Add(new PointPopup
            {
                go = go,
                text = tmp,
                rect = rect,
                timer = popupDuration
            });
        }

        private void UpdatePopups()
        {
            float dt = Time.deltaTime;
            float floatSpeed = _settings != null ? _settings.popupFloatSpeed : 50f;

            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                var popup = _activePopups[i];
                popup.timer -= dt;

                var pos = popup.rect.anchoredPosition;
                pos.y += floatSpeed * dt;
                popup.rect.anchoredPosition = pos;

                _activePopups[i] = popup;

                if (popup.timer <= 0f)
                {
                    Destroy(popup.go);
                    _activePopups.RemoveAt(i);
                }
            }
        }

        private void ClearPopups()
        {
            foreach (var popup in _activePopups)
            {
                if (popup.go != null)
                    Destroy(popup.go);
            }
            _activePopups.Clear();
        }

        private static void SetScoreGroupVisible(TMP_Text name, TMP_Text label, TMP_Text value, bool visible)
        {
            if (name != null) name.gameObject.SetActive(visible);
            if (label != null) label.gameObject.SetActive(visible);
            if (value != null) value.gameObject.SetActive(visible);
        }

        // --- Pause ---

        private void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            if (_pauseOverlay != null) _pauseOverlay.SetActive(true);
            EventBus.Emit(GameEvent.GamePaused);
        }

        private void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            if (_pauseOverlay != null) _pauseOverlay.SetActive(false);
            EventBus.Emit(GameEvent.GameResumed);
        }

        private void QuitToMenu()
        {
            _isPaused = false;
            Time.timeScale = 1f; // restore before transition
            if (_pauseOverlay != null) _pauseOverlay.SetActive(false);
            GameManager.Instance.StateMachine.ChangeState(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                EventBus.Unsubscribe(GameEvent.ScoreChanged, OnScoreChanged);
                EventBus.Unsubscribe(GameEvent.PointPopupRequested, OnPointPopupRequested);
            }
            ClearPopups();
        }
    }
}
