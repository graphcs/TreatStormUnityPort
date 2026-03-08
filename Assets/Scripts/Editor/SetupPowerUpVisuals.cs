using UnityEngine;
using UnityEditor;
using SnackAttack.Core;

namespace SnackAttack.Editor
{
    public static class SetupPowerUpVisuals
    {
        [MenuItem("SnackAttack/Setup Power Up Visuals")]
        public static void Setup()
        {
            var visuals = AssetDatabase.LoadAssetAtPath<PowerUpVisualsSO>(
                "Assets/ScriptableObjects/Config/PowerUpVisuals.asset");
            if (visuals == null)
            {
                Debug.LogError("PowerUpVisuals.asset not found!");
                return;
            }

            // Wire wing sprites
            var wingUp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Wings/wing_up.png");
            var wingDown = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Wings/wing_down.png");

            var wings = visuals.wings;
            if (wingUp != null) wings.wingUpSprite = wingUp;
            if (wingDown != null) wings.wingDownSprite = wingDown;
            visuals.wings = wings;

            // Wire white sprite for status indicator bars
            var whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/white.png");
            if (whiteSprite != null) visuals.whiteSprite = whiteSprite;

            EditorUtility.SetDirty(visuals);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SetupPowerUpVisuals] Done. wingUp={wingUp != null}, wingDown={wingDown != null}, white={whiteSprite != null}");
        }
    }
}
