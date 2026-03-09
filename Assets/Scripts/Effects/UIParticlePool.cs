using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SnackAttack.Effects
{
    public class UIParticlePool
    {
        private struct Particle
        {
            public Image image;
            public RectTransform rect;
            public Vector2 velocity;
            public float lifetime;
            public float maxLifetime;
            public Vector2 size;
            public float growthRate;
            public Color baseColor;
            public float rotSpeed;
        }

        private RectTransform _container;
        private int _maxParticles;
        private readonly List<Particle> _active = new();
        private readonly Stack<Image> _pool = new();

        private static Sprite _sharedSprite;

        private static Sprite GetSharedSprite()
        {
            if (_sharedSprite == null)
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply();
                _sharedSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            }
            return _sharedSprite;
        }

        public void Initialize(RectTransform container, int maxParticles)
        {
            _container = container;
            _maxParticles = maxParticles;
        }

        public void Emit(Vector2 pos, Vector2 vel, float lifetime, float size, Color color,
            float growthRate = 0f, float rotSpeed = 0f)
        {
            Emit(pos, vel, lifetime, new Vector2(size, size), color, growthRate, rotSpeed);
        }

        public void Emit(Vector2 pos, Vector2 vel, float lifetime, Vector2 size, Color color,
            float growthRate = 0f, float rotSpeed = 0f)
        {
            if (_active.Count >= _maxParticles) return;

            Image img;
            if (_pool.Count > 0)
            {
                img = _pool.Pop();
                img.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Particle");
                var rect = go.AddComponent<RectTransform>();
                rect.SetParent(_container, false);
                img = go.AddComponent<Image>();
                img.sprite = GetSharedSprite();
                img.raycastTarget = false;
            }

            var pRect = img.GetComponent<RectTransform>();
            pRect.anchoredPosition = pos;
            pRect.sizeDelta = size;
            pRect.localRotation = Quaternion.identity;
            img.color = color;

            _active.Add(new Particle
            {
                image = img,
                rect = pRect,
                velocity = vel,
                lifetime = 0f,
                maxLifetime = lifetime,
                size = size,
                growthRate = growthRate,
                baseColor = color,
                rotSpeed = rotSpeed
            });
        }

        public void UpdateAll(float dt)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var p = _active[i];
                p.lifetime += dt;

                if (p.lifetime >= p.maxLifetime)
                {
                    ReturnToPool(p.image);
                    _active.RemoveAt(i);
                    continue;
                }

                // Move
                p.rect.anchoredPosition += p.velocity * dt;

                // Grow
                if (p.growthRate != 0f)
                {
                    p.size += new Vector2(p.growthRate, p.growthRate) * dt;
                    p.rect.sizeDelta = p.size;
                }

                // Rotate
                if (p.rotSpeed != 0f)
                    p.rect.Rotate(0f, 0f, p.rotSpeed * dt);

                // Alpha fade
                float alpha = p.baseColor.a * (1f - p.lifetime / p.maxLifetime);
                var c = p.baseColor;
                c.a = alpha;
                p.image.color = c;

                _active[i] = p;
            }
        }

        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ReturnToPool(_active[i].image);
            }
            _active.Clear();
        }

        public void DestroyAll()
        {
            foreach (var p in _active)
            {
                if (p.image != null)
                    Object.Destroy(p.image.gameObject);
            }
            _active.Clear();

            while (_pool.Count > 0)
            {
                var img = _pool.Pop();
                if (img != null)
                    Object.Destroy(img.gameObject);
            }
        }

        private void ReturnToPool(Image img)
        {
            if (img == null) return;
            img.gameObject.SetActive(false);
            _pool.Push(img);
        }
    }
}
