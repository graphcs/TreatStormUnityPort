using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using Utilities.Inputs;

namespace SnackAttack.Screens
{
    public class GameOverScreen : BaseScreen
    {
        [Header("Background")]
        [SerializeField] private Image _background;

        [Header("Winner Display")]
        [SerializeField] private TMP_Text _winnerName;
        [SerializeField] private Image _winsImage;

        [Header("Round Results")]
        [SerializeField] private Image _menuBar;
        [SerializeField] private TMP_Text _roundsText;

        [Header("P1 Score Card")]
        [SerializeField] private RectTransform _p1ScoreBox;
        [SerializeField] private TMP_Text _p1Name;
        [SerializeField] private TMP_Text _p1ScoreLabel;
        [SerializeField] private TMP_Text _p1ScoreValue;

        [Header("P2 Score Card")]
        [SerializeField] private RectTransform _p2ScoreBox;
        [SerializeField] private TMP_Text _p2Name;
        [SerializeField] private TMP_Text _p2ScoreLabel;
        [SerializeField] private TMP_Text _p2ScoreValue;

        [Header("Menu Options")]
        [SerializeField] private TMP_Text _playAgainText;
        [SerializeField] private TMP_Text _mainMenuText;
        [SerializeField] private Image _selectIndicator;

        [Header("Celebration")]
        [SerializeField] private RectTransform _balloonContainer;
        [SerializeField] private RectTransform _confettiContainer;

        // State
        private int _selectedOption; // 0 = Play Again, 1 = Main Menu
        private string _mode;
        private bool _vsAI;
        private bool _isSingleDog;
        private bool _active;

        // Celebration
        private float _celebrationTime;
        private float _confettiSpawnAccum;
        private readonly List<BalloonData> _balloons = new();
        private readonly List<ConfettiData> _confetti = new();
        private Sprite _circleSprite;

        // Cached settings
        private UIColorsSO _colors;
        private CelebrationSettingsSO _celeb;
        private UILayoutSO _layout;
        private ControlsSO _controls;

        private struct BalloonData
        {
            public RectTransform rect;
            public Image image;
            public float x, y, speed, swayAmp, swayFreq, swayPhase;
        }

        private struct ConfettiData
        {
            public RectTransform rect;
            public Image image;
            public float x, y, speed, driftX, rotation, rotSpeed;
        }

        protected override void Awake()
        {
            base.Awake();
            _circleSprite = CreateCircleSprite(32);
        }

        public override void OnEnter(Dictionary<string, object> data)
        {
            base.OnEnter(data);

            // Cache settings
            var gm = GM;
            _colors = gm.UIColors;
            _celeb = gm.CelebrationSettings;
            _layout = gm.UILayout;
            _controls = gm.Controls;

            // Extract data from RoundManager.EndGame()
            _mode = data != null && data.ContainsKey("mode") ? (string)data["mode"] : "single_dog";
            _vsAI = data != null && data.ContainsKey("vs_ai") && (bool)data["vs_ai"];
            _isSingleDog = _mode == "single_dog";

            int winner = data != null && data.ContainsKey("winner") ? (int)data["winner"] : 0;
            int p1Score = data != null && data.ContainsKey("p1_score") ? (int)data["p1_score"] : 0;
            int p2Score = data != null && data.ContainsKey("p2_score") ? (int)data["p2_score"] : 0;
            int p1Rounds = data != null && data.ContainsKey("p1_rounds") ? (int)data["p1_rounds"] : 0;
            int p2Rounds = data != null && data.ContainsKey("p2_rounds") ? (int)data["p2_rounds"] : 0;
            string p1Name = data != null && data.ContainsKey("p1_name") ? (string)data["p1_name"] : "Player 1";
            string p2Name = data != null && data.ContainsKey("p2_name") ? (string)data["p2_name"] : "Player 2";

            Color winnerColor = _colors != null ? _colors.winnerColor : new Color32(147, 76, 48, 255);
            Color tieColor = _colors != null ? _colors.tieColor : new Color32(255, 200, 0, 255);

            // Winner display
            if (winner == 0)
            {
                _winnerName.text = "IT'S A TIE!";
                _winnerName.color = tieColor;
                if (_winsImage != null) _winsImage.gameObject.SetActive(false);
            }
            else
            {
                string winnerName = winner == 1 ? p1Name : p2Name;
                _winnerName.text = winnerName;
                _winnerName.color = winnerColor;
                if (_winsImage != null) _winsImage.gameObject.SetActive(true);
            }

            // Round results text
            if (_isSingleDog)
            {
                _roundsText.text = $"{p1Name}  rounds  {p1Rounds}";
            }
            else
            {
                _roundsText.text = $"{p1Name}  {p1Rounds}  <size=24>vs</size>  {p2Rounds}  {p2Name}";
            }

            // Score cards
            float scoreBoxSingleX = _layout != null ? _layout.scoreBoxSingleX : 319f;
            float scoreBoxY = _layout != null ? _layout.scoreBoxY : -350f;
            float scoreBoxP1X = _layout != null ? _layout.scoreBoxP1X : 72f;
            float scoreBoxP2X = _layout != null ? _layout.scoreBoxP2X : 565f;

            _p1Name.text = p1Name;
            _p1ScoreLabel.text = "score";
            _p1ScoreValue.text = p1Score.ToString();

            if (_isSingleDog)
            {
                _p1ScoreBox.anchoredPosition = new Vector2(scoreBoxSingleX, scoreBoxY);
                _p2ScoreBox.gameObject.SetActive(false);
            }
            else
            {
                _p1ScoreBox.anchoredPosition = new Vector2(scoreBoxP1X, scoreBoxY);
                _p2ScoreBox.anchoredPosition = new Vector2(scoreBoxP2X, scoreBoxY);
                _p2ScoreBox.gameObject.SetActive(true);
                _p2Name.text = p2Name;
                _p2ScoreLabel.text = "score";
                _p2ScoreValue.text = p2Score.ToString();
            }

            // Menu selection
            _selectedOption = 0;
            UpdateMenuVisuals();

            // Celebration
            _celebrationTime = 0f;
            _confettiSpawnAccum = 0f;
            InitBalloons();
            ClearConfetti();

            _active = true;

            string bgMusic = _controls != null ? _controls.backgroundMusic : "background";
            PlayMusic(bgMusic);
        }

