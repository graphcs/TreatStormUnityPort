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
        private const float CrowdChaosElapsedThreshold = 35f;
        private const float CrowdChaosCountdownDuration = 5f;
        private float _crowdChaosCountdownTimer;
        private bool _crowdChaosCountdownActive;

        // Scene objects (created dynamically)
        private GameObject _gameplayRoot;
        private Arena _arena1;
        private Arena _arena2;
        private GameObject _player1Go;
        private GameObject _player2Go;
        private PlayerController _player1;
        private PlayerController _player2;
        private Transform _leashAnchor1;
        private Transform _leashAnchor2;

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

        private void Start()
        {
            GameManager.Instance.StateMachine.RegisterScreen(GameState.Gameplay, this);
        }

        public void OnEnter(Dictionary<string, object> data)
        {
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
            _maxRounds = GameManager.Instance.GameSettings.roundsPerGame;

            _gameplayRoot = new GameObject("GameplayRoot");

            SetupArenas(p1Char, p2Char);

            if (_background != null)
                _background.Show(GetCurrentLevel());

            if (_hud != null)
            {
                _hud.Initialize(this);
                _hud.Show();
            }

            EventBus.Emit(GameEvent.GameStart);
            EventBus.Emit(GameEvent.PlayMusic, new Dictionary<string, object>
            {
                { "track", "Gameplay" }
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
            var settings = GameManager.Instance.GameSettings;
            float arenaW = settings.arenaWidth / 100f;
            float arenaH = settings.arenaHeight / 100f;
            float gap = settings.splitScreenGap / 100f;

            if (_isSingleDog)
            {
                var bounds1 = new Rect(-arenaW / 2f, -arenaH / 2f, arenaW, arenaH);
                CreateArena(1, bounds1);
                CreatePlayer(1, p1Char, bounds1, _arena1, _leashAnchor1, true, false);
            }
            else
            {
                float totalSpan = arenaW * 2f + gap;
                float leftX = -totalSpan / 2f;
                float rightX = leftX + arenaW + gap;
                var bounds1 = new Rect(leftX, -arenaH / 2f, arenaW, arenaH);
                var bounds2 = new Rect(rightX, -arenaH / 2f, arenaW, arenaH);

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
            go.transform.SetParent(_gameplayRoot.transform);

            var arena = go.AddComponent<Arena>();
            arena.Initialize(bounds, level, GameManager.Instance.SnackDatabase);
            arena.SetSpawningEnabled(false);

            var anchor = new GameObject($"LeashAnchor_{index}");
            anchor.transform.SetParent(go.transform);
            anchor.transform.position = new Vector3(bounds.center.x, bounds.yMin, 0f);

            if (index == 1)
            {
                _arena1 = arena;
                _leashAnchor1 = anchor.transform;
            }
            else
            {
                _arena2 = arena;
                _leashAnchor2 = anchor.transform;
            }
        }

        private void CreatePlayer(int playerNum, CharacterSO charSO, Rect bounds,
            Arena arena, Transform leashAnchor, bool isSinglePlayer, bool isAI)
        {
            var go = new GameObject($"Player_{playerNum}");
            go.transform.SetParent(_gameplayRoot.transform);

            // 1. SpriteRenderer (needed by CharacterAnimator.Awake)
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            // 2. BoxCollider2D (needed by PlayerController.Awake, SnackCollector trigger)
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            if (charSO != null)
                collider.size = new Vector2(charSO.hitboxSize.x / 100f, charSO.hitboxSize.y / 100f);

            // 3. PlayerController
            var pc = go.AddComponent<PlayerController>();
            pc.Configure(charSO, playerNum, horizontal: true);
            pc.InitializeArena(bounds);

            // 4. CharacterAnimator (reads PlayerController in Awake)
            go.AddComponent<CharacterAnimator>();

            // 5. SnackCollector (RequireComponent adds Rigidbody2D)
            go.AddComponent<SnackCollector>();

            // 6. LeashRenderer
            var leash = go.AddComponent<LeashRenderer>();
            leash.SetAnchorPoint(leashAnchor);

            // 7. Input handling
            var input = go.AddComponent<PlayerInputHandler>();
            bool useSinglePlayerInput = isSinglePlayer || (_vsAI && playerNum == 1);
            input.Configure(playerNum - 1, useSinglePlayerInput);

            // 8. AI (if applicable)
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
            _countdownValue = 3;
            _countdownTimer = 1.0f;


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
                    _countdownTimer = 1.0f;
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

            // Reset and enable players
            ResetPlayerForRound(_player1Go, _player1);
            if (_player2 != null)
                ResetPlayerForRound(_player2Go, _player2);

            // Reset and enable arenas
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

            // Crowd Chaos hook (Step 23 stub)
            float elapsed = _roundDuration - _roundTimer;
            if (!_crowdChaosTriggered && !_crowdChaosCountdownActive
                && elapsed >= CrowdChaosElapsedThreshold)
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
            _crowdChaosCountdownTimer = CrowdChaosCountdownDuration;
            // TODO Step 23: Show countdown overlay, determine vote type by round
        }

        private void ActivateCrowdChaos()
        {
            _crowdChaosCountdownActive = false;
            // TODO Step 23: Start voting window, apply results
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
