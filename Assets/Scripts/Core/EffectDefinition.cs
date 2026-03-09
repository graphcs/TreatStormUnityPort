using System;

namespace SnackAttack.Core
{
    [Serializable]
    public struct EffectDefinition
    {
        public EffectType type;
        public float magnitude;
        public float duration;

        public bool HasEffect => type != EffectType.None;
    }
}
