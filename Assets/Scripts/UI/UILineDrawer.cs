using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SnackAttack.UI
{
    /// <summary>
    /// Custom MaskableGraphic that draws polylines via quad strips.
    /// Replaces LineRenderer for canvas-based rendering (leash rope + lightning).
    /// </summary>
    public class UILineDrawer : MaskableGraphic
    {
        private readonly List<Vector2> _points = new();
        private float _startWidth = 4f;
        private float _endWidth = 4f;

        public void SetPoints(Vector2[] points)
        {
            _points.Clear();
            if (points != null)
                _points.AddRange(points);
            SetVerticesDirty();
        }

        public void SetWidth(float start, float end)
        {
            _startWidth = start;
            _endWidth = end;
            SetVerticesDirty();
        }

        public void SetLineColor(Color c)
        {
            color = c;
            SetVerticesDirty();
        }

        public void Clear()
        {
            _points.Clear();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_points.Count < 2) return;

            int segCount = _points.Count - 1;

            for (int i = 0; i < segCount; i++)
            {
                Vector2 a = _points[i];
                Vector2 b = _points[i + 1];
                Vector2 dir = (b - a).normalized;
                Vector2 perp = new Vector2(-dir.y, dir.x);

                float tA = (float)i / segCount;
                float tB = (float)(i + 1) / segCount;
                float wA = Mathf.Lerp(_startWidth, _endWidth, tA) * 0.5f;
                float wB = Mathf.Lerp(_startWidth, _endWidth, tB) * 0.5f;

                int vi = i * 4;

                vh.AddVert(a + perp * wA, color, Vector4.zero);
                vh.AddVert(a - perp * wA, color, Vector4.zero);
                vh.AddVert(b - perp * wB, color, Vector4.zero);
                vh.AddVert(b + perp * wB, color, Vector4.zero);

                vh.AddTriangle(vi, vi + 1, vi + 2);
                vh.AddTriangle(vi, vi + 2, vi + 3);
            }
        }
    }
}
