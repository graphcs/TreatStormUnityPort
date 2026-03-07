using System;
using UnityEngine;

namespace SnackAttack.Core
{
    [Serializable]
    public struct TreatTypeDefinition
    {
        public string id;
        public string name;
        public int pointValue;
        public float spawnWeight;
        public bool spawnBiasRight;
        public Color color;
    }
}
