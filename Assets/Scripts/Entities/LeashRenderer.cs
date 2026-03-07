using UnityEngine;

namespace SnackAttack.Entities
{
    [RequireComponent(typeof(PlayerController))]
    public class LeashRenderer : MonoBehaviour
    {
        [Header("Anchor")]
        [SerializeField] private Transform anchorPoint;

        [Header("Rope Settings")]
        [SerializeField] private int segments = 12;
        [SerializeField] private float maxSag = 0.3f;
        [SerializeField] private float sagFactor = 0.15f;

        // State colors (PyGame values)
        private static readonly Color NormalColor = new(0.545f, 0.353f, 0.169f);
        private static readonly Color ExtendedColor = new(0.314f, 0.784f, 0.314f);
        private static readonly Color YankedColor = new(0.784f, 0.314f, 0.314f);

        // Rope colors (darker, for main line)
        private static readonly Color NormalRopeColor = new(0.396f, 0.263f, 0.129f);
        private static readonly Color ExtendedRopeColor = new(0.235f, 0.706f, 0.235f);
        private static readonly Color YankedRopeColor = new(0.706f, 0.235f, 0.235f);

        // Shadow color
        private static readonly Color ShadowColor = new(0.196f, 0.118f, 0.078f);

        private PlayerController _player;
        private LineRenderer _mainLine;
        private LineRenderer _shadowLine;
        private GameObject _mainLineObj;
        private GameObject _shadowLineObj;
        private Vector3[] _points;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _points = new Vector3[segments + 1];

            _shadowLineObj = new GameObject("LeashShadow");
            _shadowLineObj.transform.SetParent(transform);
            _shadowLine = _shadowLineObj.AddComponent<LineRenderer>();
            ConfigureLine(_shadowLine, 0.06f, ShadowColor, -1);

            _mainLineObj = new GameObject("LeashMain");
            _mainLineObj.transform.SetParent(transform);
            _mainLine = _mainLineObj.AddComponent<LineRenderer>();
            ConfigureLine(_mainLine, 0.04f, NormalRopeColor, 0);
        }

        private void ConfigureLine(LineRenderer line, float width, Color color, int orderOffset)
        {
            line.useWorldSpace = true;
            line.positionCount = segments + 1;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingLayerName = "Default";
            line.sortingOrder = -1 + orderOffset;
            line.numCapVertices = 4;
        }

        private void LateUpdate()
        {
            if (anchorPoint == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            Vector3 anchor = anchorPoint.position;
            Vector2 collarOffset = _player.CollarOffset;
            Vector3 collar = _player.transform.position + (Vector3)collarOffset;

            // Update colors based on leash state
            LeashState state = _player.GetLeashState();
            Color ropeColor = state switch
            {
                LeashState.Extended => ExtendedRopeColor,
                LeashState.Yanked => YankedRopeColor,
                _ => NormalRopeColor
            };
            _mainLine.startColor = ropeColor;
            _mainLine.endColor = ropeColor;

            // Compute parabolic curve
            float distance = Vector3.Distance(anchor, collar);
            float sag = Mathf.Min(maxSag, distance * sagFactor);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float x = Mathf.Lerp(anchor.x, collar.x, t);
                float y = Mathf.Lerp(anchor.y, collar.y, t);
                y -= sag * 4f * t * (1f - t);
                _points[i] = new Vector3(x, y, 0f);
            }

            _mainLine.positionCount = segments + 1;
            _mainLine.SetPositions(_points);

            // Shadow offset slightly down
            for (int i = 0; i <= segments; i++)
                _points[i].y -= 0.01f;

            _shadowLine.positionCount = segments + 1;
            _shadowLine.SetPositions(_points);
        }

        public void SetAnchorPoint(Transform anchor)
        {
            anchorPoint = anchor;
        }

        public void SetVisible(bool visible)
        {
            _mainLine.enabled = visible;
            _shadowLine.enabled = visible;
        }

        private void OnDestroy()
        {
            if (_mainLineObj != null) Destroy(_mainLineObj);
            if (_shadowLineObj != null) Destroy(_shadowLineObj);
        }
    }
}
