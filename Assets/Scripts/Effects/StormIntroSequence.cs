using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SnackAttack.Core;
using SnackAttack.UI;

namespace SnackAttack.Effects
{
    public enum StormPhase { CloudsGather, LightningStrike, ScreenFlicker, DogsMarch, Complete }

    public class StormIntroSequence : MonoBehaviour
    {
        public bool IsComplete { get; private set; }
        public StormPhase CurrentPhase { get; private set; }

        private IntroSettingsSO _settings;
        private RectTransform _root;
        private float _screenWidth;
        private float _screenHeight;

        // Timers
        private float _phaseTimer;
        private float _globalTimer;

        // Sky
        private Image _skyImage;

        // Clouds
        private readonly List<StormCloud> _clouds = new();
        private Sprite _cloudSprite1;
        private Sprite _cloudSprite2;

        // Rain
        private RectTransform _rainContainer;
        private UIParticlePool _rainPool;
        private float _rainIntensity;
        private float _wind;

        // Lightning
        private readonly List<float> _pendingBoltTimes = new();
        private float _lightningFlash;
        private Image _flashOverlay;
        private readonly List<LightningBolt> _activeBolts = new();
        private RectTransform _boltContainer;

        // Ground blooms
        private readonly List<GroundBloom> _groundBlooms = new();
        private RectTransform _bloomContainer;

        // Screen shake
        private ScreenShake _shake = new();
        private RectTransform _shakeRoot;

        // Flicker
        private float _flickerFlash;
        private int _flickerIndex;
        private float _flickerSubTimer;
        private Image _flickerOverlay;

        // Dog march
        private Image _dog1Image;
        private Image _dog2Image;
        private Sprite[] _dog1RunSprites;
        private Sprite[] _dog2RunSprites;
        private float _dog1StartX, _dog1TargetX, _dog1CurrentX;
        private float _dog2StartX, _dog2TargetX, _dog2CurrentX;
        private float _dogGroundY;
        private float _marchBobTimer;
        private float _lastStepX1, _lastStepX2;
        private RectTransform _dustContainer;
        private UIParticlePool _dustPool;

        // Title + GO
        private Image _titleImage;
        private Image _goImage;
        private float _titleScale;
        private float _titleAlpha;
        private float _goAlpha;
        private RectTransform _titleRect;
        private RectTransform _goRect;

        // Ground
        private Image _groundImage;

        private struct LightningBolt
        {
            public UILineDrawer drawer;
            public float age;
            public float duration;
        }

        private struct GroundBloom
        {
            public UICircleDrawer drawer;
            public float life;
            public float duration;
            public float maxRadius;
            public Vector2 center;
        }

        // Dog sizes (passed from character data)
        private float _dog1Size;
        private float _dog2Size;
        private bool _hasDog2;

