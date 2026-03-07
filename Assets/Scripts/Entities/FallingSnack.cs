using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Entities
{
    /// <summary>
    /// A snack that falls from the top of the arena, can be collected or despawns.
    /// Mirrors PyGame FallingSnack from gameplay.py.
    /// </summary>
    public class FallingSnack : MonoBehaviour
    {
        // Config (set via Initialize)
        private SnackSO _snackData;
        private float _fallSpeed;
        private float _groundY;
        private float _scale = 1f;

        // State
        private bool _active = true;
        private bool _collected;
        private float _rotationSpeed;

        // Components
        private SpriteRenderer _spriteRenderer;
        private CircleCollider2D _collider;

        // Public accessors
        public SnackSO SnackData => _snackData;
        public bool IsActive => _active;
        public bool WasCollected => _collected;

        public void Initialize(SnackSO snackData, float fallSpeed, float groundY, float scale = 1f)
        {
            _snackData = snackData;
            _fallSpeed = fallSpeed;
            _groundY = groundY;
            _scale = scale;

            // Random rotation (PyGame: 30-60°/s, random direction)
            _rotationSpeed = Random.Range(30f, 60f) * (Random.value > 0.5f ? 1f : -1f);

            // Setup visuals
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            _spriteRenderer.sprite = snackData.sprite;
            _spriteRenderer.sortingOrder = 5;

            // Scale for voted snacks
            if (scale != 1f)
                transform.localScale = Vector3.one * scale;

            // Setup collider
            _collider = GetComponent<CircleCollider2D>();
            if (_collider == null)
                _collider = gameObject.AddComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            // Collider radius based on sprite bounds
            if (snackData.sprite != null)
            {
                float spriteWidth = snackData.sprite.bounds.size.x;
                _collider.radius = spriteWidth * 0.4f;
            }
        }

        private void Update()
        {
            if (!_active) return;

            // Fall downward (Unity Y-down means negative Y)
            Vector3 pos = transform.position;
            pos.y -= _fallSpeed * Time.deltaTime;
            transform.position = pos;

            // Rotate
            transform.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);

            // Remove if fallen past ground level
            if (pos.y < _groundY)
            {
                Despawn();
            }
        }

        /// <summary>
        /// Collect this snack. Returns the snack data for scoring/effects.
        /// </summary>
        public SnackSO Collect()
        {
            if (!_active) return null;

            _active = false;
            _collected = true;

            EventBus.Emit(GameEvent.SnackCollected, new Dictionary<string, object>
            {
                { "snackId", _snackData.id },
                { "pointValue", _snackData.pointValue },
                { "position", transform.position }
            });

            Destroy(gameObject);
            return _snackData;
        }

        private void Despawn()
        {
            if (!_active) return;

            _active = false;

            EventBus.Emit(GameEvent.SnackDespawned, new Dictionary<string, object>
            {
                { "snackId", _snackData.id },
                { "position", transform.position }
            });

            Destroy(gameObject);
        }
    }
}
