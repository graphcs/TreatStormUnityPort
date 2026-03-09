using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;

namespace SnackAttack.Effects
{
    public enum MenuIntroPhase { CloudRollIn, Lightning, DogsMarch, Complete }

    public class MainMenuIntro : MonoBehaviour
    {
        public bool IsComplete { get; private set; }

        private IntroSettingsSO _settings;
        private RectTransform _root;
        private float _screenWidth = 1200f;
        private float _screenHeight = 1000f;

        // Timers
        private MenuIntroPhase _phase;
        private float _phaseTimer;
        private float _globalTimer;

        // Band area (behind logo)
        private Rect _bandRect;
        private RectTransform _bandContainer;
        private float _dogGroundY;

        // Sky gradient
        private Image _bandBackground;

        // Clouds
        private readonly List<StormCloud> _clouds = new();

        // Lightning
        private readonly List<float> _pendingBoltTimes = new();
        private float _lightningFlash;
        private Image _flashOverlay;
        private readonly List<BoltData> _activeBolts = new();
        private RectTransform _boltContainer;

        // Logo glow
        private float _logoGlow;
        private UICircleDrawer _logoGlowDrawer;
        private RectTransform _logoGlowRect;
        private Rect _logoRect;

        // Dogs
        private Image _dogLeftImage;
        private Image _dogRightImage;
        private Sprite[] _dogLeftRunSprites;
        private Sprite[] _dogRightRunSprites;
        private float _dogLeftX, _dogLeftTargetX;
        private float _dogRightX, _dogRightTargetX;
        private float _dogStrideTimer;
        private float _lastLeftPuffX, _lastRightPuffX;
        private UIParticlePool _dustPool;
        private RectTransform _dustContainer;

        private struct BoltData
        {
            public Image image;
            public float age;
            public float duration;
        }

        public void Initialize(RectTransform root, IntroSettingsSO settings, Rect logoRect)
        {
            _root = root;
            _settings = settings;
            _logoRect = logoRect;

            float paddingX = _screenWidth * _settings.mmBandPaddingX;
            float bandTop = Mathf.Max(0f, logoRect.y - _settings.mmBandTopOffset);
            float bandHeight = Mathf.Min(_screenHeight * _settings.mmBandHeightFraction,
                logoRect.height + 210f);
            _bandRect = new Rect(paddingX, bandTop, _screenWidth - paddingX * 2f, bandHeight);
            _dogGroundY = Mathf.Min(_screenHeight * 0.5f, bandTop + bandHeight + _settings.mmDogGroundYPadding);

            // Band container (clipped area for clouds)
            var bandGo = new GameObject("IntroBand");
            _bandContainer = bandGo.AddComponent<RectTransform>();
            _bandContainer.SetParent(_root, false);
            _bandContainer.anchorMin = new Vector2(0f, 1f);
            _bandContainer.anchorMax = new Vector2(0f, 1f);
            _bandContainer.pivot = new Vector2(0f, 1f);
            _bandContainer.anchoredPosition = new Vector2(_bandRect.x, -_bandRect.y);
            _bandContainer.sizeDelta = new Vector2(_bandRect.width, _bandRect.height);

            // Add mask to clip clouds to band area
            bandGo.AddComponent<RectMask2D>();

            // Band background gradient
            var bgGo = new GameObject("BandBG");
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.SetParent(_bandContainer, false);
            SetStretchAll(bgRect);
            _bandBackground = bgGo.AddComponent<Image>();
            _bandBackground.raycastTarget = false;
            _bandBackground.color = _settings.mmBandInitialColor;

            // Flash overlay
            var flashGo = new GameObject("MMFlash");
            var flashRect = flashGo.AddComponent<RectTransform>();
            flashRect.SetParent(_bandContainer, false);
            SetStretchAll(flashRect);
            _flashOverlay = flashGo.AddComponent<Image>();
            _flashOverlay.raycastTarget = false;
            _flashOverlay.color = _settings.mmFlashOverlayColor;

            // Bolt container
            var boltGo = new GameObject("MMBolts");
            _boltContainer = boltGo.AddComponent<RectTransform>();
            _boltContainer.SetParent(_bandContainer, false);
            SetStretchAll(_boltContainer);

            // Logo glow
            var glowGo = new GameObject("LogoGlow");
            _logoGlowRect = glowGo.AddComponent<RectTransform>();
            _logoGlowRect.SetParent(_root, false);
            _logoGlowRect.anchorMin = new Vector2(0f, 1f);
            _logoGlowRect.anchorMax = new Vector2(0f, 1f);
            _logoGlowRect.pivot = new Vector2(0.5f, 0.5f);
            _logoGlowRect.anchoredPosition = new Vector2(
                _logoRect.x + _logoRect.width * 0.5f,
                -(_logoRect.y + _logoRect.height * 0.5f)
            );
            _logoGlowRect.sizeDelta = new Vector2(
                _logoRect.width + _settings.mmLogoGlowPaddingX,
                _logoRect.height + _settings.mmLogoGlowPaddingY);
            _logoGlowDrawer = glowGo.AddComponent<UICircleDrawer>();
            _logoGlowDrawer.raycastTarget = false;

            // Dust container (foreground, over everything)
            var dustGo = new GameObject("MMDust");
            _dustContainer = dustGo.AddComponent<RectTransform>();
            _dustContainer.SetParent(_root, false);
            SetStretchAll(_dustContainer);
            _dustPool = new UIParticlePool();
            _dustPool.Initialize(_dustContainer, _settings.mmDustPoolSize);

            // Build clouds
            BuildClouds();

            // Build dogs
            BuildDogs();
        }