        public void Initialize(RectTransform root, IntroSettingsSO settings,
            Sprite cloudSprite1, Sprite cloudSprite2,
            Sprite titleSprite, Sprite goSprite, Sprite groundSprite,
            Sprite[] dog1RunSprites, Sprite[] dog2RunSprites,
            float dogGroundY, float dog1Size = 216f, float dog2Size = 216f,
            float dog1TargetX = -1f, float dog2TargetX = -1f)
        {
            _root = root;
            _settings = settings;
            _screenWidth = 1200f;
            _screenHeight = 1000f;
            _cloudSprite1 = cloudSprite1;
            _cloudSprite2 = cloudSprite2;
            _dog1RunSprites = dog1RunSprites;
            _dog2RunSprites = dog2RunSprites;
            _dogGroundY = dogGroundY;
            _dog1Size = dog1Size * _settings.dogRenderScale;
            _dog2Size = dog2Size * _settings.dogRenderScale;
            _hasDog2 = dog2RunSprites != null && dog2RunSprites.Length > 0;

            // Shake root (parent of all visual content, offset for shake)
            var shakeGo = new GameObject("ShakeRoot");
            _shakeRoot = shakeGo.AddComponent<RectTransform>();
            _shakeRoot.SetParent(_root, false);
            SetStretchAll(_shakeRoot);

            // Sky background
            var skyGo = new GameObject("Sky");
            var skyRect = skyGo.AddComponent<RectTransform>();
            skyRect.SetParent(_shakeRoot, false);
            SetStretchAll(skyRect);
            _skyImage = skyGo.AddComponent<Image>();
            _skyImage.raycastTarget = false;
            _skyImage.color = _settings.clearSkyTop;

            // Cloud container
            var cloudGo = new GameObject("Clouds");
            var cloudRect = cloudGo.AddComponent<RectTransform>();
            cloudRect.SetParent(_shakeRoot, false);
            SetStretchAll(cloudRect);

            // Rain container
            var rainGo = new GameObject("Rain");
            _rainContainer = rainGo.AddComponent<RectTransform>();
            _rainContainer.SetParent(_shakeRoot, false);
            _rainContainer.anchorMin = new Vector2(0f, 1f);
            _rainContainer.anchorMax = new Vector2(1f, 1f);
            _rainContainer.pivot = new Vector2(0.5f, 1f);
            _rainContainer.sizeDelta = new Vector2(0f, _screenHeight);
            _rainContainer.anchoredPosition = Vector2.zero;
            _rainPool = new UIParticlePool();
            _rainPool.Initialize(_rainContainer, _settings.maxRainDrops);

            // Bolt container
            var boltGo = new GameObject("Bolts");
            _boltContainer = boltGo.AddComponent<RectTransform>();
            _boltContainer.SetParent(_shakeRoot, false);
            SetStretchAll(_boltContainer);

            // Bloom container
            var bloomGo = new GameObject("Blooms");
            _bloomContainer = bloomGo.AddComponent<RectTransform>();
            _bloomContainer.SetParent(_shakeRoot, false);
            SetStretchAll(_bloomContainer);

            // Flash overlay
            var flashGo = new GameObject("FlashOverlay");
            var flashRect = flashGo.AddComponent<RectTransform>();
            flashRect.SetParent(_shakeRoot, false);
            SetStretchAll(flashRect);
            _flashOverlay = flashGo.AddComponent<Image>();
            _flashOverlay.raycastTarget = false;
            _flashOverlay.color = new Color(
                _settings.flashOverlayColor.r,
                _settings.flashOverlayColor.g,
                _settings.flashOverlayColor.b, 0f);

            // Flicker overlay
            var flickerGo = new GameObject("FlickerOverlay");
            var flickerRect = flickerGo.AddComponent<RectTransform>();
            flickerRect.SetParent(_shakeRoot, false);
            SetStretchAll(flickerRect);
            _flickerOverlay = flickerGo.AddComponent<Image>();
            _flickerOverlay.raycastTarget = false;
            _flickerOverlay.color = new Color(1f, 1f, 1f, 0f);

            // Ground
            if (groundSprite != null)
            {
                var groundGo = new GameObject("Ground");
                var groundRt = groundGo.AddComponent<RectTransform>();
                groundRt.SetParent(_shakeRoot, false);
                groundRt.anchorMin = new Vector2(0f, 0f);
                groundRt.anchorMax = new Vector2(1f, 0f);
                groundRt.pivot = new Vector2(0.5f, 0f);
                float aspect = groundSprite.rect.height / groundSprite.rect.width;
                float groundH = _screenWidth * aspect;
                groundRt.sizeDelta = new Vector2(0f, groundH);
                groundRt.anchoredPosition = Vector2.zero;
                _groundImage = groundGo.AddComponent<Image>();
                _groundImage.sprite = groundSprite;
                _groundImage.preserveAspect = true;
                _groundImage.raycastTarget = false;
            }

            // Dust container
            var dustGo = new GameObject("Dust");
            _dustContainer = dustGo.AddComponent<RectTransform>();
            _dustContainer.SetParent(_shakeRoot, false);
            SetStretchAll(_dustContainer);
            _dustPool = new UIParticlePool();
            _dustPool.Initialize(_dustContainer, 100);

            // Dog 1 (from left)
            var dog1Go = new GameObject("Dog1");
            var dog1Rect = dog1Go.AddComponent<RectTransform>();
            dog1Rect.SetParent(_shakeRoot, false);
            dog1Rect.anchorMin = new Vector2(0f, 1f);
            dog1Rect.anchorMax = new Vector2(0f, 1f);
            dog1Rect.pivot = new Vector2(0f, 1f);
            dog1Rect.sizeDelta = new Vector2(_dog1Size, _dog1Size);
            _dog1Image = dog1Go.AddComponent<Image>();
            _dog1Image.preserveAspect = true;
            _dog1Image.raycastTarget = false;
            _dog1Image.enabled = false;
            if (_dog1RunSprites != null && _dog1RunSprites.Length > 0)
                _dog1Image.sprite = _dog1RunSprites[0];

            // Dog 2 (from right) — only if we have a 2nd player
            if (_hasDog2)
            {
                var dog2Go = new GameObject("Dog2");
                var dog2Rect = dog2Go.AddComponent<RectTransform>();
                dog2Rect.SetParent(_shakeRoot, false);
                dog2Rect.anchorMin = new Vector2(0f, 1f);
                dog2Rect.anchorMax = new Vector2(0f, 1f);
                dog2Rect.pivot = new Vector2(0f, 1f);
                dog2Rect.sizeDelta = new Vector2(_dog2Size, _dog2Size);
                _dog2Image = dog2Go.AddComponent<Image>();
                _dog2Image.preserveAspect = true;
                _dog2Image.raycastTarget = false;
                _dog2Image.enabled = false;
                if (_dog2RunSprites.Length > 0)
                    _dog2Image.sprite = _dog2RunSprites[0];
            }

            // Title
            if (titleSprite != null)
            {
                var titleGo = new GameObject("Title");
                _titleRect = titleGo.AddComponent<RectTransform>();
                _titleRect.SetParent(_shakeRoot, false);
                _titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                _titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                _titleRect.pivot = new Vector2(0.5f, 0.5f);
                float titleW = _screenWidth * _settings.titleWidthFraction;
                float titleAspect = titleSprite.rect.height / titleSprite.rect.width;
                _titleRect.sizeDelta = new Vector2(titleW, titleW * titleAspect);
                _titleRect.anchoredPosition = new Vector2(0f, _settings.titleYOffset);
                _titleImage = titleGo.AddComponent<Image>();
                _titleImage.sprite = titleSprite;
                _titleImage.preserveAspect = true;
                _titleImage.raycastTarget = false;
                _titleImage.enabled = false;
            }

            // GO
            if (goSprite != null)
            {
                var goGo = new GameObject("GO");
                _goRect = goGo.AddComponent<RectTransform>();
                _goRect.SetParent(_shakeRoot, false);
                _goRect.anchorMin = new Vector2(0.5f, 0.5f);
                _goRect.anchorMax = new Vector2(0.5f, 0.5f);
                _goRect.pivot = new Vector2(0.5f, 0.5f);
                float goW = _screenWidth * _settings.goWidthFraction;
                float goAspect = goSprite.rect.height / goSprite.rect.width;
                _goRect.sizeDelta = new Vector2(goW, goW * goAspect);
                _goRect.anchoredPosition = new Vector2(0f, _settings.goYOffset);
                _goImage = goGo.AddComponent<Image>();
                _goImage.sprite = goSprite;
                _goImage.preserveAspect = true;
                _goImage.raycastTarget = false;
                _goImage.enabled = false;
            }

            // Build clouds
            BuildClouds(cloudRect);

            // Set initial dog positions — use passed targets or fallback to defaults
            float offscreen = _settings.dogStartOffscreenDistance;
            _dog1StartX = -offscreen;
            _dog1TargetX = dog1TargetX >= 0f ? dog1TargetX : _screenWidth * 0.28f;
            _dog1CurrentX = _dog1StartX;
            _lastStepX1 = _dog1StartX;

            _dog2StartX = _screenWidth + offscreen;
            _dog2TargetX = dog2TargetX >= 0f ? dog2TargetX : _screenWidth * 0.58f;
            _dog2CurrentX = _dog2StartX;
            _lastStepX2 = _dog2StartX;
        }

