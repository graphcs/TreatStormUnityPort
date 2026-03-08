using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "AIDifficulty", menuName = "SnackAttack/AI Difficulty")]
    public class AIDifficultySO : ScriptableObject
    {
        public string difficultyName;
        public float reactionDelayMs = 250f;
        public float decisionAccuracy = 0.8f;
        public float pathfindingEfficiency = 0.7f;
        public bool avoidsPenalties = true;
        public bool targetsPowerups = true;

        [Header("Scoring Weights")]
        public float distanceWeight = 0.5f;
        public float verticalReachThreshold = 50f;
        public float unreachablePenalty = 200f;
        public float reachableBonus = 100f;
        public float penaltyAvoidanceWeight = 300f;
        public float boostBonus = 100f;
        public float invincibilityBonus = 150f;
        public float speedBonus = 100f;

        [Header("Navigation")]
        public float reachThreshold = 5f;
        public float wanderPadding = 20f;
        public float wanderPaddingVertical = 10f;
    }
}
