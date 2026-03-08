using UnityEditor;
using UnityEngine;

namespace SnackAttack.Editor
{
    public static class SetupAudioManager
    {
        [MenuItem("SnackAttack/Setup Audio Manager")]
        public static void Setup()
        {
            var gm = GameObject.Find("GameManager");
            if (gm == null)
            {
                Debug.LogError("[SetupAudioManager] GameManager not found in scene.");
                return;
            }

            var am = gm.GetComponent<Audio.AudioManager>();
            if (am == null)
            {
                Debug.LogError("[SetupAudioManager] AudioManager component not found on GameManager.");
                return;
            }

            var so = new SerializedObject(am);

            SetClip(so, "musicBackground", "Assets/Audio/Music/background.mp3");
            SetClip(so, "musicGameplay", "Assets/Audio/Music/Gameplay.mp3");
            SetClip(so, "sfxSelect", "Assets/Audio/SFX/select.mp3");
            SetClip(so, "sfxDogEat", "Assets/Audio/SFX/Dog eat.mp3");
            SetClip(so, "sfxPointEarned", "Assets/Audio/SFX/Point earned.mp3");
            SetClip(so, "sfxBroccoli", "Assets/Audio/SFX/Broccoli.mp3");
            SetClip(so, "sfxChilli", "Assets/Audio/SFX/chilli.mp3");
            SetClip(so, "sfxRedBull", "Assets/Audio/SFX/Red bull.mp3");
            SetClip(so, "sfxThunder", "Assets/Audio/SFX/Thunder.mp3");
            SetClip(so, "sfxCountdown23", "Assets/Audio/SFX/2&3.mp3");
            SetClip(so, "sfxCountdown1", "Assets/Audio/SFX/1.mp3");
            SetClip(so, "sfxStart", "Assets/Audio/SFX/start.mp3");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(am);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gm.scene);

            Debug.Log("[SetupAudioManager] All 12 AudioClip references wired successfully.");
        }

        private static void SetClip(SerializedObject so, string fieldName, string assetPath)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SetupAudioManager] Field '{fieldName}' not found.");
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null)
            {
                Debug.LogWarning($"[SetupAudioManager] AudioClip not found at '{assetPath}'.");
                return;
            }

            prop.objectReferenceValue = clip;
        }
    }
}
