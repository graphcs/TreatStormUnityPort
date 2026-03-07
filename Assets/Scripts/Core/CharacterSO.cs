using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "Character", menuName = "SnackAttack/Character")]
    public class CharacterSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public string breed;

        [Header("Stats")]
        public float baseSpeed = 1f;
        public Color color = Color.white;
        public Vector2 hitboxSize = new Vector2(130, 130);
        public float gameplaySize = 200f;

        [Header("Sprites — Animation Frames")]
        public Sprite[] runSprites;
        public Sprite[] eatSprites;
        public Sprite[] walkSprites;
        public Sprite[] chiliReactionSprites;

        [Header("Sprites — Single")]
        public Sprite boostSprite;
        public Sprite portrait;
        public Sprite faceCameraFlight;
    }
}
