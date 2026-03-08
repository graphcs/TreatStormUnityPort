using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Entities;
using SnackAttack.Screens;

namespace SnackAttack.Gameplay
{
    public enum RoundPhase { Inactive, Countdown, Active, RoundEnd }

    public class RoundManager : MonoBehaviour, IScreen
    {
        [SerializeField] private GameplayHUD _hud;
        [SerializeField] private GameplayBackground _background;
        [SerializeField] private RectTransform _gameplayCanvasTransform;

        // Match state
        private string _mode;
        private bool _vsAI;
        private bool _isSingleDog;
        private int _maxRounds;
        private int _currentRound;
        private int _currentLevelNumber;
        private int _p1RoundWins;
        private int _p2RoundWins;

        // Round state
        private RoundPhase _phase;
        private float _roundTimer;
        private float _roundDuration;

        // Countdown
        private int _countdownValue;
        private float _countdownTimer;

        // Crowd Chaos stub (Step 23)
        private bool _crowdChaosTriggered;
        private bool _crowdChaosCountdownActive;
        private float _crowdChaosCountdownTimer;

        // Scene objects (created dynamically)
        private GameObject _gameplayRoot;
        private RectTransform _gameplayRootRect;
        private Arena _arena1;
        private Arena _arena2;
        private GameObject _player1Go;
        private GameObject _player2Go;
        private PlayerController _player1;
        private PlayerController _player2;
        private RectTransform _leashAnchor1;
        private RectTransform _leashAnchor2;

        // Cached settings
        private GameSettingsSO _settings;

        // Public accessors (for HUD, Step 17)
        public int CurrentRound => _currentRound;
        public int MaxRounds => _maxRounds;
        public float RoundTimer => _roundTimer;
        public float RoundDuration => _roundDuration;
        public bool IsRoundActive => _phase == RoundPhase.Active;
        public RoundPhase CurrentPhase => _phase;
        public int CountdownValue => _countdownValue;
        public int P1RoundWins => _p1RoundWins;
        public int P2RoundWins => _p2RoundWins;
        public PlayerController Player1 => _player1;
        public PlayerController Player2 => _player2;
        public Arena Arena1 => _arena1;
        public Arena Arena2 => _arena2;
        public string Mode => _mode;
        public string CurrentLevelName => GetCurrentLevel()?.levelName ?? "";

        private void Start()
        {
            GameManager.Instance.StateMachine.RegisterScreen(GameState.Gameplay, this);
        }

        public void OnEnter(Dictionary<string, object> data)
        {
            _settings = GameManager.Instance.GameSettings;

            _mode = data.TryGetValue("mode", out var m) ? (string)m : "1p";
            _vsAI = data.TryGetValue("vs_ai", out var ai) && (bool)ai;

            var charDb = GameManager.Instance.CharacterDatabase;
            CharacterSO p1Char = charDb.characters[0];
            CharacterSO p2Char = null;
            if (data.TryGetValue("p1_character", out var p1Val))
                p1Char = p1Val is CharacterSO p1So ? p1So : charDb.GetById((string)p1Val);
            if (data.TryGetValue("p2_character", out var p2Val) && p2Val != null)
                p2Char = p2Val is CharacterSO p2So ? p2So : charDb.GetById((string)p2Val);

            _isSingleDog = (_mode == "single_dog");
            _currentRound = 1;
            _currentLevelNumber = 1;
            _p1RoundWins = 0;
            _p2RoundWins = 0;
            _maxRounds = _settings.roundsPerGame;

            // Create GameplayRoot with RectTransform under GameplayCanvas
            _gameplayRoot = new GameObject("GameplayRoot");
            _gameplayRootRect = _gameplayRoot.AddComponent<RectTransform>();
            _gameplayRootRect.SetParent(_gameplayCanvasTransform, false);
            _gameplayRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            _gameplayRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            _gameplayRootRect.pivot = new Vector2(0.5f, 0.5f);
            _gameplayRootRect.anchoredPosition = Vector2.zero;
            _gameplayRootRect.sizeDelta = new Vector2(_settings.referenceWidth, _settings.referenceHeight);

            SetupArenas(p1Char, p2Char);

            if (_background != null)
            {
                _background.Show(GetCurrentLevel());
                _background.SetSinglePlayer(_isSingleDog);
            }

            if (_hud != null)
            {
                _hud.Initialize(this);
                _hud.Show();
            }

            EventBus.Emit(GameEvent.GameStart);

            var controls = GameManager.Instance.Controls;
            string music = controls != null ? controls.gameplayMusic : "Gameplay";
            EventBus.Emit(GameEvent.PlayMusic, new Dictionary<string, object>
            {
                { "track", music }
            });

            StartCountdown();
        }

