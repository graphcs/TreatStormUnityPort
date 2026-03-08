using System;
using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "Controls", menuName = "SnackAttack/Controls")]
    public class ControlsSO : ScriptableObject
    {
        public PlayerControls player1;
        public PlayerControls player2;
        public GlobalControls global;

        [Serializable]
        public struct PlayerControls
        {
            public string up;
            public string down;
            public string left;
            public string right;
        }

        [Serializable]
        public struct GlobalControls
        {
            public string pause;
            public string confirm;
            public string back;
        }

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
