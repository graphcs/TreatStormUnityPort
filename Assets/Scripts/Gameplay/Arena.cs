using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Entities;

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

        // Arena bounds (world-space rect)
        private Rect _bounds;
        private bool _initialized;

        // Spawn settings (mirrors PyGame Arena defaults)
        private float _baseSpawnInterval;
        private float _spawnRateMultiplier = 1.0f;
        private int _maxSnacks;
        private float _fallSpeed = 180f; // PyGame: 180 px/s → 1.8 Unity units/s
        private SnackSO[] _snackPool;

        // Ground level where snacks despawn (PyGame: bounds.bottom - 16)
        private float _groundY;

        // Spawn timer
        private float _spawnTimer;

        // Cloud spawn position (center X, set by cloud animation)
        private float? _cloudSpawnX;

        // Lightning effect
        private bool _lightningActive;
        private float _lightningTimer;
        private const float LightningDuration = 0.08f;
        private SnackSO _pendingSnack;
        private float _pendingSnackX;
        private float _pendingSnackScale = 1f;
        private bool _thunderPlayedThisRound;

        // Lightning visual data (for LineRenderer)
        private LineRenderer _lightningRenderer;
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
        private float _votedFoodDuration = 5f;
        private SnackSO _votedFoodConfig;
        private float _votedFoodSpawnInterval = 0.3f;
        private float _votedFoodSpawnTimer;

        // Active snacks tracking
        private readonly List<FallingSnack> _activeSnacks = new();

        // Snack prefab parent
        private Transform _snackParent;

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
        /// </summary>
        public void Initialize(Rect bounds, LevelSO level, SnackDatabaseSO database)
        {
            _bounds = bounds;
            snackDatabase = database;
            _baseSpawnInterval = database.baseInterval;
            _maxSnacks = database.maxActive;
            _snackPool = level.snackPool;
            _spawnRateMultiplier = level.spawnRateMultiplier;

            // Convert PyGame pixel coordinates to Unity units (÷100)
            // Ground Y: bounds.bottom - 16px → bounds.yMin + 0.16 (Unity Y-up)
            _groundY = bounds.yMin + 0.16f;

            _initialized = true;
            _spawnTimer = 0f;

            // Create snack parent for organization
            _snackParent = new GameObject("Snacks").transform;
            _snackParent.SetParent(transform);
            _snackParent.localPosition = Vector3.zero;

            // Setup lightning renderer
            SetupLightningRenderer();
        }

        /// <summary>
        /// Update level config (when advancing to next level mid-match).
        /// </summary>
        public void SetLevelConfig(LevelSO level, int levelIndex)
        {
            _snackPool = level.snackPool;
            _spawnRateMultiplier = level.spawnRateMultiplier;
            // PyGame: fall_speed = 180 + (level - 1) * 30
            _fallSpeed = 180f + levelIndex * 30f;
            // PyGame: base_interval = max(min_interval, base_interval - (level - 1) * 0.15)
            _baseSpawnInterval = Mathf.Max(snackDatabase.minInterval, snackDatabase.baseInterval - levelIndex * 0.15f);
        }

        /// <summary>
        /// Set cloud spawn X position (world space). Called by cloud animation system.
        /// </summary>
        public void SetCloudSpawnX(float x)
        {
            _cloudSpawnX = x;
        }

        /// <summary>
        /// Start a voted food spawning phase.
        /// </summary>
        public void StartVotedFoodPhase(SnackSO snack, float scale = 1.5f)
        {
            _votedFoodActive = true;
            _votedFoodTimer = _votedFoodDuration;
            _votedFoodConfig = snack;
            _pendingSnackScale = scale;
            _votedFoodSpawnTimer = 0f;

            // Trigger initial lightning
            float x = _bounds.center.x;
            TriggerLightning(x);
            _pendingSnack = snack;
            _pendingSnackX = x;
        }

        /// <summary>
        /// Clear all snacks and reset for a new round.
        /// </summary>
        public void ResetForNewRound()
        {
            // Destroy all active snacks
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

            if (_lightningRenderer != null)
                _lightningRenderer.positionCount = 0;
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

            if (_votedFoodActive)
            {
                _votedFoodTimer -= dt;
                if (_votedFoodTimer <= 0f)
                {
                    _votedFoodActive = false;
                    _votedFoodConfig = null;
                    _spawnTimer = 0f; // Trigger immediate normal spawn
                    return;
                }

                // Spawn voted food frequently
                _votedFoodSpawnTimer -= dt;
                if (_votedFoodSpawnTimer <= 0f && _votedFoodConfig != null)
                {
                    float x = Random.Range(_bounds.xMin + 0.5f, _bounds.xMax - 0.5f);
                    SpawnSnackImmediate(_votedFoodConfig, x, _pendingSnackScale);
                    _votedFoodSpawnTimer = _votedFoodSpawnInterval + Random.Range(-0.1f, 0.1f);
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

            // Weighted random selection from pool
            SnackSO selected = snackDatabase.GetWeightedRandomFromPool(_snackPool);
            if (selected == null) return;

            // X position — spawn from cloud with ±1.2 unit variance, or random
            float padding = 0.2f;
            float x;
            if (_cloudSpawnX.HasValue)
            {
                float variance = 1.2f; // PyGame: 120px → 1.2 units
                x = _cloudSpawnX.Value + Random.Range(-variance, variance);
                x = Mathf.Clamp(x, _bounds.xMin + padding, _bounds.xMax - padding);
            }
            else
            {
                x = Random.Range(_bounds.xMin + padding, _bounds.xMax - padding);
            }

            // Trigger lightning, queue the snack
            TriggerLightning(x);
            _pendingSnack = selected;
            _pendingSnackX = x;
            _pendingSnackScale = 1f;
        }

        private void SpawnSnackImmediate(SnackSO snackData, float x, float scale = 1f)
        {
            // Spawn Y at top of arena (PyGame: arena.top + 60 → bounds.yMax - 0.6)
            float spawnY = _bounds.yMax - 0.6f;

            var go = new GameObject($"Snack_{snackData.id}");
            go.transform.SetParent(_snackParent);
            go.transform.position = new Vector3(x, spawnY, 0f);

            var fallingSnack = go.AddComponent<FallingSnack>();
            // Convert fall speed: PyGame px/s → Unity units/s (÷100)
            fallingSnack.Initialize(snackData, _fallSpeed / 100f, _groundY, scale);

            _activeSnacks.Add(fallingSnack);

            EventBus.Emit(GameEvent.SnackSpawned, new Dictionary<string, object>
            {
                { "snackId", snackData.id },
                { "position", go.transform.position }
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

        private void SetupLightningRenderer()
        {
            var lightningGo = new GameObject("Lightning");
            lightningGo.transform.SetParent(transform);
            lightningGo.transform.localPosition = Vector3.zero;

            _lightningRenderer = lightningGo.AddComponent<LineRenderer>();
            _lightningRenderer.startWidth = 0.05f;
            _lightningRenderer.endWidth = 0.02f;
            _lightningRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lightningRenderer.sortingOrder = 10;
            _lightningRenderer.positionCount = 0;
        }

        private void TriggerLightning(float targetX)
        {
            _lightningActive = true;
            _lightningTimer = LightningDuration;
            _lightningColor = LightningColors[Random.Range(0, LightningColors.Length)];

            // Play thunder once per round
            if (!_thunderPlayedThisRound)
            {
                EventBus.Emit(GameEvent.PlaySound, new Dictionary<string, object>
                {
                    { "sound", "Thunder" }
                });
                _thunderPlayedThisRound = true;
            }

            // Generate jagged bolt points (PyGame: 6 segments from cloud to spawn point)
            float startX = _cloudSpawnX ?? targetX;
            float startY = _bounds.yMax - 0.5f; // Just below cloud
            float endX = targetX;
            float endY = _bounds.yMax - 1.3f; // Where food appears

            int numSegments = 6;
            var points = new Vector3[numSegments + 1];
            points[0] = new Vector3(startX, startY, 0f);

            for (int i = 1; i < numSegments; i++)
            {
                float progress = (float)i / numSegments;
                float midX = Mathf.Lerp(startX, endX, progress) + Random.Range(-0.3f, 0.3f);
                float midY = Mathf.Lerp(startY, endY, progress);
                points[i] = new Vector3(midX, midY, 0f);
            }
            points[numSegments] = new Vector3(endX, endY, 0f);

            _lightningRenderer.positionCount = points.Length;
            _lightningRenderer.SetPositions(points);
            _lightningRenderer.startColor = _lightningColor;
            _lightningRenderer.endColor = _lightningColor;
        }

        private void UpdateLightning(float dt)
        {
            if (!_lightningActive) return;

            _lightningTimer -= dt;

            // Randomly change color during flash (PyGame: 30% chance per frame)
            if (Random.value < 0.3f)
            {
                _lightningColor = LightningColors[Random.Range(0, LightningColors.Length)];
                _lightningRenderer.startColor = _lightningColor;
                _lightningRenderer.endColor = _lightningColor;
            }

            if (_lightningTimer <= 0f)
            {
                _lightningActive = false;
                _lightningRenderer.positionCount = 0;
                SpawnPendingSnack();
            }
        }

        // --- Gizmos ---

        private void OnDrawGizmosSelected()
        {
            if (!_initialized) return;

            // Draw arena bounds
            Gizmos.color = Color.green;
            var center = new Vector3(_bounds.center.x, _bounds.center.y, 0f);
            var size = new Vector3(_bounds.width, _bounds.height, 0f);
            Gizmos.DrawWireCube(center, size);

            // Draw ground line
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(_bounds.xMin, _groundY, 0f),
                new Vector3(_bounds.xMax, _groundY, 0f)
            );
        }
    }
}
