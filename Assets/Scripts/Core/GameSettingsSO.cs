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
        public float arenaWidth = 350f;
        public float arenaHeight = 450f;
        public float splitScreenGap = 20f;
    }
}