        public void OnExit()
        {
            _phase = RoundPhase.Inactive;

            if (_hud != null)
                _hud.Hide();

            if (_background != null)
                _background.Hide();

            if (_gameplayRoot != null)
                Destroy(_gameplayRoot);
            _gameplayRoot = null;
            _gameplayRootRect = null;
            _arena1 = _arena2 = null;
            _player1Go = _player2Go = null;
            _player1 = _player2 = null;
            _leashAnchor1 = _leashAnchor2 = null;
        }

        private void Update()
        {
            switch (_phase)
            {
                case RoundPhase.Countdown:
                    UpdateCountdown();
                    break;
                case RoundPhase.Active:
                    UpdateActiveRound();
                    break;
            }
        }

        // --- Setup ---

        private void SetupArenas(CharacterSO p1Char, CharacterSO p2Char)
        {
            float w = _settings.arenaWidth;
            float h = _settings.arenaHeight;
            float gap = _settings.splitScreenGap;
            float margin = _settings.arenaBottomMargin;
            float yMin = -(_settings.referenceHeight * 0.5f) + margin;

            if (_isSingleDog)
            {
                var bounds1 = new Rect(-w / 2f, yMin, w, h);
                CreateArena(1, bounds1);
                CreatePlayer(1, p1Char, bounds1, _arena1, _leashAnchor1, true, false);
            }
            else
            {
                float totalW = w * 2f + gap;
                float startX = -totalW / 2f;
                var bounds1 = new Rect(startX, yMin, w, h);
                var bounds2 = new Rect(startX + w + gap, yMin, w, h);

                CreateArena(1, bounds1);
                CreateArena(2, bounds2);
                CreatePlayer(1, p1Char, bounds1, _arena1, _leashAnchor1, false, false);
                CreatePlayer(2, p2Char, bounds2, _arena2, _leashAnchor2, false, _vsAI);
            }
        }

