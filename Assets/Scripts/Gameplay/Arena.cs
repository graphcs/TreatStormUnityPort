using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Entities;
using SnackAttack.UI;

namespace SnackAttack.Gameplay
{
    /// <summary>
    /// Manages a single player's game arena: bounds, snack spawning, lightning effects.
    /// Mirrors PyGame Arena class from gameplay.py.
    /// </summary>
    public class Arena : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SnackDatabaseSO snackDatabase;

        // Arena bounds (canvas-space rect)
        private Rect _bounds;
        private Rect _spawnBounds;
        private bool _initialized;

        // Spawn settings (mirrors PyGame Arena defaults)
        private float _baseSpawnInterval;
        private float _spawnRateMultiplier = 1.0f;
        private int _maxSnacks;
        private float _fallSpeed;
        private SnackSO[] _snackPool;

        // Ground level where snacks despawn
        private float _groundY;

        // Spawn timer
        private float _spawnTimer;

        // Cloud spawn position (center X, set by cloud animation)
        private float? _cloudSpawnX;

        // Lightning effect
        private bool _lightningActive;
        private float _lightningTimer;
        private SnackSO _pendingSnack;
        private float _pendingSnackX;
        private float _pendingSnackScale = 1f;
        private bool _thunderPlayedThisRound;

        // Lightning visual data (UILineDrawer)
        private UILineDrawer _lightningDrawer;
        private Color _lightningColor;
        private static readonly Color[] LightningColors =
        {
            new Color(1f, 1f, 0.39f),       // Yellow
            new Color(0.39f, 0.78f, 1f),     // Cyan
            new Color(1f, 0.39f, 1f),        // Magenta
            new Color(1f, 0.59f, 0.2f),      // Orange
            new Color(0.59f, 1f, 0.59f),     // Light green
            new Color(1f, 0.39f, 0.39f),     // Light red
        };

        // Spawning gate
        private bool _spawningEnabled;

        // Voted food spawning
        private bool _votedFoodActive;
        private float _votedFoodTimer;
        private SnackSO _votedFoodConfig;
        private float _votedFoodSpawnTimer;

        // Active snacks tracking
        private readonly List<FallingSnack> _activeSnacks = new();

        // Snack parent (under GameplayRoot for shared coordinate space)
        private RectTransform _snackParent;

        // Entity root (GameplayRoot) for lightning parenting
        private RectTransform _entityRoot;

        // Cached settings
        private GameSettingsSO _gameSettings;
        private SnackDatabaseSO _snackDb;

        // Public accessors
        public void SetSpawningEnabled(bool enabled)
        {
            _spawningEnabled = enabled;
        }

        public Rect Bounds => _bounds;
        public IReadOnlyList<FallingSnack> ActiveSnacks => _activeSnacks;
        public float GroundY => _groundY;
        public float FallSpeed => _fallSpeed;

        /// <summary>
        /// Initialize the arena with bounds and level config.
        /// entityRoot is the shared GameplayRoot for snack/lightning parenting.
        /// </summary>
        public void Initialize(Rect bounds, LevelSO level, SnackDatabaseSO database, RectTransform entityRoot,
            Rect spawnBounds)
        {
            _bounds = bounds;
            _spawnBounds = spawnBounds;
            snackDatabase = database;
            _snackDb = database;
            _baseSpawnInterval = database.baseInterval;
            _maxSnacks = database.maxActive;
            _snackPool = level.snackPool;
            _spawnRateMultiplier = level.spawnRateMultiplier;
            _entityRoot = entityRoot;

            _gameSettings = GameManager.Instance?.GameSettings;

            // Fall speed from SO
            _fallSpeed = _snackDb != null ? _snackDb.baseFallSpeed : 180f;

            // Ground Y: bounds.yMin + groundYOffset
            float groundYOffset = _gameSettings != null ? _gameSettings.groundYOffset : 16f;
            _groundY = bounds.yMin + groundYOffset;

            _initialized = true;
            _spawnTimer = 0f;

            // Create snack parent under entity root for shared coordinate space
            var snackGo = new GameObject("Snacks");
            _snackParent = snackGo.AddComponent<RectTransform>();
            _snackParent.SetParent(entityRoot, false);
            _snackParent.anchorMin = new Vector2(0.5f, 0.5f);
            _snackParent.anchorMax = new Vector2(0.5f, 0.5f);
            _snackParent.anchoredPosition = Vector2.zero;
            _snackParent.sizeDelta = Vector2.zero;

            // Setup lightning drawer
            SetupLightningDrawer();
        }