        public override void OnExit()
        {
            _active = false;
            ClearBalloons();
            ClearConfetti();
            base.OnExit();
        }

        private void Update()
        {
            if (!_active) return;

            HandleInput();
            UpdateBalloons();
            UpdateConfetti();

            _celebrationTime += Time.deltaTime;
        }

        // --- Input ---

        private void HandleInput()
        {
            if (!InputsManager.Started) return;

            string vAxis = _controls != null ? _controls.verticalAxis : "Vertical";
            string submitAction = _controls != null ? _controls.submitAction : "Submit";
            string cancelAction = _controls != null ? _controls.cancelAction : "Cancel";
            string selectSnd = _controls != null ? _controls.selectSound : "select";

            // Keyboard
            if (InputsManager.InputNegativeDown(vAxis))
            {
                // Down
                if (_selectedOption < 1)
                {
                    _selectedOption = 1;
                    UpdateMenuVisuals();
                    PlaySound(selectSnd);
                }
            }
            else if (InputsManager.InputPositiveDown(vAxis))
            {
                // Up
                if (_selectedOption > 0)
                {
                    _selectedOption = 0;
                    UpdateMenuVisuals();
                    PlaySound(selectSnd);
                }
            }
            else if (InputsManager.InputDown(submitAction))
            {
                ActivateSelected();
            }
            else if (InputsManager.InputDown(cancelAction))
            {
                string snd = _controls != null ? _controls.selectSound : "select";
                PlaySound(snd);
                ChangeState(GameState.MainMenu);
            }

            // Mouse
            HandleMouseInput();
        }

        private void HandleMouseInput()
        {
            Vector2 mousePos = InputsManager.InputMousePosition();

            // Check Play Again
            if (CheckTextHover(_playAgainText, mousePos, 0)) return;
            // Check Main Menu
            CheckTextHover(_mainMenuText, mousePos, 1);
        }

        private bool CheckTextHover(TMP_Text text, Vector2 mousePos, int optionIndex)
        {
            if (text == null) return false;

            var rect = text.rectTransform;
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
            {
                if (_selectedOption != optionIndex)
                {
                    _selectedOption = optionIndex;
                    UpdateMenuVisuals();
                    string selectSnd = _controls != null ? _controls.selectSound : "select";
                    PlaySound(selectSnd);
                }

                if (InputsManager.InputMouseButtonUp(0))
                {
                    ActivateSelected();
                }
                return true;
            }
            return false;
        }

        private void ActivateSelected()
        {
            string selectSnd = _controls != null ? _controls.selectSound : "select";
            PlaySound(selectSnd);

            if (_selectedOption == 0)
            {
                // Play Again → CharacterSelect with same mode/vs_ai
                ChangeState(GameState.CharacterSelect, new Dictionary<string, object>
                {
                    { "mode", _mode },
                    { "vs_ai", _vsAI }
                });
            }
            else
            {
                // Main Menu
                ChangeState(GameState.MainMenu);
            }
        }

        // --- Menu Visuals ---

