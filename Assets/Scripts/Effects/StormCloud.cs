using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;

namespace SnackAttack.Effects
{
    public class StormCloud
    {
        public Image Image { get; private set; }
        public RectTransform Rect { get; private set; }
        public int Layer { get; private set; }

        private float _startX;
        private float _targetX;
        private float _x;
        private float _baseY;
        private float _speed;
        private float _bobOffset;

        public StormCloud(RectTransform parent, Sprite sprite, float startX, float targetX,
            float y, float speed, int layer, float scale)
        {
            var go = new GameObject($"Cloud_L{layer}");
            Rect = go.AddComponent<RectTransform>();
            Rect.SetParent(parent, false);
            Rect.anchorMin = new Vector2(0f, 1f);
            Rect.anchorMax = new Vector2(0f, 1f);
            Rect.pivot = new Vector2(0f, 1f);

            Image = go.AddComponent<Image>();
            Image.sprite = sprite;
            Image.preserveAspect = true;
            Image.raycastTarget = false;

            float w = sprite.rect.width * scale;
            float h = sprite.rect.height * scale;
            Rect.sizeDelta = new Vector2(w, h);

            _startX = startX;
            _targetX = targetX;
            _x = startX;
            _baseY = y;
            _speed = speed;
            Layer = layer;
            _bobOffset = Random.Range(0f, Mathf.PI * 2f);

            UpdatePosition();
        }

        public void UpdateGather(float dt, float gatherT, float globalTime, IntroSettingsSO settings = null)
        {
            float speedMultHigh = settings != null ? settings.cloudSpeedMultHigh : 2.5f;
            float speedMultLow = settings != null ? settings.cloudSpeedMultLow : 0.5f;
            float transition = settings != null ? settings.cloudSpeedMultTransition : 0.4f;
            float threshold = settings != null ? settings.cloudApproachThreshold : 2f;
            float lerpSpeed = settings != null ? settings.cloudApproachLerpSpeed : 2f;
            float bobSpeed = settings != null ? settings.cloudBobSpeed : 0.7f;
            float bobAmp = settings != null ? settings.cloudBobAmplitude : 3f;

            float diff = _targetX - _x;
            float speedMult = gatherT < transition ? speedMultHigh :
                Mathf.Lerp(speedMultHigh, speedMultLow, (gatherT - transition) / (1f - transition));
            float eased = EasingUtils.EaseInOutCubic(gatherT);
            float approachSpeed = Mathf.Abs(_speed) * (1f + (1f - eased) * speedMult);

            if (Mathf.Abs(diff) > threshold)
            {
                float dir = diff > 0f ? 1f : -1f;
                _x += dir * approachSpeed * dt;
                if ((dir > 0f && _x > _targetX) || (dir < 0f && _x < _targetX))
                    _x = _targetX;
            }
            else
            {
                _x = Mathf.Lerp(_x, _targetX, dt * lerpSpeed);
            }

            float bobY = Mathf.Sin(globalTime * bobSpeed + _bobOffset) * bobAmp * dt;
            _baseY += bobY;

            UpdatePosition();
        }

        public void UpdateMenuGather(float gatherT, float globalTime, float swaySpeed, float swayAmount)
        {
            float travelBase = 1.15f;
            float travelLayerMult = 0.1f;
            float travelT = EasingUtils.EaseInOutCubic(
                Mathf.Min(1f, gatherT * (travelBase + Layer * travelLayerMult)));
            _x = Mathf.Lerp(_startX, _targetX, travelT);
            float y = _baseY + Mathf.Sin(globalTime * swaySpeed + _bobOffset) * swayAmount;

            Rect.anchoredPosition = new Vector2(_x, -y);
        }

        public void UpdateMenuGather(float gatherT, float globalTime, float swaySpeed, float swayAmount,
            IntroSettingsSO settings)
        {
            float travelBase = settings != null ? settings.menuCloudTravelSpeedBase : 1.15f;
            float travelLayerMult = settings != null ? settings.menuCloudTravelSpeedLayerMult : 0.1f;
            float travelT = EasingUtils.EaseInOutCubic(
                Mathf.Min(1f, gatherT * (travelBase + Layer * travelLayerMult)));
            _x = Mathf.Lerp(_startX, _targetX, travelT);
            float y = _baseY + Mathf.Sin(globalTime * swaySpeed + _bobOffset) * swayAmount;

            Rect.anchoredPosition = new Vector2(_x, -y);
        }

        public void SetAlpha(float alpha)
        {
            var c = Image.color;
            c.a = alpha;
            Image.color = c;
        }

        public void Destroy()
        {
            if (Image != null && Image.gameObject != null)
                Object.Destroy(Image.gameObject);
        }

        private void UpdatePosition()
        {
            Rect.anchoredPosition = new Vector2(_x, -_baseY);
        }
    }
}