        public void StartSequence()
        {
            CurrentPhase = StormPhase.CloudsGather;
            _phaseTimer = 0f;
            _globalTimer = 0f;
            IsComplete = false;
            _rainIntensity = 0f;
            _wind = 0f;
            _lightningFlash = 0f;
            _flickerFlash = 0f;
            _flickerIndex = 0;
            _flickerSubTimer = 0f;
            _titleScale = 0f;
            _titleAlpha = 0f;
            _goAlpha = 0f;
            _marchBobTimer = 0f;
            _shake.Reset();

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

            _shake.Update(dt);
            _shakeRoot.anchoredPosition = new Vector2(_shake.OffsetX, _shake.OffsetY);

            UpdateRain(dt);
            UpdateLightning(dt);
            UpdateBolts(dt);
            UpdateBlooms(dt);

            switch (CurrentPhase)
            {
                case StormPhase.CloudsGather:
                    UpdateCloudsGather(dt);
                    break;
                case StormPhase.LightningStrike:
                    UpdateLightningPhase(dt);
                    break;
                case StormPhase.ScreenFlicker:
                    UpdateFlicker(dt);
                    break;
                case StormPhase.DogsMarch:
                    UpdateDogsMarch(dt);
                    break;
            }

            // Check phase advance
            float phaseDur = GetPhaseDuration(CurrentPhase);
            if (phaseDur > 0f && _phaseTimer >= phaseDur)
            {
                float overflow = _phaseTimer - phaseDur;
                AdvancePhase(overflow);
            }
        }