        public void StartIntro()
        {
            _phase = MenuIntroPhase.CloudRollIn;
            _phaseTimer = 0f;
            _globalTimer = 0f;
            IsComplete = false;
            _lightningFlash = 0f;
            _logoGlow = 0f;
            _dogStrideTimer = 0f;

            if (_dogLeftImage != null) _dogLeftImage.enabled = false;
            if (_dogRightImage != null) _dogRightImage.enabled = false;

            EventBus.Emit(GameEvent.PlaySound, new Dictionary<string, object>
            {
                { "sound", "start" }
            });
        }

        private void Update()
        {
            if (IsComplete) return;

            float dt = Time.unscaledDeltaTime;
            _phaseTimer += dt;
            _globalTimer += dt;

            UpdateLightningDecay(dt);

            switch (_phase)
            {
                case MenuIntroPhase.CloudRollIn:
                    UpdateCloudRollIn();
                    break;
                case MenuIntroPhase.Lightning:
                    UpdateLightningPhase(dt);
                    break;
                case MenuIntroPhase.DogsMarch:
                    UpdateDogsMarch(dt);
                    break;
            }

            UpdateLogoGlow(dt);
            UpdateBolts(dt);

            float phaseDur = GetPhaseDuration(_phase);
            if (phaseDur > 0f && _phaseTimer >= phaseDur)
            {
                AdvancePhase();
            }
        }

        private float GetPhaseDuration(MenuIntroPhase phase)
        {
            return phase switch
            {
                MenuIntroPhase.CloudRollIn => _settings.mmCloudRollInDuration,
                MenuIntroPhase.Lightning => _settings.mmLightningDuration,
                MenuIntroPhase.DogsMarch => _settings.mmDogsMarchDuration,
                _ => 0f
            };
        }

        private void AdvancePhase()
        {
            switch (_phase)
            {
                case MenuIntroPhase.CloudRollIn:
                    _phase = MenuIntroPhase.Lightning;
                    _phaseTimer = 0f;
                    _pendingBoltTimes.Clear();
                    if (_settings.menuBoltTimes != null)
                        _pendingBoltTimes.AddRange(_settings.menuBoltTimes);
                    break;
                case MenuIntroPhase.Lightning:
                    _phase = MenuIntroPhase.DogsMarch;
                    _phaseTimer = 0f;
                    if (_dogLeftImage != null) _dogLeftImage.enabled = true;
                    if (_dogRightImage != null) _dogRightImage.enabled = true;
                    break;
                case MenuIntroPhase.DogsMarch:
                    _phase = MenuIntroPhase.Complete;
                    IsComplete = true;
                    EventBus.Emit(GameEvent.IntroComplete);
                    break;
            }
        }

