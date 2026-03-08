using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "CelebrationSettings", menuName = "SnackAttack/Celebration Settings")]
    public class CelebrationSettingsSO : ScriptableObject
    {
        [Header("Balloons")]
        public int balloonCount = 12;
        public int radiusMin = 18;
        public int radiusMax = 29;
        public float speedMin = 40f;
        public float speedMax = 75f;
        public float swayAmpMin = 15f;
        public float swayAmpMax = 35f;
        public float swayFreqMin = 0.8f;
        public float swayFreqMax = 1.6f;
        public float spawnXMin = 30f;
        public float spawnXMax = 1170f;
        public float spawnYMin = -950f;
        public float spawnYMax = -300f;
        public float respawnThresholdY = 50f;
        public float respawnFloorY = -1050f;
        public float ovalHeightRatio = 2.4f;

        [Header("Confetti")]
        public float confettiSpawnRate = 18f;
        public int maxConfetti = 200;
        public float confettiSpeedMin = 60f;
        public float confettiSpeedMax = 130f;
        public float confettiDriftMin = -30f;
        public float confettiDriftMax = 30f;
        public float confettiRotSpeedMin = 120f;
        public float confettiRotSpeedMax = 360f;
        public float confettiWidthMin = 4f;
        public float confettiWidthMax = 9f;
        public float confettiHeightMin = 8f;
        public float confettiHeightMax = 16f;
        public float confettiRemovalY = -1050f;
        public float confettiSpawnYMin = 5f;
        public float confettiSpawnYMax = 20f;
    }
}
