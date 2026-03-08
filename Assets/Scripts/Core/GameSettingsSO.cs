using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "SnackAttack/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        [Header("Window")]
        public int windowWidth = 1200;
        public int windowHeight = 1000;

        [Header("UI Reference Resolution (Canvas Scaler)")]
        public int referenceWidth = 1200;
        public int referenceHeight = 1000;

        [Header("Gameplay")]
        public int targetFPS = 60;
        public float roundDuration = 90f;
        public int roundsPerGame = 1;

        [Header("Arena")]
        public float arenaWidth = 515f;
        public float arenaHeight = 860f;
        public float splitScreenGap = 30f;

        [Header("Movement")]
        public float baseMoveSpeed = 350f;
        public float playerGroundYOffset = 230f;

        [Header("Flight")]
        public float flightHeightFraction = 0.35f;
        public float flightCeilingMargin = 30f;
        public float hoverFrequency = 3f;
        public float hoverAmplitude = 6f;
        public float flightLiftTarget = -15f;
        public float flightSpringForce = 5f;
        public float tiltVelocityThreshold = 50f;
        public float tiltAngle = 8f;
        public float lateralLeanMultiplier = 4f;
        public float leanToTiltRatio = 0.3f;
        public float tiltSmoothingRate = 8f;
        public float hoverDecayRate = 6f;
        public float liftDecayRate = 6f;
        public float tiltDecayRate = 8f;
        public float dampenRate = 6f;

        [Header("Leash")]
        public float leashEffectDuration = 8f;
        public float leashExtendFraction = 0.15f;
        public float leashYankFraction = 0.35f;
        public float leashAnchorPadding = 15f;
        public float leashAnchorYOffset = 100f;

        [Header("Scoring")]
        public float stolenBonusMultiplier = 1.5f;
        public float playerHitboxShrink = 40f;

        [Header("Spawning")]
        public float groundYOffset = 16f;
        public float snackSpawnYOffset = 60f;
        public float snackSpawnPadding = 20f;
        public float snackSpawnVariance = 120f;
        public float votedFoodDuration = 5f;
        public float votedFoodSpawnInterval = 0.3f;

        [Header("Lightning")]
        public float lightningDuration = 0.08f;
        public int lightningSegments = 6;
        public float lightningJagRange = 30f;
        public float lightningLineWidth = 5f;
        public float lightningColorChangeProb = 0.3f;

        [Header("Round Flow")]
        public int countdownStart = 3;
        public float countdownTickDuration = 1.0f;
        public float goDisplayDuration = 0.5f;
        public float crowdChaosThreshold = 35f;
        public float crowdChaosCountdown = 5f;

        [Header("Animation")]
        public float runFrameDuration = 0.1f;
        public float eatFrameDuration = 0.12f;
        public float eatAnimDuration = 0.4f;
        public float faceCameraFrameDuration = 0.1f;

        [Header("HUD")]
        public float popupDuration = 1.0f;
        public float popupFloatSpeed = 50f;
        public int popupFontSize = 24;

        [Header("Background")]
        public float cloudSpeed = 8f;
        public float cloud2SpeedMultiplier = 0.7f;
    }
}
