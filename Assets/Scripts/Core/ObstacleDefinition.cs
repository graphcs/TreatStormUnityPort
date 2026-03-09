using System;
using UnityEngine;

namespace SnackAttack.Core
{
    [Serializable]
    public struct ObstacleDefinition
    {
        public string type;
        public Vector2 position;
        public Vector2 size;
    }
}