        public void SetLevelConfig(LevelSO level, int levelIndex)
        {
            _snackPool = level.snackPool;
            _spawnRateMultiplier = level.spawnRateMultiplier;

            float baseFall = _snackDb != null ? _snackDb.baseFallSpeed : 180f;
            float perLevel = _snackDb != null ? _snackDb.fallSpeedPerLevel : 30f;
            float intervalDec = _snackDb != null ? _snackDb.spawnIntervalDecrement : 0.15f;

            _fallSpeed = baseFall + levelIndex * perLevel;
            _baseSpawnInterval = Mathf.Max(snackDatabase.minInterval, snackDatabase.baseInterval - levelIndex * intervalDec);
        }

        public void SetCloudSpawnX(float x)
        {
            _cloudSpawnX = x;
        }

        public void StartVotedFoodPhase(SnackSO snack, float scale = 1.5f)
        {
            float votedDuration = _gameSettings != null ? _gameSettings.votedFoodDuration : 5f;

            _votedFoodActive = true;
            _votedFoodTimer = votedDuration;
            _votedFoodConfig = snack;
            _pendingSnackScale = scale;
            _votedFoodSpawnTimer = 0f;

            float x = _bounds.center.x;
            TriggerLightning(x);
            _pendingSnack = snack;
            _pendingSnackX = x;
        }

        public void ResetForNewRound()
        {
            for (int i = _activeSnacks.Count - 1; i >= 0; i--)
            {
                if (_activeSnacks[i] != null)
                    Destroy(_activeSnacks[i].gameObject);
            }
            _activeSnacks.Clear();

            _spawnTimer = 0f;
            _lightningActive = false;
            _pendingSnack = null;
            _thunderPlayedThisRound = false;
            _votedFoodActive = false;
            _votedFoodConfig = null;

            if (_lightningDrawer != null)
                _lightningDrawer.Clear();
        }

        private void Update()
        {
            if (!_initialized) return;

            float dt = Time.deltaTime;

            UpdateLightning(dt);
            if (_spawningEnabled)
                UpdateSpawning(dt);
            CleanupDeadSnacks();
        }

        // --- Spawning ---

        private void UpdateSpawning(float dt)
        {
            _spawnTimer -= dt;

            float votedInterval = _gameSettings != null ? _gameSettings.votedFoodSpawnInterval : 0.3f;

            if (_votedFoodActive)
            {
                _votedFoodTimer -= dt;
                if (_votedFoodTimer <= 0f)
                {
                    _votedFoodActive = false;
                    _votedFoodConfig = null;
                    _spawnTimer = 0f;
                    return;
                }

                _votedFoodSpawnTimer -= dt;
                if (_votedFoodSpawnTimer <= 0f && _votedFoodConfig != null)
                {
                    float x = Random.Range(_spawnBounds.xMin + 50f, _spawnBounds.xMax - 50f);
                    SpawnSnackImmediate(_votedFoodConfig, x, _pendingSnackScale);
                    _votedFoodSpawnTimer = votedInterval + Random.Range(-0.1f, 0.1f);
                }
            }
            else if (_spawnTimer <= 0f)
            {
                TrySpawnSnack();
                float interval = _baseSpawnInterval / _spawnRateMultiplier;
                _spawnTimer = interval + Random.Range(-0.3f, 0.3f);
            }
        }

        private void TrySpawnSnack()
        {
            if (_activeSnacks.Count >= _maxSnacks) return;
            if (_lightningActive) return;
            if (_snackPool == null || _snackPool.Length == 0) return;

            SnackSO selected = snackDatabase.GetWeightedRandomFromPool(_snackPool);
            if (selected == null) return;

            float padding = _gameSettings != null ? _gameSettings.snackSpawnPadding : 20f;
            float variance = _gameSettings != null ? _gameSettings.snackSpawnVariance : 120f;
            float x;
            if (_cloudSpawnX.HasValue)
            {
                x = _cloudSpawnX.Value + Random.Range(-variance, variance);
                x = Mathf.Clamp(x, _spawnBounds.xMin + padding, _spawnBounds.xMax - padding);
            }
            else
            {
                x = Random.Range(_spawnBounds.xMin + padding, _spawnBounds.xMax - padding);
            }

            TriggerLightning(x);
            _pendingSnack = selected;
            _pendingSnackX = x;
            _pendingSnackScale = 1f;
        }

