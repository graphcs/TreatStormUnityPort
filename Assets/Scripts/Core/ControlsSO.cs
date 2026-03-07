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
    }
}
