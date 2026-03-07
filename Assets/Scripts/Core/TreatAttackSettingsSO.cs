using System;
using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "TreatAttackSettings", menuName = "SnackAttack/Treat Attack Settings")]
    public class TreatAttackSettingsSO : ScriptableObject
    {
        public ScreenSettings screen;
        public GameplaySettings gameplay;
        public DogSettings dog;
        public LeashSettings leash;
        public TreatsSettings treats;
        public VotingSettings voting;
        public UISettings ui;

        [Serializable]
        public struct ScreenSettings
        {
            public int width;
            public int height;
        }

        [Serializable]
        public struct GameplaySettings
        {
            public float roundDurationSeconds;
            public float baseFallSpeed;
            public float fallSpeedIncreasePerLevel;
            public float spawnIntervalSeconds;
            public float minSpawnIntervalSeconds;
        }

        [Serializable]
        public struct DogSettings
        {
            public float groundY;
            public float width;
            public float height;
            public float moveSpeed;
            public float leashAnchorX;
        }

        [Serializable]
        public struct LeashSettings
        {
            public float defaultMinX;
            public float defaultMaxX;
            public float extendedMaxX;
            public float yankedMaxX;
            public float effectDurationSeconds;
        }

        [Serializable]
        public struct TreatsSettings
        {
            public TreatTypeDefinition[] treatTypes;
            public Vector2 size;
        }

        [Serializable]
        public struct VotingSettings
        {
            public float windowDurationSeconds;
            public float cooldownSeconds;
            public string[] extendCommands;
            public string[] yankCommands;
        }

        [Serializable]
        public struct UISettings
        {
            public float meterHeight;
            public float meterMargin;
            public float scoreFontSize;
            public float timerFontSize;
        }
    }
}
