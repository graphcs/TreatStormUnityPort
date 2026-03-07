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
    }
}