        private void CreateArena(int index, Rect bounds)
        {
            LevelSO level = GetCurrentLevel();
            var go = new GameObject($"Arena_{index}");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(_gameplayRootRect, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var arena = go.AddComponent<Arena>();
            float spawnMul = _settings.arenaSpawnMultiplier;
            float spawnW = bounds.width * spawnMul;
            float spawnInset = (bounds.width - spawnW) * 0.5f;
            var spawnBounds = new Rect(bounds.xMin + spawnInset, bounds.yMin, spawnW, bounds.height);
            arena.Initialize(bounds, level, GameManager.Instance.SnackDatabase, _gameplayRootRect, spawnBounds);
            arena.SetSpawningEnabled(false);

            // Leash anchor parented to GameplayRoot (same coord space as players)
            var anchor = new GameObject($"LeashAnchor_{index}");
            var anchorRect = anchor.AddComponent<RectTransform>();
            anchorRect.SetParent(_gameplayRootRect, false);
            anchorRect.anchorMin = new Vector2(0.5f, 0.5f);
            anchorRect.anchorMax = new Vector2(0.5f, 0.5f);
            anchorRect.pivot = new Vector2(0.5f, 0.5f);

            float anchorPadding = _settings.leashAnchorPadding;
            float anchorYOffset = _settings.leashAnchorYOffset;

            float anchorX = index == 1
                ? bounds.xMin + anchorPadding
                : bounds.xMin + bounds.width - anchorPadding;
            float anchorY = bounds.yMin + anchorYOffset;
            anchorRect.anchoredPosition = new Vector2(anchorX, anchorY);

            if (index == 1)
            {
                _arena1 = arena;
                _leashAnchor1 = anchorRect;
            }
            else
            {
                _arena2 = arena;
                _leashAnchor2 = anchorRect;
            }
        }

        private void CreatePlayer(int playerNum, CharacterSO charSO, Rect bounds,
            Arena arena, RectTransform leashAnchor, bool isSinglePlayer, bool isAI)
        {
            var go = new GameObject($"Player_{playerNum}");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(_gameplayRootRect, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float gpSize = charSO != null ? charSO.gameplaySize : 216f;
            rect.sizeDelta = new Vector2(gpSize, gpSize);

            // 1. PlayerController
            var pc = go.AddComponent<PlayerController>();
            pc.Configure(charSO, playerNum, horizontal: true);
            pc.InitializeArena(bounds);

            // 2. CharacterAnimator (reads PlayerController in Awake)
            go.AddComponent<CharacterAnimator>();

            // 3. SnackCollector (manual overlap, no physics)
            var collector = go.AddComponent<SnackCollector>();
            collector.SetArena(arena);

            // 4. LeashRenderer
            var leash = go.AddComponent<LeashRenderer>();
            leash.SetAnchorPoint(leashAnchor);

            // 4.5. VFX Controller
            go.AddComponent<SnackAttack.Effects.PlayerVFXController>();

            // 5. Input handling
            var input = go.AddComponent<PlayerInputHandler>();
            bool useSinglePlayerInput = isSinglePlayer || (_vsAI && playerNum == 1);
            input.Configure(playerNum - 1, useSinglePlayerInput);

            // 6. AI (if applicable)
            if (isAI)
            {
                var ai = go.AddComponent<AIController>();
                ai.Configure(GameManager.Instance.AIDifficulty, arena);
            }

            // Disable player movement during countdown
            pc.enabled = false;

            if (playerNum == 1)
            {
                _player1Go = go;
                _player1 = pc;
            }
            else
            {
                _player2Go = go;
                _player2 = pc;
            }
        }

        // --- Countdown ---

        private void StartCountdown()
        {
            _phase = RoundPhase.Countdown;
            _countdownValue = _settings.countdownStart;
            _countdownTimer = _settings.countdownTickDuration;

            if (_player1 != null) _player1.enabled = false;
            if (_player2 != null) _player2.enabled = false;

            EventBus.Emit(GameEvent.PlaySound, new Dictionary<string, object>
            {
                { "sound", "countdown_2_3" }
            });
        }

        private void UpdateCountdown()
        {
            _countdownTimer -= Time.deltaTime;
            if (_countdownTimer <= 0f)
            {
                _countdownValue--;
                if (_countdownValue > 0)
                {
                    _countdownTimer = _settings.countdownTickDuration;
                    EventBus.Emit(GameEvent.PlaySound, new Dictionary<string, object>
                    {
                        { "sound", _countdownValue == 1 ? "countdown_1" : "countdown_2_3" }
                    });
                }
                else
                {
                    StartRound();
                }
            }
        }

        // --- Round Lifecycle ---

        private void StartRound()
        {
            _phase = RoundPhase.Active;

            LevelSO level = GetCurrentLevel();
            _roundDuration = level.roundDurationSeconds;
            _roundTimer = _roundDuration;

            _crowdChaosTriggered = false;
            _crowdChaosCountdownActive = false;

            ResetPlayerForRound(_player1Go, _player1);
            if (_player2 != null)
                ResetPlayerForRound(_player2Go, _player2);

            _arena1.ResetForNewRound();
            _arena1.SetSpawningEnabled(true);
            if (_arena2 != null)
            {
                _arena2.ResetForNewRound();
                _arena2.SetSpawningEnabled(true);
            }

            EventBus.Emit(GameEvent.RoundStart, new Dictionary<string, object>
            {
                { "round", _currentRound },
                { "level", _currentLevelNumber },
                { "duration", _roundDuration }
            });
        }

        private void UpdateActiveRound()
        {
            _roundTimer -= Time.deltaTime;

            float elapsed = _roundDuration - _roundTimer;
            float chaosThreshold = _settings.crowdChaosThreshold;
            if (!_crowdChaosTriggered && !_crowdChaosCountdownActive
                && elapsed >= chaosThreshold)
            {
                StartCrowdChaosCountdown();
            }

            if (_crowdChaosCountdownActive)
            {
                _crowdChaosCountdownTimer -= Time.deltaTime;
                if (_crowdChaosCountdownTimer <= 0f)
                    ActivateCrowdChaos();
            }

            if (_roundTimer <= 0f)
            {
                _roundTimer = 0f;
                EndRound();
            }
        }

        private void StartCrowdChaosCountdown()
        {
            _crowdChaosTriggered = true;
            _crowdChaosCountdownActive = true;
            _crowdChaosCountdownTimer = _settings.crowdChaosCountdown;
        }

        private void ActivateCrowdChaos()
        {
            _crowdChaosCountdownActive = false;
        }

        private void EndRound()
        {
            _phase = RoundPhase.RoundEnd;

            _arena1.SetSpawningEnabled(false);
            if (_arena2 != null) _arena2.SetSpawningEnabled(false);

            _player1.enabled = false;
            if (_player2 != null) _player2.enabled = false;

            int p1Score = _player1.Score;
            int p2Score = _player2 != null ? _player2.Score : 0;

            if (_isSingleDog)
                _p1RoundWins++;
            else if (p1Score > p2Score)
                _p1RoundWins++;
            else if (p2Score > p1Score)
                _p2RoundWins++;

            EventBus.Emit(GameEvent.RoundEnd, new Dictionary<string, object>
            {
                { "round", _currentRound },
                { "p1Score", p1Score },
                { "p2Score", p2Score },
                { "p1RoundWins", _p1RoundWins },
                { "p2RoundWins", _p2RoundWins }
            });

            int winsNeeded = (_maxRounds / 2) + 1;
            bool gameOver = _p1RoundWins >= winsNeeded
                || _p2RoundWins >= winsNeeded
                || _currentRound >= _maxRounds;

            if (gameOver)
            {
                EndGame();
            }
            else
            {
                _currentRound++;
                AdvanceLevel();
                StartCountdown();
            }
        }

        private void AdvanceLevel()
        {
            _currentLevelNumber++;
            LevelSO level = GetCurrentLevel();
            int levelIndex = _currentLevelNumber - 1;

            _arena1.SetLevelConfig(level, levelIndex);
            if (_arena2 != null)
                _arena2.SetLevelConfig(level, levelIndex);
        }

        private void EndGame()
        {
            string p1Name = _player1.CharacterData?.displayName ?? "Player 1";
            string p2Name = _player2?.CharacterData?.displayName ?? "Player 2";

            int winner = 0;
            if (_isSingleDog || _p1RoundWins > _p2RoundWins)
                winner = 1;
            else if (_p2RoundWins > _p1RoundWins)
                winner = 2;

            int finalP1 = _player1.Score;
            int finalP2 = _player2 != null ? _player2.Score : 0;

            EventBus.Emit(GameEvent.GameOver, new Dictionary<string, object>
            {
                { "winner", winner },
                { "p1Score", finalP1 },
                { "p2Score", finalP2 }
            });

            GameManager.Instance.StateMachine.ChangeState(GameState.GameOver, new Dictionary<string, object>
            {
                { "mode", _mode },
                { "winner", winner },
                { "p1_score", finalP1 },
                { "p2_score", finalP2 },
                { "p1_rounds", _p1RoundWins },
                { "p2_rounds", _p2RoundWins },
                { "vs_ai", _vsAI },
                { "p1_name", p1Name },
                { "p2_name", p2Name }
            });
        }

        // --- Helpers ---

        private void ResetPlayerForRound(GameObject go, PlayerController pc)
        {
            pc.ResetForNewRound();
            pc.enabled = true;
            go.GetComponent<CharacterAnimator>()?.ResetAnimation();
            go.GetComponent<SnackAttack.Effects.PlayerVFXController>()?.ResetForNewRound();
            go.GetComponent<AIController>()?.ResetForNewRound();
        }

        private LevelSO GetCurrentLevel()
        {
            var db = GameManager.Instance.LevelDatabase;
            var level = db.GetByNumber(_currentLevelNumber);
            if (level == null && db.levels.Count > 0)
            {
                int index = (_currentLevelNumber - 1) % db.levels.Count;
                level = db.levels[index];
            }
            return level;
        }
    }
}
