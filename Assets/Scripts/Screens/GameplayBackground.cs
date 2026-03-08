using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;

namespace SnackAttack.Screens
{
    public class GameplayBackground : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private Image _background;

        [Header("Logo & Clouds")]
        [SerializeField] private Image _logo;
        [SerializeField] private Image _cloud1;
        [SerializeField] private Image _cloud2;

        [Header("Battlefields")]
        [SerializeField] private Image _battleField1;
        [SerializeField] private Image _battleField2;

        [Header("Chat Placeholder")]
        [SerializeField] private RectTransform _chatPlaceholder;

        private CanvasGroup _canvasGroup;

        // Cloud animation
        private RectTransform _cloud1Rect;
        private RectTransform _cloud2Rect;
        private float _cloud1X;
        private float _cloud2X;
        private const float CloudSpeed = 8f; // px/s, gentle drift

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_cloud1 != null)
                _cloud1Rect = _cloud1.GetComponent<RectTransform>();
            if (_cloud2 != null)
                _cloud2Rect = _cloud2.GetComponent<RectTransform>();

            SetVisible(false);
        }

        public void Show(LevelSO level)
        {
            SetVisible(true);

            // Swap battlefield sprites if level provides them
            if (level != null && level.battlefieldSprite != null)
            {
                if (_battleField1 != null)
                    _battleField1.sprite = level.battlefieldSprite;
                if (_battleField2 != null)
                    _battleField2.sprite = level.battlefieldSprite;
            }

            // Initialize cloud positions
            if (_cloud1Rect != null)
                _cloud1X = _cloud1Rect.anchoredPosition.x;
            if (_cloud2Rect != null)
                _cloud2X = _cloud2Rect.anchoredPosition.x;
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void Update()
        {
            if (_canvasGroup.alpha < 1f)
                return;

            // Gentle cloud drift animation (matching PyGame's cloud movement)
            float dt = Time.deltaTime;

            if (_cloud1Rect != null)
            {
                _cloud1X += CloudSpeed * dt;
                // Wrap around when cloud drifts off right side
                float cloudWidth = _cloud1Rect.sizeDelta.x;
                if (_cloud1X > 1200f + cloudWidth)
                    _cloud1X = -cloudWidth;
                var pos = _cloud1Rect.anchoredPosition;
                pos.x = _cloud1X;
                _cloud1Rect.anchoredPosition = pos;
            }

            if (_cloud2Rect != null)
            {
                _cloud2X -= CloudSpeed * 0.7f * dt; // Cloud 2 drifts opposite, slower
                float cloudWidth = _cloud2Rect.sizeDelta.x;
                if (_cloud2X < -cloudWidth)
                    _cloud2X = 1200f + cloudWidth;
                var pos = _cloud2Rect.anchoredPosition;
                pos.x = _cloud2X;
                _cloud2Rect.anchoredPosition = pos;
            }
        }

        public void SetSinglePlayer(bool isSingle)
        {
            if (_battleField2 != null)
                _battleField2.gameObject.SetActive(!isSingle);
        }

        private void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
            gameObject.SetActive(visible);
        }
    }
}
