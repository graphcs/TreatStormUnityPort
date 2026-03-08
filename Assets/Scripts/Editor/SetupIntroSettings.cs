#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Editor
{
    public static class SetupIntroSettings
    {
        [MenuItem("SnackAttack/Setup/Create Intro Settings")]
        public static void CreateIntroSettings()
        {
            // Create or load SO
            const string assetPath = "Assets/ScriptableObjects/Config/IntroSettings.asset";
            var existing = AssetDatabase.LoadAssetAtPath<IntroSettingsSO>(assetPath);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<IntroSettingsSO>();
                AssetDatabase.CreateAsset(existing, assetPath);
            }

            // Wire sprite references
            existing.cloudSprite1 = FindSprite("Cloud 1");
            existing.cloudSprite2 = FindSprite("Cloud 2");
            existing.titleSprite = FindSpriteInFolder("Assets/Art/UI/StormIntro", "Title  ");
            existing.goSprite = FindSpriteInFolder("Assets/Art/UI/StormIntro", "go");
            existing.groundSprite = FindSpriteInFolder("Assets/Art/UI/StormIntro", "ground");

            EditorUtility.SetDirty(existing);

            // Wire to GameManager
            var gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                var gm = gmObj.GetComponent<GameManager>();
                if (gm != null)
                {
                    var so = new SerializedObject(gm);
                    var prop = so.FindProperty("introSettings");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = existing;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("IntroSettings created and wired successfully.");
        }

        private static Sprite FindSprite(string name)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Sprite {name}", new[] { "Assets/Art/UI" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        private static Sprite FindSpriteInFolder(string folder, string name)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Texture2D", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                {
                    // Try loading as sprite first
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null) return sprite;

                    // Ensure texture import settings have sprite mode
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.SaveAndReimport();
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                    return sprite;
                }
            }
            return null;
        }
    }
}
#endif
