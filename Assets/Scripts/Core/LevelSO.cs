using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "Level", menuName = "SnackAttack/Level")]
    public class LevelSO : ScriptableObject
    {
        [Header("Identity")]
        public int levelNumber;
        public string levelName;
        public Color backgroundColor = Color.white;

        [Header("Gameplay")]
        public float roundDurationSeconds = 60f;
        public float spawnRateMultiplier = 1f;

        [Header("Visuals")]
        public Sprite battlefieldSprite;

        [Header("Content")]
        public SnackSO[] snackPool;
        public ObstacleDefinition[] obstacles;
    }
}