        private float GetPhaseDuration(StormPhase phase)
        {
            return phase switch
            {
                StormPhase.CloudsGather => _settings.cloudsGatherDuration,
                StormPhase.LightningStrike => _settings.lightningStrikeDuration,
                StormPhase.ScreenFlicker => _settings.screenFlickerDuration,
                StormPhase.DogsMarch => _settings.dogsMarchDuration,
                _ => 0f
            };
        }

        private void AdvancePhase(float carryOver)
        {
            switch (CurrentPhase)
            {
                case StormPhase.CloudsGather:
                    CurrentPhase = StormPhase.LightningStrike;
                    _phaseTimer = carryOver;
                    _pendingBoltTimes.Clear();
                    if (_settings.stormBoltTimes != null)
                        _pendingBoltTimes.AddRange(_settings.stormBoltTimes);
                    break;
                case StormPhase.LightningStrike:
                    CurrentPhase = StormPhase.ScreenFlicker;
                    _phaseTimer = carryOver;
                    _flickerIndex = 0;
                    _flickerSubTimer = 0f;
                    _shake.Trigger(_settings.flickerShakeIntensity, _settings.flickerShakeDecay);
                    break;
                case StormPhase.ScreenFlicker:
                    CurrentPhase = StormPhase.DogsMarch;
                    _phaseTimer = carryOver;
                    _wind *= 0.5f;
                    if (_dog1Image != null) _dog1Image.enabled = true;
                    if (_dog2Image != null) _dog2Image.enabled = true;
                    break;
                case StormPhase.DogsMarch:
                    CurrentPhase = StormPhase.Complete;
                    IsComplete = true;
                    EventBus.Emit(GameEvent.IntroComplete);
                    break;
            }
        }

        // ── Sky ──
        private void UpdateSkyColor()
        {
            float totalDur = _settings.cloudsGatherDuration + _settings.lightningStrikeDuration
                + _settings.screenFlickerDuration + _settings.dogsMarchDuration;
            float t = Mathf.Clamp01(_globalTimer / totalDur * _settings.skyColorRampMultiplier);
            _skyImage.color = EasingUtils.LerpColor(_settings.clearSkyTop, _settings.stormSkyTop, t);
        }

        // ── Clouds ──
        private void BuildClouds(RectTransform container)
        {
            Sprite[] sprites = new[] { _cloudSprite1, _cloudSprite2 };

            BuildCloudLayer(container, sprites, 0,
                _settings.cloudLayer0Count, _settings.cloudLayer0ScaleMin,
                _settings.cloudLayer0ScaleMax, _settings.cloudLayer0SpeedMin,
                _settings.cloudLayer0SpeedMax);

            BuildCloudLayer(container, sprites, 1,
                _settings.cloudLayer1Count, _settings.cloudLayer1ScaleMin,
                _settings.cloudLayer1ScaleMax, _settings.cloudLayer1SpeedMin,
                _settings.cloudLayer1SpeedMax);

            BuildCloudLayer(container, sprites, 2,
                _settings.cloudLayer2Count, _settings.cloudLayer2ScaleMin,
                _settings.cloudLayer2ScaleMax, _settings.cloudLayer2SpeedMin,
                _settings.cloudLayer2SpeedMax);
        }

