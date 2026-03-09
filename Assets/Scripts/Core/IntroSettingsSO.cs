using System;
using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "IntroSettings", menuName = "SnackAttack/IntroSettings")]
    public class IntroSettingsSO : ScriptableObject
    {
        // ── Storm Intro Phase Durations ──
        [Header("Storm Intro — Phase Durations")]
        public float cloudsGatherDuration = 4.0f;
        public float lightningStrikeDuration = 1.6f;
        public float screenFlickerDuration = 0.9f;
        public float dogsMarchDuration = 2.2f;

        // ── Menu Intro Phase Durations ──
        [Header("Menu Intro — Phase Durations")]
        public float mmCloudRollInDuration = 1.85f;
        public float mmLightningDuration = 1.15f;
        public float mmDogsMarchDuration = 1.7f;

        // ── Sky Colors ──
        [Header("Sky Colors")]
        public Color clearSkyTop = new Color(0.529f, 0.808f, 0.922f, 1f);    // (135,206,235)
        public Color clearSkyBottom = new Color(0.392f, 0.647f, 0.824f, 1f); // (100,165,210)
        public Color stormSkyTop = new Color(0.086f, 0.086f, 0.149f, 1f);    // (22,22,38)
        public Color stormSkyBottom = new Color(0.047f, 0.047f, 0.086f, 1f); // (12,12,22)

        // ── Clouds ──
        [Header("Clouds")]
        public int cloudLayer0Count = 4;
        public float cloudLayer0ScaleMin = 0.5f;
        public float cloudLayer0ScaleMax = 0.7f;
        public float cloudLayer0SpeedMin = 100f;
        public float cloudLayer0SpeedMax = 170f;

        public int cloudLayer1Count = 5;
        public float cloudLayer1ScaleMin = 0.4f;
        public float cloudLayer1ScaleMax = 0.6f;
        public float cloudLayer1SpeedMin = 150f;
        public float cloudLayer1SpeedMax = 260f;

        public int cloudLayer2Count = 5;
        public float cloudLayer2ScaleMin = 0.3f;
        public float cloudLayer2ScaleMax = 0.5f;
        public float cloudLayer2SpeedMin = 200f;
        public float cloudLayer2SpeedMax = 340f;

        // ── Rain ──
        [Header("Rain")]
        public int maxRainDrops = 600;
        public float rainSpawnMultiplier = 200f;
        public float rainSpeedMin = 400f;
        public float rainSpeedMax = 700f;
        public float rainWindMin = -60f;
        public float rainWindMax = -100f;
        public float rainLengthMin = 6f;
        public float rainLengthMax = 16f;
        public Color rainColor = new Color(0.627f, 0.686f, 0.784f, 0.6f);
        public Color rainHeavyColor = new Color(0.510f, 0.588f, 0.706f, 0.7f);

        // ── Lightning ──
        [Header("Lightning")]
        public float[] stormBoltTimes = { 0f, 0.45f, 0.95f };
        public float[] menuBoltTimes = { 0.04f, 0.21f, 0.46f, 0.82f };
        public float boltDurationMin = 0.5f;
        public float boltDurationMax = 0.7f;
        public float lightningFlashDecay = 4.0f;
        public float shakeIntensity = 12f;
        public float shakeDecay = 7f;
        public Color boltCoreColor = new Color(0.902f, 0.902f, 1f, 1f);
        public Color boltInnerGlowColor = new Color(0.706f, 0.745f, 1f, 1f);
        public Color boltOuterGlowColor = new Color(0.471f, 0.510f, 0.863f, 1f);
        public Color flashOverlayColor = new Color(0.784f, 0.824f, 1f, 0.7f);

        // ── Flicker ──
        [Header("Screen Flicker")]
        public FlickerEntry[] flickerPattern = new FlickerEntry[]
        {
            new FlickerEntry(0.06f, 0.9f, 0f),
            new FlickerEntry(0.04f, 0f, 0f),
            new FlickerEntry(0.08f, 1f, 0.1f),
            new FlickerEntry(0.05f, 0f, 0f),
            new FlickerEntry(0.04f, 0.6f, -0.05f),
            new FlickerEntry(0.06f, 0f, 0f),
            new FlickerEntry(0.10f, 0.8f, 0.05f),
            new FlickerEntry(0.03f, 0.3f, 0f),
            new FlickerEntry(0.07f, 0f, 0f),
            new FlickerEntry(0.12f, 0.5f, -0.1f),
            new FlickerEntry(0.25f, 0f, 0f),
        };
        public float flickerShakeIntensity = 6f;
        public float flickerShakeDecay = 5f;

        // ── Dogs March ──
        [Header("Dogs March")]
        public float dogRenderScale = 1.12f;
        public float dogBobFrequency = 14f;
        public float dogBobAmplitude = 3.5f;
        public float dogTiltAmplitude = 1.5f;
        public float dustStepDistance = 28f;
        public float dustPuffMaxRadius = 16f;
        public float dustPuffLifetime = 0.5f;
        public Color dustPuffColor = new Color(0.627f, 0.549f, 0.431f, 0.7f);

        // ── Title / GO ──
        [Header("Title & GO")]
        public float titleAppearT = 0.2f;
        public float titleFullT = 0.65f;
        public float titleFadeEndT = 0.8f;
        public float titleWidthFraction = 0.65f;
        public float goAppearT = 0.82f;
        public float goWidthFraction = 0.3f;

        // ── Ground ──
        [Header("Ground")]
        public float groundYFraction = 0.91f;

        // ── Ground Bloom ──
        [Header("Ground Bloom")]
        public float bloomMaxRadius = 80f;
        public float bloomDuration = 0.5f;

        // ── Menu Intro Dogs ──
        [Header("Menu Intro — Dogs")]
        public string mmLeftDogId = "jazzy";
        public string mmRightDogId = "snowy";
        public float mmDogSize = 140f;
        public float mmDogBobFrequency = 12f;
        public float mmDogBobAmplitude = 4f;

        // ── Menu Intro — Cloud Band ──
        [Header("Menu Intro — Cloud Band")]
        public float mmBandPaddingX = 0.08f;
        public float mmBandTopOffset = 28f;
        public float mmBandHeightFraction = 0.44f;

        // ── Menu Intro — Logo Glow ──
        [Header("Menu Intro — Logo Glow")]
        public Color mmLogoGlowColor = new Color(0.722f, 0.804f, 1f, 0.53f);

        // ── Shared Sprite References ──
        [Header("Sprite References")]
        public Sprite cloudSprite1;
        public Sprite cloudSprite2;
        public Sprite titleSprite;
        public Sprite goSprite;
        public Sprite groundSprite;

        // ── Menu Cloud Layers (reuse same sprites, different configs) ──
        [Header("Menu Clouds")]
        public int mmCloudLayer0Count = 4;
        public float mmCloudLayer0ScaleMin = 0.68f;
        public float mmCloudLayer0ScaleMax = 0.82f;
        public float mmCloudLayer0SwaySpeed = 0.68f;

        public int mmCloudLayer1Count = 5;
        public float mmCloudLayer1ScaleMin = 0.52f;
        public float mmCloudLayer1ScaleMax = 1.04f;
        public float mmCloudLayer1SwaySpeed = 1.04f;

        public int mmCloudLayer2Count = 6;
        public float mmCloudLayer2ScaleMin = 0.38f;
        public float mmCloudLayer2ScaleMax = 1.26f;
        public float mmCloudLayer2SwaySpeed = 1.26f;

        // ═══════════════════════════════════════════
        //  Storm Intro — Cloud Layout
        // ═══════════════════════════════════════════
        [Header("Storm — Cloud Layout")]
        public float cloudStartOffscreenMin = 100f;
        public float cloudStartOffscreenMax = 300f;
        public float cloudTargetSpriteWidthFactor = 0.3f;
        public float cloudTargetJitter = 40f;
        public float cloudYRandomMin = -20f;
        public float cloudYScreenFraction = 0.08f;
        public float cloudYLayerFraction = 0.04f;
        public float cloudGatherAlphaMin = 0.63f;
        public float cloudGatherAlphaMax = 1f;
        public float cloudBobSpeed = 0.7f;
        public float cloudBobAmplitude = 3f;
        public float cloudApproachThreshold = 2f;
        public float cloudApproachLerpSpeed = 2f;
        public float cloudSpeedMultHigh = 2.5f;
        public float cloudSpeedMultLow = 0.5f;
        public float cloudSpeedMultTransition = 0.4f;

        // ═══════════════════════════════════════════
        //  Storm Intro — Rain & Wind Phases
        // ═══════════════════════════════════════════
        [Header("Storm — Rain & Wind Phases")]
        public float gatherWindTarget = -60f;
        public float gatherRainMax = 0.4f;
        public float lightningRainRampSpeed = 0.5f;
        public float lightningWindTarget = -100f;
        public float lightningWindLerpSpeed = 2f;
        public float lightningCloudFlashBoost = 0.3f;
        public float flickerRainIntensity = 0.8f;
        public float marchRainMin = 0.2f;
        public float marchRainDecaySpeed = 0.3f;
        public float rainSpawnXPadding = 50f;
        public float rainWindRandomRange = 20f;

        // ═══════════════════════════════════════════
        //  Storm Intro — Bolt Visuals
        // ═══════════════════════════════════════════
        [Header("Storm — Bolt Visuals")]
        public float boltXMinFraction = 0.15f;
        public float boltXMaxFraction = 0.85f;
        public float boltStrikeYOffset = 50f;
        public int boltSegmentsMin = 8;
        public int boltSegmentsMax = 14;
        public float boltJagRange = 30f;
        public float boltLineWidthStart = 4f;
        public float boltLineWidthEnd = 2f;
        public float boltFadeInEnd = 0.1f;
        public float boltFullEnd = 0.35f;

        // ═══════════════════════════════════════════
        //  Storm Intro — Bloom Detail
        // ═══════════════════════════════════════════
        [Header("Storm — Bloom Detail")]
        public float bloomOuterAlpha = 0.33f;
        public float bloomInnerAlpha = 0.5f;
        public float bloomInnerRadiusFraction = 0.5f;
        public float bloomCoreRadiusFraction = 0.15f;
        public float bloomCoreMinRadius = 2f;

        // ═══════════════════════════════════════════
        //  Storm Intro — Dog March Detail
        // ═══════════════════════════════════════════
        [Header("Storm — Dog March Detail")]
        public float dogStartOffscreenDistance = 200f;
        public float dogMarchFrameRate = 10f;
        public float dogMarchFrameOffset = 0.5f;

        // ═══════════════════════════════════════════
        //  Storm Intro — Title/GO Position
        // ═══════════════════════════════════════════
        [Header("Storm — Title/GO Position")]
        public float titleYOffset = 60f;
        public float goYOffset = 20f;

        // ═══════════════════════════════════════════
        //  Storm Intro — Sky Color
        // ═══════════════════════════════════════════
        [Header("Storm — Sky Color")]
        public float skyColorRampMultiplier = 1.25f;

        // ═══════════════════════════════════════════
        //  Storm Intro — Dust Emission
        // ═══════════════════════════════════════════
        [Header("Storm — Dust Emission")]
        public float dustEmitXRandomRange = 6f;
        public float dustEmitYRandomMin = -2f;
        public float dustEmitYRandomMax = 4f;
        public float dustVxRange = 5f;
        public float dustVyMin = 10f;
        public float dustVyMax = 20f;
        public float dustSizeMin = 4f;
        public float dustSizeMax = 8f;
        public float dustGrowthMin = 15f;
        public float dustGrowthMax = 30f;

        // ═══════════════════════════════════════════
        //  Menu Intro — Band Colors
        // ═══════════════════════════════════════════
        [Header("Menu Intro — Band Colors")]
        public Color mmBandInitialColor = new Color(0.486f, 0.639f, 0.824f, 0.05f);
        public Color mmBandStormColor = new Color(0.086f, 0.114f, 0.173f, 0.78f);
        public Color mmFlashOverlayColor = new Color(0.84f, 0.89f, 1f, 0f);
        public float mmDogGroundYPadding = 34f;

        // ═══════════════════════════════════════════
        //  Menu Intro — Cloud Layout
        // ═══════════════════════════════════════════
        [Header("Menu Intro — Cloud Layout")]
        public float mmCloudStartOffscreenMin = 120f;
        public float mmCloudStartOffscreenMax = 280f;
        public float mmCloudTargetWidthFactor = 0.33f;
        public float mmCloudTargetJitter = 30f;
        public float mmCloudYRandomMax = 0.2f;
        public float mmCloudYLayerFactor = 0.06f;
        public float mmCloudSwaySpeed = 0.7f;
        public float mmCloudSwayAmount = 10f;
        public float mmCloudSwayLayerMult = 0.25f;
        public float mmCloudGatherAlphaMin = 0.38f;
        public float mmCloudGatherAlphaMax = 0.96f;
        public float mmLogoGlowGatherMax = 0.25f;

        // ═══════════════════════════════════════════
        //  Menu Intro — Lightning Detail
        // ═══════════════════════════════════════════
        [Header("Menu Intro — Lightning Detail")]
        public float mmLightningFlashDecay = 4.8f;
        public float mmFlashAlphaMultiplier = 0.43f;
        public float mmBoltXMinFraction = 0.14f;
        public float mmBoltXMaxFraction = 0.86f;
        public float mmBoltYMinFraction = 0.03f;
        public float mmBoltYMaxFraction = 0.08f;
        public float mmBoltHeightMinFraction = 0.72f;
        public float mmBoltHeightMaxFraction = 0.95f;
        public float mmBoltWidth = 4f;
        public Color mmBoltColor = new Color(0.902f, 0.922f, 1f, 1f);
        public float mmBoltDurationMin = 0.25f;
        public float mmBoltDurationMax = 0.38f;
        public float mmBoltFadeInEnd = 0.22f;
        public float mmBoltFullEnd = 0.56f;

        // ═══════════════════════════════════════════
        //  Menu Intro — Logo Glow Detail
        // ═══════════════════════════════════════════
        [Header("Menu Intro — Logo Glow Detail")]
        public float mmLogoGlowDecay = 0.65f;
        public float mmLogoGlowThreshold = 0.04f;
        public float mmLogoGlowAlphaMultiplier = 0.53f;
        public float mmLogoGlowInnerRatio = 0.7f;
        public float mmLogoGlowInnerAlphaFactor = 0.5f;
        public Color mmLogoGlowInnerColor = new Color(0.922f, 0.941f, 1f, 1f);
        public float mmLogoGlowPaddingX = 90f;
        public float mmLogoGlowPaddingY = 70f;
        public float mmBoltFlashGlow = 0.92f;

        // ═══════════════════════════════════════════
        //  Menu Intro — Dog March Detail
        // ═══════════════════════════════════════════
        [Header("Menu Intro — Dog March Detail")]
        public float mmDogStartOffscreenPadding = 60f;
        public float mmDogLeftTargetFactor = 0.35f;
        public float mmDogRightTargetFactor = 0.62f;
        public float mmDogMarchLerpBase = 4.2f;
        public float mmDogMarchLerpEase = 0.08f;
        public float mmDogStrideAccel = 0.75f;
        public float mmDogFrameRate = 10f;
        public float mmDogLogoGlowMin = 0.16f;
        public float mmDogLogoGlowMax = 0.36f;
        public float mmDogDustDistance = 26f;
        public float mmDogDustOffsetLeftX = 32f;
        public float mmDogDustOffsetRightX = 44f;
        public float mmDogDustOffsetY = 2f;

        // ═══════════════════════════════════════════
        //  Menu Intro — Dust Emission
        // ═══════════════════════════════════════════
        [Header("Menu Intro — Dust")]
        public Color mmDustColor = new Color(0.502f, 0.463f, 0.439f, 0.59f);
        public float mmDustVxRange = 3f;
        public float mmDustVyMin = 10f;
        public float mmDustVyMax = 20f;
        public float mmDustLifetime = 0.42f;
        public float mmDustSize = 4f;
        public float mmDustGrowthRate = 25f;
        public int mmDustPoolSize = 60;

        // ═══════════════════════════════════════════
        //  StormCloud — Movement Tuning
        // ═══════════════════════════════════════════
        [Header("StormCloud — Movement")]
        public float menuCloudTravelSpeedBase = 1.15f;
        public float menuCloudTravelSpeedLayerMult = 0.1f;
    }

    [Serializable]
    public struct FlickerEntry
    {
        public float duration;
        public float intensity;
        public float tempShift;

        public FlickerEntry(float duration, float intensity, float tempShift)
        {
            this.duration = duration;
            this.intensity = intensity;
            this.tempShift = tempShift;
        }
    }
}