        // ── Clouds ──
        private void BuildClouds()
        {
            Sprite[] sprites = { _settings.cloudSprite1, _settings.cloudSprite2 };

            BuildMenuCloudLayer(sprites, 0,
                _settings.mmCloudLayer0Count, _settings.mmCloudLayer0ScaleMin,
                _settings.mmCloudLayer0ScaleMax, _settings.mmCloudLayer0SwaySpeed);
            BuildMenuCloudLayer(sprites, 1,
                _settings.mmCloudLayer1Count, _settings.mmCloudLayer1ScaleMin,
                _settings.mmCloudLayer1ScaleMax, _settings.mmCloudLayer1SwaySpeed);
            BuildMenuCloudLayer(sprites, 2,
                _settings.mmCloudLayer2Count, _settings.mmCloudLayer2ScaleMin,
                _settings.mmCloudLayer2ScaleMax, _settings.mmCloudLayer2SwaySpeed);
        }

        private void BuildMenuCloudLayer(Sprite[] sprites, int layer,
            int count, float scaleMin, float scaleMax, float swaySpeed)
        {
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = sprites[i % sprites.Length];
                if (sprite == null) continue;

                float side = (i % 2 == 0) ? -1f : 1f;
                float startX = side * (_bandRect.width + Random.Range(
                    _settings.mmCloudStartOffscreenMin, _settings.mmCloudStartOffscreenMax));
                float targetX = (_bandRect.width / Mathf.Max(count, 1)) * i
                    - sprite.rect.width * (_settings.mmCloudTargetWidthFactor * scaleMin)
                    + Random.Range(-_settings.mmCloudTargetJitter, _settings.mmCloudTargetJitter);
                float y = Random.Range(0f, _bandRect.height * _settings.mmCloudYRandomMax)
                    + layer * _bandRect.height * _settings.mmCloudYLayerFactor;
                float scale = Random.Range(scaleMin, scaleMax);
                float speed = Random.Range(100f, 200f);
                float swayAmount = Random.Range(6f, 14f) * (1f + layer * _settings.mmCloudSwayLayerMult);

                var cloud = new StormCloud(_bandContainer, sprite, startX, targetX,
                    y, speed, layer, scale);
                _clouds.Add(cloud);
            }
        }

        private void UpdateCloudRollIn()
        {
            float gatherT = _phaseTimer / _settings.mmCloudRollInDuration;
            float swaySpeed = _settings.mmCloudSwaySpeed;
            float swayAmount = _settings.mmCloudSwayAmount;

            foreach (var cloud in _clouds)
            {
                float layerSway = swayAmount * (1f + cloud.Layer * _settings.mmCloudSwayLayerMult);
                cloud.UpdateMenuGather(gatherT, _globalTimer,
                    swaySpeed + cloud.Layer * 0.15f, layerSway);
            }

            // Darken band background
            float storm_t = Mathf.Clamp01(gatherT);
            Color initC = _settings.mmBandInitialColor;
            Color stormC = _settings.mmBandStormColor;
            float alpha = Mathf.Lerp(initC.a, stormC.a, storm_t);
            _bandBackground.color = new Color(
                Mathf.Lerp(initC.r, stormC.r, storm_t),
                Mathf.Lerp(initC.g, stormC.g, storm_t),
                Mathf.Lerp(initC.b, stormC.b, storm_t),
                alpha
            );

            // Tint clouds
            float cloudAlpha = Mathf.Lerp(_settings.mmCloudGatherAlphaMin,
                _settings.mmCloudGatherAlphaMax, storm_t);
            foreach (var cloud in _clouds)
                cloud.SetAlpha(cloudAlpha);

            _logoGlow = Mathf.Lerp(0f, _settings.mmLogoGlowGatherMax, gatherT);
        }