        private void BuildCloudLayer(RectTransform container, Sprite[] sprites, int layer,
            int count, float scaleMin, float scaleMax, float speedMin, float speedMax)
        {
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = sprites[i % sprites.Length];
                if (sprite == null) continue;

                float side = (i % 2 == 0) ? -1f : 1f;
                float startX = side * (_screenWidth + Random.Range(
                    _settings.cloudStartOffscreenMin, _settings.cloudStartOffscreenMax));
                float targetX = (_screenWidth / (count + 1)) * (i + 1)
                    - sprite.rect.width * _settings.cloudTargetSpriteWidthFactor
                    + Random.Range(-_settings.cloudTargetJitter, _settings.cloudTargetJitter);
                float y = Random.Range(_settings.cloudYRandomMin, _screenHeight * _settings.cloudYScreenFraction)
                    + layer * _screenHeight * _settings.cloudYLayerFraction;
                float speed = Random.Range(speedMin, speedMax);
                float scale = Random.Range(scaleMin, scaleMax);

                _clouds.Add(new StormCloud(container, sprite, startX, targetX, y, speed, layer, scale));
            }
        }

        private void UpdateCloudsGather(float dt)
        {
            float dur = _settings.cloudsGatherDuration;
            float t = Mathf.Clamp01(_phaseTimer / dur);

            foreach (var cloud in _clouds)
                cloud.UpdateGather(dt, t, _globalTimer, _settings);

            float eased = EasingUtils.EaseInOutCubic(t);
            _wind = Mathf.Lerp(0f, _settings.gatherWindTarget, eased);
            _rainIntensity = Mathf.Lerp(0f, _settings.gatherRainMax, eased);

            UpdateSkyColor();

            float cloudAlpha = Mathf.Lerp(_settings.cloudGatherAlphaMin, _settings.cloudGatherAlphaMax, t);
            foreach (var cloud in _clouds)
                cloud.SetAlpha(cloudAlpha);
        }

        // ── Rain ──
        private void UpdateRain(float dt)
        {
            if (_rainIntensity <= 0f) return;

            int spawnCount = Mathf.RoundToInt(_rainIntensity * _settings.rainSpawnMultiplier * dt);
            float pad = _settings.rainSpawnXPadding;
            float halfW = _screenWidth * 0.5f;
            float halfH = _screenHeight * 0.5f;
            for (int i = 0; i < spawnCount; i++)
            {
                // Particles use default center anchors (0.5, 0.5), so (0,0) = center of container
                float x = Random.Range(-halfW - pad, halfW + pad);
                float speed = Random.Range(_settings.rainSpeedMin, _settings.rainSpeedMax) * _rainIntensity;
                float windVx = _wind + Random.Range(-_settings.rainWindRandomRange, _settings.rainWindRandomRange);
                float length = Random.Range(_settings.rainLengthMin, _settings.rainLengthMax) * _rainIntensity;

                Color rainColor = _rainIntensity > 0.6f ? _settings.rainHeavyColor : _settings.rainColor;
                rainColor.a = Random.Range(0.3f, 0.7f);

                _rainPool.Emit(
                    new Vector2(x, halfH),
                    new Vector2(windVx, -speed),
                    _screenHeight / speed + 0.5f,
                    new Vector2(1f, length),
                    rainColor
                );
            }

            _rainPool.UpdateAll(dt);
        }

        // ── Lightning ──
        private void UpdateLightning(float dt)
        {
            _lightningFlash = Mathf.Max(0f, _lightningFlash - dt * _settings.lightningFlashDecay);
            _flashOverlay.color = new Color(
                _settings.flashOverlayColor.r,
                _settings.flashOverlayColor.g,
                _settings.flashOverlayColor.b,
                _lightningFlash * 0.7f
            );
        }

        private void UpdateLightningPhase(float dt)
        {
            // Spawn bolts at scheduled times
            for (int i = _pendingBoltTimes.Count - 1; i >= 0; i--)
            {
                if (_phaseTimer >= _pendingBoltTimes[i])
                {
                    SpawnBolt();
                    _pendingBoltTimes.RemoveAt(i);
                }
            }

            // Intensify rain
            _rainIntensity = Mathf.Min(1f, _rainIntensity + dt * _settings.lightningRainRampSpeed);
            _wind = Mathf.Lerp(_wind, _settings.lightningWindTarget, dt * _settings.lightningWindLerpSpeed);

            UpdateSkyColor();

            // Flash clouds during lightning
            float flashAlpha = 1f + _lightningFlash * _settings.lightningCloudFlashBoost;
            foreach (var cloud in _clouds)
                cloud.SetAlpha(Mathf.Min(1f, flashAlpha));
        }