        private void SpawnSnackImmediate(SnackSO snackData, float x, float scale = 1f)
        {
            float spawnYOffset = _gameSettings != null ? _gameSettings.snackSpawnYOffset : 60f;
            float spawnY = _bounds.yMax - spawnYOffset;

            var go = new GameObject($"Snack_{snackData.id}");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(_snackParent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, spawnY);

            var fallingSnack = go.AddComponent<FallingSnack>();
            fallingSnack.Initialize(snackData, _fallSpeed, _groundY, scale);

            _activeSnacks.Add(fallingSnack);

            EventBus.Emit(GameEvent.SnackSpawned, new Dictionary<string, object>
            {
                { "snackId", snackData.id },
                { "position", (Vector3)rect.anchoredPosition }
            });
        }

        private void SpawnPendingSnack()
        {
            if (_pendingSnack == null) return;

            SpawnSnackImmediate(_pendingSnack, _pendingSnackX, _pendingSnackScale);
            _pendingSnack = null;
            _pendingSnackScale = 1f;
        }

        private void CleanupDeadSnacks()
        {
            for (int i = _activeSnacks.Count - 1; i >= 0; i--)
            {
                if (_activeSnacks[i] == null || !_activeSnacks[i].IsActive)
                    _activeSnacks.RemoveAt(i);
            }
        }

        // --- Lightning ---

        private void SetupLightningDrawer()
        {
            float lineWidth = _gameSettings != null ? _gameSettings.lightningLineWidth : 5f;

            var lightningGo = new GameObject("Lightning");
            var lightningRect = lightningGo.AddComponent<RectTransform>();
            lightningRect.SetParent(_entityRoot, false);
            lightningRect.anchorMin = new Vector2(0.5f, 0.5f);
            lightningRect.anchorMax = new Vector2(0.5f, 0.5f);
            lightningRect.anchoredPosition = Vector2.zero;
            lightningRect.sizeDelta = Vector2.zero;

            _lightningDrawer = lightningGo.AddComponent<UILineDrawer>();
            _lightningDrawer.SetWidth(lineWidth, 2f);
            _lightningDrawer.raycastTarget = false;
        }

        private void TriggerLightning(float targetX)
        {
            float duration = _gameSettings != null ? _gameSettings.lightningDuration : 0.08f;
            int numSegments = _gameSettings != null ? _gameSettings.lightningSegments : 6;
            float jagRange = _gameSettings != null ? _gameSettings.lightningJagRange : 30f;

            _lightningActive = true;
            _lightningTimer = duration;
            _lightningColor = LightningColors[Random.Range(0, LightningColors.Length)];

            if (!_thunderPlayedThisRound)
            {
                EventBus.Emit(GameEvent.PlaySound, new Dictionary<string, object>
                {
                    { "sound", "thunder" }
                });
                _thunderPlayedThisRound = true;
            }

            // Generate jagged bolt points
            float startX = _cloudSpawnX ?? targetX;
            float startY = _bounds.yMax - 50f;   // Just below cloud
            float endX = targetX;
            float endY = _bounds.yMax - 130f;     // Where food appears

            var points = new Vector2[numSegments + 1];
            points[0] = new Vector2(startX, startY);

            for (int i = 1; i < numSegments; i++)
            {
                float progress = (float)i / numSegments;
                float midX = Mathf.Lerp(startX, endX, progress) + Random.Range(-jagRange, jagRange);
                float midY = Mathf.Lerp(startY, endY, progress);
                points[i] = new Vector2(midX, midY);
            }
            points[numSegments] = new Vector2(endX, endY);

            _lightningDrawer.SetPoints(points);
            _lightningDrawer.SetLineColor(_lightningColor);
        }

        private void UpdateLightning(float dt)
        {
            if (!_lightningActive) return;

            _lightningTimer -= dt;

            float colorChangeProb = _gameSettings != null ? _gameSettings.lightningColorChangeProb : 0.3f;
            if (Random.value < colorChangeProb)
            {
                _lightningColor = LightningColors[Random.Range(0, LightningColors.Length)];
                _lightningDrawer.SetLineColor(_lightningColor);
            }

            if (_lightningTimer <= 0f)
            {
                _lightningActive = false;
                _lightningDrawer.Clear();
                SpawnPendingSnack();
            }
        }
    }
}
