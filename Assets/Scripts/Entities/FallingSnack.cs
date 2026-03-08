using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;

namespace SnackAttack.Entities
{
    /// <summary>
    /// A snack that falls from the top of the arena as a canvas Image.
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

        // Cached SO values
        private float _baseSnackSize;
        private float _hitboxShrink;

        // Components
        private Image _image;
        private RectTransform _rectTransform;

        // Public accessors
        public SnackSO SnackData => _snackData;
        public bool IsActive => _active;
        public bool WasCollected => _collected;
        public RectTransform RectTransform => _rectTransform;

        public void Initialize(SnackSO snackData, float fallSpeed, float groundY, float scale = 1f)
        {
            _snackData = snackData;
            _fallSpeed = fallSpeed;
            _groundY = groundY;
            _scale = scale;

            // Cache SO values
            var snackDb = GameManager.Instance?.SnackDatabase;
            _baseSnackSize = snackDb != null ? snackDb.baseSnackSize : 72f;
            _hitboxShrink = snackDb != null ? snackDb.snackHitboxShrink : 10f;
            float rotMin = snackDb != null ? snackDb.snackRotationSpeedMin : 30f;
            float rotMax = snackDb != null ? snackDb.snackRotationSpeedMax : 60f;

            // Random rotation
            _rotationSpeed = Random.Range(rotMin, rotMax) * (Random.value > 0.5f ? 1f : -1f);

            // Cache RectTransform
            _rectTransform = GetComponent<RectTransform>();

            // Setup visuals as Image
            _image = gameObject.AddComponent<Image>();
            _image.sprite = snackData.sprite;
            _image.preserveAspect = true;
            _image.raycastTarget = false;

            // Size: base size * scale
            float size = _baseSnackSize * scale;
            _rectTransform.sizeDelta = new Vector2(size, size);
        }

        private void Update()
        {
            if (!_active) return;

            // Fall downward (canvas Y: more negative = lower)
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.y -= _fallSpeed * Time.deltaTime;
            _rectTransform.anchoredPosition = pos;

            // Rotate
            _rectTransform.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);

            // Remove if fallen past ground level
            if (pos.y < _groundY)
            {
                Despawn();
            }
        }

        /// <summary>
        /// Returns a collision rect shrunk by hitboxShrink per side.
        /// </summary>
        public Rect GetCollisionRect()
        {
            Vector2 pos = _rectTransform.anchoredPosition;
            float size = _baseSnackSize * _scale;
            float halfSize = size * 0.5f;
            float shrink = _hitboxShrink;
            return new Rect(
                pos.x - halfSize + shrink,
                pos.y - halfSize + shrink,
                size - shrink * 2f,
                size - shrink * 2f
            );
        }

        public SnackSO Collect()
        {
            if (!_active) return null;

            _active = false;
            _collected = true;

            EventBus.Emit(GameEvent.SnackCollected, new Dictionary<string, object>
            {
                { "snackId", _snackData.id },
                { "pointValue", _snackData.pointValue },
                { "position", (Vector3)_rectTransform.anchoredPosition }
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
                { "position", (Vector3)_rectTransform.anchoredPosition }
            });

            Destroy(gameObject);
        }
    }
}
