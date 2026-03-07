using UnityEngine;
using UnityEditor;
using TMPro;
using TMPro.EditorUtilities;

namespace SnackAttack.Editor
{
    public static class CreateDaydreamFont
    {
        [MenuItem("Tools/SnackAttack/Create Daydream SDF Font")]
        public static void Create()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Daydream.ttf");
            if (font == null)
            {
                Debug.LogError("Daydream.ttf not found at Assets/Fonts/Daydream.ttf");
                return;
            }

            // Create TMP font asset from dynamic font
            var tmpFont = TMP_FontAsset.CreateFontAsset(font);
            if (tmpFont == null)
            {
                Debug.LogError("Failed to create TMP_FontAsset from Daydream.ttf");
                return;
            }

            tmpFont.name = "Daydream SDF";
            AssetDatabase.CreateAsset(tmpFont, "Assets/Fonts/Daydream SDF.asset");

            // Generate atlas with ASCII characters
            uint[] characterSet = new uint[95];
            for (int i = 0; i < 95; i++)
                characterSet[i] = (uint)(32 + i); // ASCII 32-126

            tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Daydream SDF font asset created at Assets/Fonts/Daydream SDF.asset");
        }
    }
}