        private void SpawnBolt()
        {
            float x = Random.Range(_screenWidth * _settings.boltXMinFraction,
                _screenWidth * _settings.boltXMaxFraction);
            float strikeY = _dogGroundY + _settings.boltStrikeYOffset;

            // Create procedural lightning bolt using UILineDrawer
            var boltGo = new GameObject("Bolt");
            var boltRect = boltGo.AddComponent<RectTransform>();
            boltRect.SetParent(_boltContainer, false);
            boltRect.anchorMin = new Vector2(0f, 1f);
            boltRect.anchorMax = new Vector2(0f, 1f);
            boltRect.pivot = new Vector2(0f, 1f);
            boltRect.anchoredPosition = Vector2.zero;
            boltRect.sizeDelta = new Vector2(_screenWidth, _screenHeight);

            boltGo.AddComponent<CanvasRenderer>();
            var drawer = boltGo.AddComponent<UILineDrawer>();
            drawer.raycastTarget = false;
            drawer.SetLineColor(_settings.boltCoreColor);
            drawer.SetWidth(_settings.boltLineWidthStart, _settings.boltLineWidthEnd);

            // Generate jagged bolt path
            var points = new List<Vector2>();
            int segments = Random.Range(_settings.boltSegmentsMin, _settings.boltSegmentsMax);
            float yStep = strikeY / segments;
            float cx = x;
            float jagRange = _settings.boltJagRange;
            points.Add(new Vector2(cx, 0f));
            for (int i = 1; i < segments; i++)
            {
                cx += Random.Range(-jagRange, jagRange);
                points.Add(new Vector2(cx, -i * yStep));
            }
            points.Add(new Vector2(cx + Random.Range(-jagRange * 0.33f, jagRange * 0.33f), -strikeY));
            drawer.SetPoints(points.ToArray());

            float duration = Random.Range(_settings.boltDurationMin, _settings.boltDurationMax);
            _activeBolts.Add(new LightningBolt
            {
                drawer = drawer,
                age = 0f,
                duration = duration
            });

            // Ground bloom
            var bloomGo = new GameObject("Bloom");
            var bloomRect = bloomGo.AddComponent<RectTransform>();
            bloomRect.SetParent(_bloomContainer, false);
            bloomRect.anchorMin = new Vector2(0f, 1f);
            bloomRect.anchorMax = new Vector2(0f, 1f);
            bloomRect.pivot = new Vector2(0.5f, 0.5f);
            bloomRect.anchoredPosition = new Vector2(cx, -strikeY);
            bloomRect.sizeDelta = new Vector2(200f, 200f);
            bloomGo.AddComponent<CanvasRenderer>();
            var bloomDrawer = bloomGo.AddComponent<UICircleDrawer>();
            bloomDrawer.raycastTarget = false;
            _groundBlooms.Add(new GroundBloom
            {
                drawer = bloomDrawer,
                life = 0f,
                duration = _settings.bloomDuration,
                maxRadius = Random.Range(50f, _settings.bloomMaxRadius),
                center = Vector2.zero
            });

            _shake.Trigger(_settings.shakeIntensity, _settings.shakeDecay);
            _lightningFlash = 1f;

            EventBus.Emit(GameEvent.PlaySound, new Dictionary<string, object>
            {
                { "sound", "thunder" }
            });
        }

        private void UpdateBolts(float dt)
        {
            for (int i = _activeBolts.Count - 1; i >= 0; i--)
            {
                var bolt = _activeBolts[i];
                bolt.age += dt;

                float t = bolt.age / bolt.duration;
                float alpha;
                if (t < _settings.boltFadeInEnd)
                    alpha = t / _settings.boltFadeInEnd;
                else if (t < _settings.boltFullEnd)
                    alpha = 1f;
                else
                    alpha = Mathf.Max(0f, 1f - (t - _settings.boltFullEnd) / (1f - _settings.boltFullEnd));

                if (alpha <= 0f || bolt.age >= bolt.duration)
                {
                    if (bolt.drawer != null)
                        Destroy(bolt.drawer.gameObject);
                    _activeBolts.RemoveAt(i);
                }
                else
                {
                    var c = _settings.boltCoreColor;
                    c.a = alpha;
                    bolt.drawer.SetLineColor(c);
                    _activeBolts[i] = bolt;
                }
            }
        }