        // ── Lightning ──
        private void UpdateLightningPhase(float dt)
        {
            for (int i = _pendingBoltTimes.Count - 1; i >= 0; i--)
            {
                if (_phaseTimer >= _pendingBoltTimes[i])
                {
                    SpawnMenuBolt();
                    _pendingBoltTimes.RemoveAt(i);
                }
            }

            // Keep clouds updated
            foreach (var cloud in _clouds)
                cloud.UpdateMenuGather(1f, _globalTimer, _settings.mmCloudSwaySpeed, _settings.mmCloudSwayAmount);
        }

        private void SpawnMenuBolt()
        {
            float localX = Random.Range(_bandRect.width * _settings.mmBoltXMinFraction,
                _bandRect.width * _settings.mmBoltXMaxFraction);
            float localY = Random.Range(_bandRect.height * _settings.mmBoltYMinFraction,
                _bandRect.height * _settings.mmBoltYMaxFraction);
            float boltHeight = _bandRect.height * Random.Range(
                _settings.mmBoltHeightMinFraction, _settings.mmBoltHeightMaxFraction);

            // Create bolt image (simple bright line)
            var boltGo = new GameObject("MenuBolt");
            var boltRect = boltGo.AddComponent<RectTransform>();
            boltRect.SetParent(_boltContainer, false);
            boltRect.anchorMin = new Vector2(0f, 1f);
            boltRect.anchorMax = new Vector2(0f, 1f);
            boltRect.pivot = new Vector2(0.5f, 1f);
            boltRect.anchoredPosition = new Vector2(localX, -localY);
            boltRect.sizeDelta = new Vector2(_settings.mmBoltWidth, boltHeight);

            var boltImage = boltGo.AddComponent<Image>();
            boltImage.raycastTarget = false;
            boltImage.color = _settings.mmBoltColor;

            _activeBolts.Add(new BoltData
            {
                image = boltImage,
                age = 0f,
                duration = Random.Range(_settings.mmBoltDurationMin, _settings.mmBoltDurationMax)
            });

            _lightningFlash = 1f;
            _logoGlow = _settings.mmBoltFlashGlow;

            EventBus.Emit(GameEvent.PlaySound, new Dictionary<string, object>
            {
                { "sound", "thunder" }
            });
        }

        private void UpdateLightningDecay(float dt)
        {
            _lightningFlash = Mathf.Max(0f, _lightningFlash - dt * _settings.mmLightningFlashDecay);
            Color fc = _settings.mmFlashOverlayColor;
            _flashOverlay.color = new Color(fc.r, fc.g, fc.b, _lightningFlash * _settings.mmFlashAlphaMultiplier);
        }

        private void UpdateBolts(float dt)
        {
            for (int i = _activeBolts.Count - 1; i >= 0; i--)
            {
                var bolt = _activeBolts[i];
                bolt.age += dt;
                float t = bolt.age / bolt.duration;

                float alpha;
                if (t < _settings.mmBoltFadeInEnd)
                    alpha = t / _settings.mmBoltFadeInEnd;
                else if (t < _settings.mmBoltFullEnd)
                    alpha = 1f;
                else
                    alpha = Mathf.Max(0f, 1f - (t - _settings.mmBoltFullEnd) / (1f - _settings.mmBoltFullEnd));

                if (alpha <= 0f || bolt.age >= bolt.duration)
                {
                    if (bolt.image != null) Destroy(bolt.image.gameObject);
                    _activeBolts.RemoveAt(i);
                }
                else
                {
                    var c = bolt.image.color;
                    c.a = alpha;
                    bolt.image.color = c;
                    _activeBolts[i] = bolt;
                }
            }
        }

