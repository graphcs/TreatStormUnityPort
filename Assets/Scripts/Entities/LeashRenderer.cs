using UnityEngine;
using SnackAttack.Core;
using SnackAttack.Effects;
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
        private UILineDrawer _highlightLine;
        private UICircleDrawer _anchorRing;
        private UICircleDrawer _collarVisual;
        private Vector2[] _points;
        private Vector2[] _shadowPoints;
        private Vector2[] _highlightPoints;

        // Cached colors
        private Color _normalRopeColor;
        private Color _extendedRopeColor;
        private Color _yankedRopeColor;
        private Color _normalHighlightColor;
        private Color _extendedHighlightColor;
        private Color _yankedHighlightColor;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _points = new Vector2[segments + 1];
            _shadowPoints = new Vector2[segments + 1];
            _highlightPoints = new Vector2[segments + 1];

            // Cache colors from SO
            var colors = GameManager.Instance?.UIColors;
            _normalRopeColor = colors != null ? colors.normalRopeColor : (Color)new Color32(139, 90, 43, 255);
            _extendedRopeColor = colors != null ? colors.extendedRopeColor : (Color)new Color32(80, 200, 80, 255);
            _yankedRopeColor = colors != null ? colors.yankedRopeColor : (Color)new Color32(200, 80, 80, 255);
            Color shadowColor = colors != null ? colors.shadowColor : (Color)new Color32(50, 30, 20, 255);
            _normalHighlightColor = colors != null ? colors.normalHighlightColor : (Color)new Color32(101, 67, 33, 255);
            _extendedHighlightColor = colors != null ? colors.extendedHighlightColor : (Color)new Color32(60, 180, 60, 255);
            _yankedHighlightColor = colors != null ? colors.yankedHighlightColor : (Color)new Color32(180, 60, 60, 255);

            // 1. Anchor ring (renders behind everything)
            var anchorGo = new GameObject("AnchorRing");
            var anchorRect = anchorGo.AddComponent<RectTransform>();
            anchorRect.SetParent(transform, false);
            anchorRect.anchorMin = Vector2.zero;
            anchorRect.anchorMax = Vector2.one;
            anchorRect.offsetMin = Vector2.zero;
            anchorRect.offsetMax = Vector2.zero;
            _anchorRing = anchorGo.AddComponent<UICircleDrawer>();
            _anchorRing.raycastTarget = false;

            // 2. Shadow UILineDrawer child
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

            // 3. Main UILineDrawer child
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

            // 4. Highlight UILineDrawer child
            var highlightGo = new GameObject("LeashHighlight");
            var highlightRect = highlightGo.AddComponent<RectTransform>();
            highlightRect.SetParent(transform, false);
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            _highlightLine = highlightGo.AddComponent<UILineDrawer>();
            _highlightLine.SetWidth(2f, 2f);
            _highlightLine.SetLineColor(_normalHighlightColor);
            _highlightLine.raycastTarget = false;

            // 5. Collar visual (renders on top)
            var collarGo = new GameObject("CollarVisual");
            var collarRect = collarGo.AddComponent<RectTransform>();
            collarRect.SetParent(transform, false);
            collarRect.anchorMin = Vector2.zero;
            collarRect.anchorMax = Vector2.one;
            collarRect.offsetMin = Vector2.zero;
            collarRect.offsetMax = Vector2.zero;
            _collarVisual = collarGo.AddComponent<UICircleDrawer>();
            _collarVisual.raycastTarget = false;
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
            Color highlightColor = state switch
            {
                LeashState.Extended => _extendedHighlightColor,
                LeashState.Yanked => _yankedHighlightColor,
                _ => _normalHighlightColor
            };
            _mainLine.SetLineColor(ropeColor);
            _highlightLine.SetLineColor(highlightColor);

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
                _highlightPoints[i] = new Vector2(x - playerPos.x, y - playerPos.y + 1f);
            }

            _mainLine.SetPoints(_points);
            _shadowLine.SetPoints(_shadowPoints);
            _highlightLine.SetPoints(_highlightPoints);

            // Anchor ring: 3 concentric filled circles at anchor local position
            Vector2 anchorLocal = new Vector2(anchor.x - playerPos.x, anchor.y - playerPos.y);
            _anchorRing.Clear();
            _anchorRing.AddFilledCircle(anchorLocal, 8f, new Color32(100, 100, 100, 255));
            _anchorRing.AddFilledCircle(anchorLocal, 6f, new Color32(150, 150, 150, 255));
            _anchorRing.AddFilledCircle(anchorLocal, 4f, new Color32(80, 80, 80, 255));
            _anchorRing.Rebuild();

            // Collar visual: colored outer + light gray inner at collar local position
            Vector2 collarLocal = new Vector2(collar.x - playerPos.x, collar.y - playerPos.y);
            _collarVisual.Clear();
            _collarVisual.AddFilledCircle(collarLocal, 6f, ropeColor);
            _collarVisual.AddFilledCircle(collarLocal, 4f, new Color32(200, 200, 200, 255));
            _collarVisual.Rebuild();
        }

        public void SetAnchorPoint(RectTransform anchor)
        {
            anchorPoint = anchor;
        }

        public void SetVisible(bool visible)
        {
            _mainLine.enabled = visible;
            _shadowLine.enabled = visible;
            _highlightLine.enabled = visible;
            _anchorRing.enabled = visible;
            _collarVisual.enabled = visible;
        }
    }
}
