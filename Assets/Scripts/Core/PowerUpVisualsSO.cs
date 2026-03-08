using System;
using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "PowerUpVisuals", menuName = "SnackAttack/Power Up Visuals")]
    public class PowerUpVisualsSO : ScriptableObject
    {
        [Header("Shared")]
        public Sprite whiteSprite;

        public WingsParams wings;
        public SpeedStreakParams speedStreak;
        public AuraParams aura;
        public StatusIndicatorParams statusIndicator;
        public SnackGlowParams snackGlow;
        public PickupFlashParams pickupFlash;
        public SteamParams steam;
        public TintParams tint;

        [Serializable]
        public struct WingsParams
        {
            public bool enabled;
            public Color color;
            public Color glowColor;
            public float opacity;
            public float flapSpeed;
            public float flapAmplitude;
            public float wingWidth;
            public float wingHeight;
            public int featherCount;
            public bool trailParticles;
            public int trailParticleRate;
            public float trailParticleLifetime;
            public Sprite wingUpSprite;
            public Sprite wingDownSprite;
            public float shoulderOffsetYFraction;
            public float shoulderOffsetXFraction;
            public Color flightGlowOuter;
            public Color flightGlowInner;
            public float flightGlowPulseSpeed;
            public float flightGlowRadiusFraction;
        }

        [Serializable]
        public struct SpeedStreakParams
        {
            public bool enabled;
            public int afterimageCount;
            public float afterimageSpacing;
            public float afterimageBaseAlpha;
            public Color streakColorBoost;
            public Color streakColorSpeed;
            public Vector2 streakWidthRange;
            public int streakRate;
            public float streakLifetime;
            public int particleRate;
            public float particleLifetime;
            public Vector2 particleSizeRange;
        }

        [Serializable]
        public struct AuraParams
        {
            public bool enabled;
            public float pulseSpeed;
            public float baseRadiusPadding;
            public float pulseAmplitude;
            public float baseAlpha;
            public float ringWidth;
            public Color colorBoost;
            public Color colorSpeedBoost;
            public Color colorInvincibility;
            public Color colorChaos;
            public Color colorSlow;
            public int sparkleCount;
            public float sparkleSpeed;
            public float sparkleSize;
        }

        [Serializable]
        public struct StatusIndicatorParams
        {
            public bool enabled;
            public float barWidth;
            public float barHeight;
            public float barOffsetY;
            public float iconSize;
            public float iconOffsetY;
            public float iconBobSpeed;
            public float iconBobAmplitude;
            public float stackingSpacing;
            public Color barBackgroundColor;
        }

        [Serializable]
        public struct SnackGlowParams
        {
            public bool enabled;
            public string[] powerupSnackIds;
            public float glowRadiusPadding;
            public float glowPulseSpeed;
            public float glowBaseAlpha;
            public float glowPulseAlpha;
            public int sparkleCount;
            public float sparkleOrbitSpeed;
            public float sparkleOrbitRadius;
            public float sparkleSize;
            public bool beamEnabled;
            public float beamWidth;
            public float beamHeight;
            public float beamAlpha;
        }

        [Serializable]
        public struct PickupFlashParams
        {
            public bool enabled;
            public float duration;
            public float maxAlpha;
            public float ringExpandSpeed;
            public float ringMaxRadius;
        }

        [Serializable]
        public struct SteamParams
        {
            public bool enabled;
            public int emitRate;
            public float lifetime;
            public Vector2 sizeRange;
            public float growthRate;
            public float horizontalSpread;
            public float verticalOffset;
            public Vector2 velocityYRange;
            public float velocityXRange;
        }

        [Serializable]
        public struct TintParams
        {
            public bool enabled;
            public Color invincibilityTint;
            public float invincibilityFlashRate;
            public Color chaosTint;
            public Color slowTint;
            public Color boostTint;
        }
    }
}
