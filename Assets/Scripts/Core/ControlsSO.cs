using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "Controls", menuName = "SnackAttack/Controls")]
    public class ControlsSO : ScriptableObject
    {
        [Header("Shared Axes")]
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";

        [Header("Player-Specific Axes")]
        public string player1Horizontal = "Player1_Horizontal";
        public string player1Vertical = "Player1_Vertical";
        public string player2Horizontal = "Player2_Horizontal";
        public string player2Vertical = "Player2_Vertical";

        [Header("UI Actions")]
        public string submitAction = "Submit";
        public string cancelAction = "Cancel";
        public string quitAction = "Quit";

        [Header("Sound Names")]
        public string selectSound = "select";
        public string backgroundMusic = "background";
        public string gameplayMusic = "Gameplay";
    }
}
