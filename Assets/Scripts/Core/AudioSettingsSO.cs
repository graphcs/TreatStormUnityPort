using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "SnackAttack/Audio Settings")]
    public class AudioSettingsSO : ScriptableObject
    {
        [Range(0f, 1f)] public float masterVolume = 0.7f;
        [Range(0f, 1f)] public float musicVolume = 0.9f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        public bool musicEnabled = true;
        public bool sfxEnabled = true;
    }
}