        // ── Logo Glow ──
        private void UpdateLogoGlow(float dt)
        {
            _logoGlow = Mathf.Max(0f, _logoGlow - dt * _settings.mmLogoGlowDecay);

            if (_logoGlow > _settings.mmLogoGlowThreshold && _logoGlowDrawer != null)
            {
                _logoGlowDrawer.Clear();
                float glowAlpha = _logoGlow * _settings.mmLogoGlowAlphaMultiplier;
                var c1 = _settings.mmLogoGlowColor;
                c1.a = glowAlpha;
                float hw = _logoGlowRect.sizeDelta.x * 0.5f;
                float hh = _logoGlowRect.sizeDelta.y * 0.5f;
                _logoGlowDrawer.AddFilledCircle(Vector2.zero, Mathf.Min(hw, hh), c1);

                var c2 = _settings.mmLogoGlowInnerColor;
                c2.a = glowAlpha * _settings.mmLogoGlowInnerAlphaFactor;
                _logoGlowDrawer.AddFilledCircle(Vector2.zero, Mathf.Min(hw, hh) * _settings.mmLogoGlowInnerRatio, c2);
                _logoGlowDrawer.Rebuild();
            }
            else if (_logoGlowDrawer != null)
            {
                _logoGlowDrawer.Clear();
            }
        }

        // ── Dogs ──
        private void BuildDogs()
        {
            var charDb = GameManager.Instance.CharacterDatabase;
            CharacterSO leftChar = charDb.GetById(_settings.mmLeftDogId);
            CharacterSO rightChar = charDb.GetById(_settings.mmRightDogId);

            float dogSize = _settings.mmDogSize;

            // Left dog
            if (leftChar != null && leftChar.runSprites != null && leftChar.runSprites.Length > 0)
            {
                _dogLeftRunSprites = leftChar.runSprites;
                var dogGo = new GameObject("DogLeft");
                var dogRect = dogGo.AddComponent<RectTransform>();
                dogRect.SetParent(_root, false);
                dogRect.anchorMin = new Vector2(0f, 1f);
                dogRect.anchorMax = new Vector2(0f, 1f);
                dogRect.pivot = new Vector2(0f, 1f);
                dogRect.sizeDelta = new Vector2(dogSize, dogSize);
                _dogLeftImage = dogGo.AddComponent<Image>();
                _dogLeftImage.sprite = _dogLeftRunSprites[0];
                _dogLeftImage.preserveAspect = true;
                _dogLeftImage.raycastTarget = false;
                _dogLeftImage.enabled = false;
            }

            // Right dog
            if (rightChar != null && rightChar.runSprites != null && rightChar.runSprites.Length > 0)
            {
                _dogRightRunSprites = rightChar.runSprites;
                var dogGo = new GameObject("DogRight");
                var dogRect = dogGo.AddComponent<RectTransform>();
                dogRect.SetParent(_root, false);
                dogRect.anchorMin = new Vector2(0f, 1f);
                dogRect.anchorMax = new Vector2(0f, 1f);
                dogRect.pivot = new Vector2(0f, 1f);
                dogRect.sizeDelta = new Vector2(dogSize, dogSize);
                _dogRightImage = dogGo.AddComponent<Image>();
                _dogRightImage.sprite = _dogRightRunSprites[0];
                _dogRightImage.preserveAspect = true;
                _dogRightImage.raycastTarget = false;
                _dogRightImage.enabled = false;
                // Face right by default, flip to face left
                dogRect.localScale = new Vector3(-1f, 1f, 1f);
            }

            // Positions
            _dogLeftX = -dogSize - _settings.mmDogStartOffscreenPadding;
            _dogLeftTargetX = _logoRect.x - dogSize * _settings.mmDogLeftTargetFactor;
            _lastLeftPuffX = _dogLeftX;

            _dogRightX = _screenWidth + _settings.mmDogStartOffscreenPadding;
            _dogRightTargetX = _logoRect.x + _logoRect.width - dogSize * _settings.mmDogRightTargetFactor;
            _lastRightPuffX = _dogRightX;
        }