        private void UpdateMenuVisuals()
        {
            Color menuNormal = _colors != null ? _colors.menuNormal : new Color32(77, 43, 31, 255);
            Color menuSelected = _colors != null ? _colors.menuSelected : new Color32(147, 76, 48, 255);

            if (_playAgainText != null)
                _playAgainText.color = _selectedOption == 0 ? menuSelected : menuNormal;

            if (_mainMenuText != null)
                _mainMenuText.color = _selectedOption == 1 ? menuSelected : menuNormal;

            UpdateSelectIndicator();
        }

        private void UpdateSelectIndicator()
        {
            if (_selectIndicator == null) return;

            TMP_Text selectedText = _selectedOption == 0 ? _playAgainText : _mainMenuText;
            if (selectedText == null) return;

            float selectorPadding = _layout != null ? _layout.selectorPadding : 10f;

            _selectIndicator.enabled = true;
            var textRect = selectedText.rectTransform;
            float iconX = textRect.anchoredPosition.x - (selectedText.preferredWidth / 2f)
                          - _selectIndicator.rectTransform.sizeDelta.x - selectorPadding;
            _selectIndicator.rectTransform.anchoredPosition =
                new Vector2(iconX, textRect.anchoredPosition.y);
        }

        // --- Balloons ---

        private void InitBalloons()
        {
            ClearBalloons();

            int count = _celeb != null ? _celeb.balloonCount : 12;
            Color32[] festiveColors = _colors != null ? _colors.festiveColors : new Color32[]
            {
                new(255, 80, 80, 255), new(80, 200, 255, 255), new(255, 220, 60, 255),
                new(120, 255, 120, 255), new(255, 140, 200, 255), new(180, 120, 255, 255),
                new(255, 160, 60, 255)
            };

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Balloon_{i}");
                var rect = go.AddComponent<RectTransform>();
                rect.SetParent(_balloonContainer, false);
                var img = go.AddComponent<Image>();
                img.sprite = _circleSprite;
                img.color = festiveColors[Random.Range(0, festiveColors.Length)];
                img.raycastTarget = false;

                int radMin = _celeb != null ? _celeb.radiusMin : 18;
                int radMax = _celeb != null ? _celeb.radiusMax : 29;
                float ovalRatio = _celeb != null ? _celeb.ovalHeightRatio : 2.4f;
                int radius = Random.Range(radMin, radMax);
                rect.sizeDelta = new Vector2(radius * 2, radius * ovalRatio);
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0.5f, 0.5f);

                float xMin = _celeb != null ? _celeb.spawnXMin : 30f;
                float xMax = _celeb != null ? _celeb.spawnXMax : 1170f;
                float yMin = _celeb != null ? _celeb.spawnYMin : -950f;
                float yMax = _celeb != null ? _celeb.spawnYMax : -300f;
                float x = Random.Range(xMin, xMax);
                float y = Random.Range(yMin, yMax);
                rect.anchoredPosition = new Vector2(x, y);

                float spdMin = _celeb != null ? _celeb.speedMin : 40f;
                float spdMax = _celeb != null ? _celeb.speedMax : 75f;
                float swAmpMin = _celeb != null ? _celeb.swayAmpMin : 15f;
                float swAmpMax = _celeb != null ? _celeb.swayAmpMax : 35f;
                float swFrqMin = _celeb != null ? _celeb.swayFreqMin : 0.8f;
                float swFrqMax = _celeb != null ? _celeb.swayFreqMax : 1.6f;

