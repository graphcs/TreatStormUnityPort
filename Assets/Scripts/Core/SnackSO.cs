using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "Snack", menuName = "SnackAttack/Snack")]
    public class SnackSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Gameplay")]
        public int pointValue;
        public float spawnWeight = 1f;
        public float despawnSeconds = 10f;
        public Color color = Color.white;

        [Header("Effect")]
        public EffectDefinition effect;

        [Header("Visuals")]
        public Sprite sprite;
    }
}