        private void UpdateDogsMarch(float dt)
        {
            float t = EasingUtils.EaseInOutCubic(
                _phaseTimer / _settings.mmDogsMarchDuration);

            // Move dogs toward targets with smooth interpolation
            float lerpBase = _settings.mmDogMarchLerpBase;
            float lerpEase = _settings.mmDogMarchLerpEase;
            _dogLeftX = Mathf.Lerp(_dogLeftX, _dogLeftTargetX, dt * lerpBase + t * lerpEase);
            _dogRightX = Mathf.Lerp(_dogRightX, _dogRightTargetX, dt * lerpBase + t * lerpEase);

            _dogStrideTimer += dt * (1f + (1f - t) * _settings.mmDogStrideAccel);

            // Update glow
            _logoGlow = Mathf.Max(_logoGlow,
                Mathf.Lerp(_settings.mmDogLogoGlowMin, _settings.mmDogLogoGlowMax, t));

            // Animate sprites
            float frameRate = _settings.mmDogFrameRate;
            if (_dogLeftRunSprites != null && _dogLeftRunSprites.Length > 0)
            {
                int frame = Mathf.FloorToInt(_dogStrideTimer * frameRate) % _dogLeftRunSprites.Length;
                _dogLeftImage.sprite = _dogLeftRunSprites[frame];
            }
            if (_dogRightRunSprites != null && _dogRightRunSprites.Length > 0)
            {
                int frame = Mathf.FloorToInt(_dogStrideTimer * frameRate) % _dogRightRunSprites.Length;
                _dogRightImage.sprite = _dogRightRunSprites[frame];
            }

            // Bob
            float bob = Mathf.Sin(_dogStrideTimer * _settings.mmDogBobFrequency) * _settings.mmDogBobAmplitude;
            float dogSize = _settings.mmDogSize;

            if (_dogLeftImage != null)
            {
                _dogLeftImage.rectTransform.anchoredPosition =
                    new Vector2(_dogLeftX, -(_dogGroundY - dogSize + bob));
            }
            if (_dogRightImage != null)
            {
                _dogRightImage.rectTransform.anchoredPosition =
                    new Vector2(_dogRightX, -(_dogGroundY - dogSize + bob));
            }

            // Dust puffs
            float dustDist = _settings.mmDogDustDistance;
            if (Mathf.Abs(_dogLeftX - _lastLeftPuffX) >= dustDist)
            {
                EmitDust(_dogLeftX + _settings.mmDogDustOffsetLeftX,
                    _dogGroundY + _settings.mmDogDustOffsetY);
                _lastLeftPuffX = _dogLeftX;
            }
            if (Mathf.Abs(_dogRightX - _lastRightPuffX) >= dustDist)
            {
                EmitDust(_dogRightX + _settings.mmDogDustOffsetRightX,
                    _dogGroundY + _settings.mmDogDustOffsetY);
                _lastRightPuffX = _dogRightX;
            }
            _dustPool.UpdateAll(dt);

            // Keep clouds swaying
            foreach (var cloud in _clouds)
                cloud.UpdateMenuGather(1f, _globalTimer, _settings.mmCloudSwaySpeed, _settings.mmCloudSwayAmount);
        }

        private void EmitDust(float x, float y)
        {
            var color = _settings.mmDustColor;
            _dustPool.Emit(
                new Vector2(x, -y),
                new Vector2(
                    Random.Range(-_settings.mmDustVxRange, _settings.mmDustVxRange),
                    Random.Range(_settings.mmDustVyMin, _settings.mmDustVyMax)),
                _settings.mmDustLifetime,
                _settings.mmDustSize,
                color,
                growthRate: _settings.mmDustGrowthRate
            );
        }

        private void OnDestroy()
        {
            foreach (var cloud in _clouds)
                cloud.Destroy();
            _clouds.Clear();

            foreach (var bolt in _activeBolts)
            {
                if (bolt.image != null)
                    Destroy(bolt.image.gameObject);
            }
            _activeBolts.Clear();

            _dustPool?.DestroyAll();
        }

        private static void SetStretchAll(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
