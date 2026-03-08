using UnityEngine;
using SnackAttack.Core;
using SnackAttack.UI;

namespace SnackAttack.Entities
{
    [RequireComponent(typeof(PlayerController))]
    public class LeashRenderer : MonoBehaviour
    {
        [Header("Anchor")]
        [SerializeField] private RectTransform anchorPoint;

        [Header("Rope Settings")]
        [SerializeField] private int segments = 12;
        [SerializeField] private float maxSag = 30f;
        [SerializeField] private float sagFactor = 0.15f;

        private PlayerController _player;
        private UILineDrawer _mainLine;
        private UILineDrawer _shadowLine;
        private Vector2[] _points;
        private Vector2[] _shadowPoints;

        // Cached colors
        private Color _normalRopeColor;
        private Color _extendedRopeColor;
        private Color _yankedRopeColor;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _points = new Vector2[segments + 1];
            _shadowPoints = new Vector2[segments + 1];

            // Cache colors from SO
            var colors = GameManager.Instance?.UIColors;
            _normalRopeColor = colors != null ? colors.normalRopeColor : new Color(0.396f, 0.263f, 0.129f);
            _extendedRopeColor = colors != null ? colors.extendedRopeColor : new Color(0.235f, 0.706f, 0.235f);
            _yankedRopeColor = colors != null ? colors.yankedRopeColor : new Color(0.706f, 0.235f, 0.235f);
            Color shadowColor = colors != null ? colors.shadowColor : new Color(0.196f, 0.118f, 0.078f);

            // Create shadow UILineDrawer child
            var shadowGo = new GameObject("LeashShadow");
            var shadowRect = shadowGo.AddComponent<RectTransform>();
            shadowRect.SetParent(transform, false);
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.offsetMin = Vector2.zero;
            shadowRect.offsetMax = Vector2.zero;
            _shadowLine = shadowGo.AddComponent<UILineDrawer>();
            _shadowLine.SetWidth(6f, 6f);
            _shadowLine.SetLineColor(shadowColor);
            _shadowLine.raycastTarget = false;

            // Create main UILineDrawer child
            var mainGo = new GameObject("LeashMain");
            var mainRect = mainGo.AddComponent<RectTransform>();
            mainRect.SetParent(transform, false);
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;
            _mainLine = mainGo.AddComponent<UILineDrawer>();
            _mainLine.SetWidth(4f, 4f);
            _mainLine.SetLineColor(_normalRopeColor);
            _mainLine.raycastTarget = false;
        }

        private void LateUpdate()
        {
            if (anchorPoint == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            // Anchor position in GameplayRoot canvas coords
            Vector2 anchor = anchorPoint.anchoredPosition;
            // Collar position in GameplayRoot canvas coords
            Vector2 collarOffset = _player.CollarOffset;
            Vector2 playerPos = _player.RectTransform.anchoredPosition;
            Vector2 collar = playerPos + collarOffset;

            // Update colors based on leash state
            LeashState state = _player.GetLeashState();
            Color ropeColor = state switch
            {
                LeashState.Extended => _extendedRopeColor,
                LeashState.Yanked => _yankedRopeColor,
                _ => _normalRopeColor
            };
            _mainLine.SetLineColor(ropeColor);

            // Compute parabolic curve in canvas coords
            float distance = Vector2.Distance(anchor, collar);
            float sag = Mathf.Min(maxSag, distance * sagFactor);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float x = Mathf.Lerp(anchor.x, collar.x, t);
                float y = Mathf.Lerp(anchor.y, collar.y, t);
                y -= sag * 4f * t * (1f - t);

                // Convert to player-local space by subtracting player position
                _points[i] = new Vector2(x - playerPos.x, y - playerPos.y);
                _shadowPoints[i] = new Vector2(x - playerPos.x, y - playerPos.y - 1f);
            }

            _mainLine.SetPoints(_points);
            _shadowLine.SetPoints(_shadowPoints);
        }

        public void SetAnchorPoint(RectTransform anchor)
        {
            anchorPoint = anchor;
        }

        public void SetVisible(bool visible)
        {
            _mainLine.enabled = visible;
            _shadowLine.enabled = visible;
        }
    }
}