        private void UpdateBlooms(float dt)
        {
            for (int i = _groundBlooms.Count - 1; i >= 0; i--)
            {
                var bloom = _groundBlooms[i];
                bloom.life += dt;
                float t = Mathf.Clamp01(bloom.life / bloom.duration);

                if (t >= 1f)
                {
                    if (bloom.drawer != null)
                        Destroy(bloom.drawer.gameObject);
                    _groundBlooms.RemoveAt(i);
                }
                else
                {
                    float radius = bloom.maxRadius * EasingUtils.EaseOutQuad(t);
                    float alpha = 1f - EasingUtils.EaseInQuad(t);
                    bloom.drawer.Clear();
                    var outer = _settings.boltOuterGlowColor;
                    outer.a = alpha * _settings.bloomOuterAlpha;
                    bloom.drawer.AddFilledCircle(bloom.center, radius, outer);
                    var inner = _settings.boltInnerGlowColor;
                    inner.a = alpha * _settings.bloomInnerAlpha;
                    bloom.drawer.AddFilledCircle(bloom.center, radius * _settings.bloomInnerRadiusFraction, inner);
                    var core = _settings.boltCoreColor;
                    core.a = alpha;
                    bloom.drawer.AddFilledCircle(bloom.center,
                        Mathf.Max(_settings.bloomCoreMinRadius, radius * _settings.bloomCoreRadiusFraction), core);
                    bloom.drawer.Rebuild();
                    _groundBlooms[i] = bloom;
                }
            }
        }

        // ── Flicker ──
        private void UpdateFlicker(float dt)
        {
            _flickerSubTimer += dt;
            if (_settings.flickerPattern != null && _flickerIndex < _settings.flickerPattern.Length)
            {
                var entry = _settings.flickerPattern[_flickerIndex];
                _flickerFlash = entry.intensity;
                float tempShift = entry.tempShift;

                float rShift = tempShift * 40f / 255f;
                float r = Mathf.Clamp01(1f + rShift);
                float b = Mathf.Clamp01(1f - rShift);
                _flickerOverlay.color = new Color(r, 1f, b, _flickerFlash * 0.63f);

                if (_flickerSubTimer >= entry.duration)
                {
                    _flickerSubTimer -= entry.duration;
                    _flickerIndex++;
                }
            }
            else
            {
                _flickerOverlay.color = new Color(1f, 1f, 1f, 0f);
            }

            // Keep rain at peak
            _rainIntensity = Mathf.Max(_rainIntensity, _settings.flickerRainIntensity);
        }

        // ── Dogs March ──
        private void UpdateDogsMarch(float dt)
        {
            float dur = _settings.dogsMarchDuration;
            float t = Mathf.Clamp01(_phaseTimer / dur);
            float eased = EasingUtils.EaseInOutCubic(t);

            // Move dogs
            _dog1CurrentX = Mathf.Lerp(_dog1StartX, _dog1TargetX, eased);
            if (_hasDog2)
                _dog2CurrentX = Mathf.Lerp(_dog2StartX, _dog2TargetX, eased);

            _marchBobTimer += dt;
            float frameRate = _settings.dogMarchFrameRate;

            // Update dog sprites (animation)
            if (_dog1RunSprites != null && _dog1RunSprites.Length > 0)
            {
                int frame = Mathf.FloorToInt(_marchBobTimer * frameRate) % _dog1RunSprites.Length;
                _dog1Image.sprite = _dog1RunSprites[frame];
            }
            if (_hasDog2 && _dog2RunSprites != null && _dog2RunSprites.Length > 0)
            {
                int frame = Mathf.FloorToInt(_marchBobTimer * frameRate + _settings.dogMarchFrameOffset)
                    % _dog2RunSprites.Length;
                _dog2Image.sprite = _dog2RunSprites[frame];
            }

            // Bob and tilt
            float bob1 = Mathf.Sin(_marchBobTimer * _settings.dogBobFrequency) * _settings.dogBobAmplitude;
            float tilt1 = Mathf.Sin(_marchBobTimer * _settings.dogBobFrequency + Mathf.PI * 0.5f)
                * _settings.dogTiltAmplitude;

            _dog1Image.rectTransform.anchoredPosition = new Vector2(_dog1CurrentX, -(_dogGroundY - _dog1Size + bob1));
            _dog1Image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt1);

