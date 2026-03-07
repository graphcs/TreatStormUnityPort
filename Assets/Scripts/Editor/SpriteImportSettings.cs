using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-configures sprite import settings to match PyGame's pixel art style.
/// PPU=100 throughout; scaling is handled via Transform.localScale at runtime.
///
/// PyGame reference sizes (runtime, in pixels on a 1200x1000 screen):
///   Gameplay sprites (run/eat): 216x216 (raw 500x500 per frame, 3 frames in 1500x500 sheet)
///   Prissy gameplay: 173x173
///   Portraits: 160x160 (raw 350x350)
///   Food: 72x72 (raw 1024x1024)
///   Face camera flight: 216x216 (raw 500x500)
///   Steam: 64x64 (raw 500x500)
/// </summary>
public class SpriteImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/") && !assetPath.StartsWith("Assets/Fonts/"))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = 100;

        // Sprite sheets need Multiple sprite mode for slicing
        if (assetPath.Contains("SpriteSheets/") && IsSpriteSheet(assetPath))
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
        }
        else
        {
            importer.spriteImportMode = SpriteImportMode.Single;
        }
    }

    static bool IsSpriteSheet(string path)
    {
        string lower = path.ToLower();
        // These are sprite sheets with multiple frames
        return lower.Contains("run sprite") ||
               lower.Contains("eat_attack") || lower.Contains("eat attack") ||
               lower.Contains("walking") ||
               lower.Contains("chili reaction");
    }
}