                _balloons.Add(new BalloonData
                {
                    rect = rect, image = img,
                    x = x, y = y,
                    speed = Random.Range(spdMin, spdMax),
                    swayAmp = Random.Range(swAmpMin, swAmpMax),
                    swayFreq = Random.Range(swFrqMin, swFrqMax),
                    swayPhase = Random.Range(0f, Mathf.PI * 2)
                });
            }
        }

        private void UpdateBalloons()
        {
            float respawnThreshold = _celeb != null ? _celeb.respawnThresholdY : 50f;
            float respawnFloor = _celeb != null ? _celeb.respawnFloorY : -1050f;
            float xMin = _celeb != null ? _celeb.spawnXMin : 30f;
            float xMax = _celeb != null ? _celeb.spawnXMax : 1170f;
            float spdMin = _celeb != null ? _celeb.speedMin : 40f;
            float spdMax = _celeb != null ? _celeb.speedMax : 75f;
            Color32[] festiveColors = _colors != null ? _colors.festiveColors : new Color32[]
            {
                new(255, 80, 80, 255), new(80, 200, 255, 255), new(255, 220, 60, 255),
                new(120, 255, 120, 255), new(255, 140, 200, 255), new(180, 120, 255, 255),
                new(255, 160, 60, 255)
            };

            for (int i = 0; i < _balloons.Count; i++)
            {
                var b = _balloons[i];
                b.y += b.speed * Time.deltaTime;

                if (b.y > respawnThreshold)
                {
                    b.y = respawnFloor;
                    b.x = Random.Range(xMin, xMax);
                    b.speed = Random.Range(spdMin, spdMax);
                    b.image.color = festiveColors[Random.Range(0, festiveColors.Length)];
                }

                float swayX = Mathf.Sin(_celebrationTime * b.swayFreq + b.swayPhase) * b.swayAmp;
                b.rect.anchoredPosition = new Vector2(b.x + swayX, b.y);
                _balloons[i] = b;
            }
        }

        private void ClearBalloons()
        {
            for (int i = _balloons.Count - 1; i >= 0; i--)
            {
                if (_balloons[i].rect != null)
                    Destroy(_balloons[i].rect.gameObject);
            }
            _balloons.Clear();
        }

        // --- Confetti ---

        private void UpdateConfetti()
        {
            float spawnRate = _celeb != null ? _celeb.confettiSpawnRate : 18f;
            int maxConfetti = _celeb != null ? _celeb.maxConfetti : 200;
            float removalY = _celeb != null ? _celeb.confettiRemovalY : -1050f;
            float refWidth = GM.GameSettings != null ? GM.GameSettings.referenceWidth : 1200f;
            float spawnYMin = _celeb != null ? _celeb.confettiSpawnYMin : 5f;
            float spawnYMax = _celeb != null ? _celeb.confettiSpawnYMax : 20f;

            // Spawn new confetti
            _confettiSpawnAccum += Time.deltaTime * spawnRate;
            while (_confettiSpawnAccum >= 1f && _confetti.Count < maxConfetti)
            {
                _confettiSpawnAccum -= 1f;
                SpawnConfettiPiece(Random.Range(0f, refWidth), Random.Range(spawnYMin, spawnYMax));
            }
            if (_confettiSpawnAccum >= 1f) _confettiSpawnAccum = 0f;

            // Update existing confetti
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                var c = _confetti[i];
                c.y -= c.speed * Time.deltaTime;
                c.x += c.driftX * Time.deltaTime;
                c.rotation += c.rotSpeed * Time.deltaTime;

                c.rect.anchoredPosition = new Vector2(c.x, c.y);
                c.rect.localEulerAngles = new Vector3(0, 0, c.rotation);

                if (c.y < removalY)
                {
                    Destroy(c.rect.gameObject);
                    _confetti.RemoveAt(i);
                    continue;
                }

                _confetti[i] = c;
            }
        }

        private void SpawnConfettiPiece(float x, float startY)
        {
            Color32[] festiveColors = _colors != null ? _colors.festiveColors : new Color32[]
            {
                new(255, 80, 80, 255), new(80, 200, 255, 255), new(255, 220, 60, 255),
                new(120, 255, 120, 255), new(255, 140, 200, 255), new(180, 120, 255, 255),
                new(255, 160, 60, 255)
            };

            var go = new GameObject("Confetti");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(_confettiContainer, false);
            var img = go.AddComponent<Image>();
            img.color = festiveColors[Random.Range(0, festiveColors.Length)];
            img.raycastTarget = false;

            float wMin = _celeb != null ? _celeb.confettiWidthMin : 4f;
            float wMax = _celeb != null ? _celeb.confettiWidthMax : 9f;
            float hMin = _celeb != null ? _celeb.confettiHeightMin : 8f;
            float hMax = _celeb != null ? _celeb.confettiHeightMax : 16f;
            float w = Random.Range(wMin, wMax);
            float h = Random.Range(hMin, hMax);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, -startY);

            float spdMin = _celeb != null ? _celeb.confettiSpeedMin : 60f;
            float spdMax = _celeb != null ? _celeb.confettiSpeedMax : 130f;
            float driftMin = _celeb != null ? _celeb.confettiDriftMin : -30f;
            float driftMax = _celeb != null ? _celeb.confettiDriftMax : 30f;
            float rotMin = _celeb != null ? _celeb.confettiRotSpeedMin : 120f;
            float rotMax = _celeb != null ? _celeb.confettiRotSpeedMax : 360f;

            _confetti.Add(new ConfettiData
            {
                rect = rect, image = img,
                x = x, y = -startY,
                speed = Random.Range(spdMin, spdMax),
                driftX = Random.Range(driftMin, driftMax),
                rotation = Random.Range(0f, 360f),
                rotSpeed = Random.Range(rotMin, rotMax)
            });
        }

        private void ClearConfetti()
        {
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                if (_confetti[i].rect != null)
                    Destroy(_confetti[i].rect.gameObject);
            }
            _confetti.Clear();
            _confettiSpawnAccum = 0f;
        }

        // --- Helpers ---

        private static Sprite CreateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radiusSq = center * center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= radiusSq
                        ? Color.white
                        : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
