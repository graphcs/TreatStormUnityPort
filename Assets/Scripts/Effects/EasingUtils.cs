using UnityEngine;

namespace SnackAttack.Effects
{
    public static class EasingUtils
    {
        public static float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.5f)
                return 4f * t * t * t;
            return 1f - Mathf.Pow(-2f * t + 2f, 3) / 2f;
        }

        public static float EaseOutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        public static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3);
        }

        public static float EaseInQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        public static Color LerpColor(Color a, Color b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(
                a.r + (b.r - a.r) * t,
                a.g + (b.g - a.g) * t,
                a.b + (b.b - a.b) * t,
                a.a + (b.a - a.a) * t
            );
        }
    }
}