            // Dog2 faces left — flip via scale
            if (_hasDog2 && _dog2Image != null)
            {
                float bob2 = Mathf.Sin(_marchBobTimer * _settings.dogBobFrequency + 1f) * _settings.dogBobAmplitude;
                float tilt2 = Mathf.Sin(_marchBobTimer * _settings.dogBobFrequency + 1f + Mathf.PI * 0.5f)
                    * _settings.dogTiltAmplitude;
                _dog2Image.rectTransform.anchoredPosition = new Vector2(_dog2CurrentX, -(_dogGroundY - _dog2Size + bob2));
                _dog2Image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt2);
                _dog2Image.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            // Dust puffs
            if (Mathf.Abs(_dog1CurrentX - _lastStepX1) >= _settings.dustStepDistance && t < 0.85f)
            {
                EmitDust(_dog1CurrentX + 20f, _dogGroundY + 10f);
                _lastStepX1 = _dog1CurrentX;
            }
            if (_hasDog2 && Mathf.Abs(_dog2CurrentX - _lastStepX2) >= _settings.dustStepDistance && t < 0.85f)
            {
                EmitDust(_dog2CurrentX + 40f, _dogGroundY + 10f);
                _lastStepX2 = _dog2CurrentX;
            }
            _dustPool.UpdateAll(dt);

            // Title animation
            UpdateTitle(t);

            // GO animation
            _goAlpha = t > _settings.goAppearT
                ? Mathf.Clamp01((t - _settings.goAppearT) / (1f - _settings.goAppearT))
                : 0f;
            if (_goImage != null)
            {
                _goImage.enabled = _goAlpha > 0f;
                var gc = _goImage.color;
                gc.a = _goAlpha;
                _goImage.color = gc;
            }

            // Calm rain
            _rainIntensity = Mathf.Max(_settings.marchRainMin, _rainIntensity - dt * _settings.marchRainDecaySpeed);
        }

        private void UpdateTitle(float t)
        {
            if (_titleImage == null) return;

            if (t > _settings.titleAppearT && t < _settings.titleFullT)
            {
                float titleT = (t - _settings.titleAppearT) / (_settings.titleFullT - _settings.titleAppearT);
                _titleScale = 0.3f + 0.7f * EasingUtils.EaseOutQuad(Mathf.Min(titleT * 2f, 1f));
                _titleAlpha = Mathf.Min(titleT * 3f, 1f);
            }
            else if (t >= _settings.titleFullT && t < _settings.titleFadeEndT)
            {
                float fadeT = (t - _settings.titleFullT) / (_settings.titleFadeEndT - _settings.titleFullT);
                _titleAlpha = Mathf.Max(0f, 1f - fadeT);
                _titleScale = 1f;
            }
            else if (t >= _settings.titleFadeEndT)
            {
                _titleAlpha = 0f;
            }

            _titleImage.enabled = _titleAlpha > 0.01f;
            if (_titleImage.enabled)
            {
                var c = _titleImage.color;
                c.a = _titleAlpha;
                _titleImage.color = c;
                _titleRect.localScale = Vector3.one * _titleScale;
            }
        }

        private void EmitDust(float x, float y)
        {
            var color = _settings.dustPuffColor;
            color.a = Random.Range(0.5f, 0.8f);
            _dustPool.Emit(
                new Vector2(
                    x + Random.Range(-_settings.dustEmitXRandomRange, _settings.dustEmitXRandomRange),
                    -(y + Random.Range(_settings.dustEmitYRandomMin, _settings.dustEmitYRandomMax))),
                new Vector2(
                    Random.Range(-_settings.dustVxRange, _settings.dustVxRange),
                    Random.Range(_settings.dustVyMin, _settings.dustVyMax)),
                Random.Range(0.3f, _settings.dustPuffLifetime),
                Random.Range(_settings.dustSizeMin, _settings.dustSizeMax),
                color,
                growthRate: Random.Range(_settings.dustGrowthMin, _settings.dustGrowthMax)
            );
        }

        // ── Cleanup ──
        private void OnDestroy()
        {
            foreach (var cloud in _clouds)
                cloud.Destroy();
            _clouds.Clear();

            foreach (var bolt in _activeBolts)
            {
                if (bolt.drawer != null)
                    Destroy(bolt.drawer.gameObject);
            }
            _activeBolts.Clear();

            foreach (var bloom in _groundBlooms)
            {
                if (bloom.drawer != null)
                    Destroy(bloom.drawer.gameObject);
            }
            _groundBlooms.Clear();

            _rainPool?.DestroyAll();
            _dustPool?.DestroyAll();
        }

        // ── Helpers ──
        private static void SetStretchAll(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
