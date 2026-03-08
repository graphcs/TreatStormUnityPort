using UnityEngine;

namespace SnackAttack.Effects
{
    public class ScreenShake
    {
        public float OffsetX { get; private set; }
        public float OffsetY { get; private set; }

        private float _intensity;
        private float _decay;

        public void Trigger(float intensity, float decay = 8f)
        {
            _intensity = intensity;
            _decay = decay;
        }

        public void Update(float dt)
        {
            if (_intensity <= 0.1f)
            {
                OffsetX = 0f;
                OffsetY = 0f;
                return;
            }

            _intensity *= Mathf.Max(0f, 1f - _decay * dt);
            OffsetX = Random.Range(-_intensity, _intensity);
            OffsetY = Random.Range(-_intensity, _intensity);
        }

        public void Reset()
        {
            _intensity = 0f;
            OffsetX = 0f;
            OffsetY = 0f;
        }
    }
}
