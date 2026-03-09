using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SnackAttack.Effects
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UICircleDrawer : MaskableGraphic
    {
        private enum CommandType { Ring, FilledCircle, Beam }

        private struct DrawCommand
        {
            public CommandType type;
            public Vector2 center;
            public float radius;
            public float width;
            public float height;
            public Color color;
        }

        private readonly List<DrawCommand> _commands = new();

        public void AddRing(Vector2 center, float radius, float width, Color color)
        {
            _commands.Add(new DrawCommand
            {
                type = CommandType.Ring,
                center = center,
                radius = radius,
                width = width,
                color = color
            });
        }

        public void AddFilledCircle(Vector2 center, float radius, Color color)
        {
            _commands.Add(new DrawCommand
            {
                type = CommandType.FilledCircle,
                center = center,
                radius = radius,
                color = color
            });
        }

        public void AddBeam(Vector2 center, float width, float height, Color color)
        {
            _commands.Add(new DrawCommand
            {
                type = CommandType.Beam,
                center = center,
                width = width,
                height = height,
                color = color
            });
        }

        public void Rebuild()
        {
            SetVerticesDirty();
        }

        public void Clear()
        {
            _commands.Clear();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            foreach (var cmd in _commands)
            {
                switch (cmd.type)
                {
                    case CommandType.Ring:
                        GenerateRing(vh, cmd);
                        break;
                    case CommandType.FilledCircle:
                        GenerateFilledCircle(vh, cmd);
                        break;
                    case CommandType.Beam:
                        GenerateBeam(vh, cmd);
                        break;
                }
            }
        }

        private void GenerateRing(VertexHelper vh, DrawCommand cmd)
        {
            const int segments = 32;
            int baseVert = vh.currentVertCount;
            float innerR = cmd.radius - cmd.width * 0.5f;
            float outerR = cmd.radius + cmd.width * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector2 inner = cmd.center + new Vector2(cos * innerR, sin * innerR);
                Vector2 outer = cmd.center + new Vector2(cos * outerR, sin * outerR);

                vh.AddVert(inner, cmd.color, Vector4.zero);
                vh.AddVert(outer, cmd.color, Vector4.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int vi = baseVert + i * 2;
                vh.AddTriangle(vi, vi + 1, vi + 3);
                vh.AddTriangle(vi, vi + 3, vi + 2);
            }
        }

        private void GenerateFilledCircle(VertexHelper vh, DrawCommand cmd)
        {
            const int segments = 24;
            int baseVert = vh.currentVertCount;

            // Center vertex
            vh.AddVert(cmd.center, cmd.color, Vector4.zero);

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector2 pt = cmd.center + new Vector2(Mathf.Cos(angle) * cmd.radius, Mathf.Sin(angle) * cmd.radius);
                vh.AddVert(pt, cmd.color, Vector4.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                vh.AddTriangle(baseVert, baseVert + 1 + i, baseVert + 2 + i);
            }
        }

        private void GenerateBeam(VertexHelper vh, DrawCommand cmd)
        {
            int baseVert = vh.currentVertCount;
            float hw = cmd.width * 0.5f;
            float hh = cmd.height * 0.5f;

            vh.AddVert(cmd.center + new Vector2(-hw, -hh), cmd.color, Vector4.zero);
            vh.AddVert(cmd.center + new Vector2(hw, -hh), cmd.color, Vector4.zero);
            vh.AddVert(cmd.center + new Vector2(hw, hh), cmd.color, Vector4.zero);
            vh.AddVert(cmd.center + new Vector2(-hw, hh), cmd.color, Vector4.zero);

            vh.AddTriangle(baseVert, baseVert + 1, baseVert + 2);
            vh.AddTriangle(baseVert, baseVert + 2, baseVert + 3);
        }
    }
}
