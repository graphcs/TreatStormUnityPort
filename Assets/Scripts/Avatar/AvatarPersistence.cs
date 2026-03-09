using System;
using System.IO;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Avatar
{
    public static class AvatarPersistence
    {
        private static string CustomAvatarsDir =>
            Path.Combine(Application.persistentDataPath, "custom_avatars");

        /// <summary>
        /// Save all avatar data (PNGs + manifest JSON) to persistent storage.
        /// </summary>
        public static void SaveAvatar(string id, string displayName, string breed,
            byte[] profilePng, byte[] runPng, byte[] eatPng, byte[] walkPng, byte[] boostPng)
        {
            string dir = Path.Combine(CustomAvatarsDir, id);
            Directory.CreateDirectory(dir);

            File.WriteAllBytes(Path.Combine(dir, "profile.png"), profilePng);
            File.WriteAllBytes(Path.Combine(dir, "run_sprite.png"), runPng);
            File.WriteAllBytes(Path.Combine(dir, "eat_sprite.png"), eatPng);

            if (walkPng != null)
                File.WriteAllBytes(Path.Combine(dir, "walk_sprite.png"), walkPng);

            if (boostPng != null)
                File.WriteAllBytes(Path.Combine(dir, "boost.png"), boostPng);

            // Write manifest
            string manifest = "{\n" +
                $"  \"id\": \"{EscapeJson(id)}\",\n" +
                $"  \"displayName\": \"{EscapeJson(displayName)}\",\n" +
                $"  \"breed\": \"{EscapeJson(breed)}\",\n" +
                $"  \"baseSpeed\": 1.0,\n" +
                $"  \"color\": [200, 180, 150],\n" +
                $"  \"gameplaySize\": 163\n" +
                "}";
            File.WriteAllText(Path.Combine(dir, "manifest.json"), manifest);

            Debug.Log($"[AvatarPersistence] Saved avatar '{displayName}' to {dir}");
        }

        /// <summary>
        /// Load all custom avatars from persistent storage and add to character database.
        /// </summary>
        public static void LoadAllCustomAvatars(CharacterDatabaseSO database)
        {
            if (database == null) return;
            if (!Directory.Exists(CustomAvatarsDir)) return;

            string[] dirs = Directory.GetDirectories(CustomAvatarsDir);
            int loaded = 0;

            foreach (string dir in dirs)
            {
                string manifestPath = Path.Combine(dir, "manifest.json");
                if (!File.Exists(manifestPath)) continue;

                try
                {
                    string json = File.ReadAllText(manifestPath);
                    var manifest = ParseManifest(json);
                    if (manifest == null) continue;

                    string id = manifest.id;

                    // Skip if already in database
                    if (database.GetById(id) != null) continue;

                    // Load required PNGs
                    byte[] profilePng = ReadFileIfExists(Path.Combine(dir, "profile.png"));
                    byte[] runPng = ReadFileIfExists(Path.Combine(dir, "run_sprite.png"));
                    byte[] eatPng = ReadFileIfExists(Path.Combine(dir, "eat_sprite.png"));

                    if (profilePng == null || runPng == null || eatPng == null)
                    {
                        Debug.LogWarning($"[AvatarPersistence] Skipping '{id}' — missing required sprites");
                        continue;
                    }

                    byte[] walkPng = ReadFileIfExists(Path.Combine(dir, "walk_sprite.png"));
                    byte[] boostPng = ReadFileIfExists(Path.Combine(dir, "boost.png"));

                    // Create runtime CharacterSO
                    var character = ScriptableObject.CreateInstance<CharacterSO>();
                    character.id = manifest.id;
                    character.displayName = manifest.displayName;
                    character.breed = manifest.breed;
                    character.baseSpeed = manifest.baseSpeed;
                    character.color = new Color32((byte)manifest.colorR, (byte)manifest.colorG,
                        (byte)manifest.colorB, 255);
                    character.hitboxSize = new Vector2(130, 130);
                    character.gameplaySize = manifest.gameplaySize;
                    character.name = manifest.displayName;

                    // Create sprites
                    character.portrait = CreateSprite(profilePng, "portrait");
                    character.runSprites = SliceSpriteSheet(runPng, 3, "run");
                    character.eatSprites = SliceSpriteSheet(eatPng, 3, "eat");
                    character.walkSprites = walkPng != null ? SliceSpriteSheet(walkPng, 5, "walk") : new Sprite[0];
                    character.boostSprite = boostPng != null ? CreateSprite(boostPng, "boost") : null;
                    character.chiliReactionSprites = new Sprite[0];

                    database.characters.Add(character);
                    loaded++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AvatarPersistence] Failed to load avatar from {dir}: {e.Message}");
                }
            }

            if (loaded > 0)
                Debug.Log($"[AvatarPersistence] Loaded {loaded} custom avatar(s)");
        }

        private static byte[] ReadFileIfExists(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        private static Sprite CreateSprite(byte[] pngBytes, string name)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(pngBytes);
            tex.filterMode = FilterMode.Point;
            tex.name = name;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite[] SliceSpriteSheet(byte[] pngBytes, int frameCount, string baseName)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(pngBytes);
            tex.filterMode = FilterMode.Point;
            tex.name = baseName + "_sheet";

            int frameWidth = tex.width / frameCount;
            int frameHeight = tex.height;
            var sprites = new Sprite[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                var rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
                sprites[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                sprites[i].name = $"{baseName}_{i}";
            }

            return sprites;
        }

        #region Simple JSON Manifest Parser

        private class ManifestData
        {
            public string id, displayName, breed;
            public float baseSpeed = 1f;
            public int colorR = 200, colorG = 180, colorB = 150;
            public float gameplaySize = 163f;
        }

        private static ManifestData ParseManifest(string json)
        {
            var data = new ManifestData();

            data.id = ExtractString(json, "id");
            data.displayName = ExtractString(json, "displayName");
            data.breed = ExtractString(json, "breed");

            if (string.IsNullOrEmpty(data.id) || string.IsNullOrEmpty(data.displayName))
                return null;

            string speedStr = ExtractValue(json, "baseSpeed");
            if (float.TryParse(speedStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float speed))
                data.baseSpeed = speed;

            string sizeStr = ExtractValue(json, "gameplaySize");
            if (float.TryParse(sizeStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float size))
                data.gameplaySize = size;

            // Parse color array [r, g, b]
            int colorIdx = json.IndexOf("\"color\"", StringComparison.Ordinal);
            if (colorIdx >= 0)
            {
                int bracketStart = json.IndexOf('[', colorIdx);
                int bracketEnd = json.IndexOf(']', bracketStart);
                if (bracketStart >= 0 && bracketEnd > bracketStart)
                {
                    string colorStr = json.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                    string[] parts = colorStr.Split(',');
                    if (parts.Length >= 3)
                    {
                        if (int.TryParse(parts[0].Trim(), out int r)) data.colorR = r;
                        if (int.TryParse(parts[1].Trim(), out int g)) data.colorG = g;
                        if (int.TryParse(parts[2].Trim(), out int b)) data.colorB = b;
                    }
                }
            }

            return data;
        }

        private static string ExtractString(string json, string key)
        {
            string pattern = $"\"{key}\"";
            int idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0) return null;
            idx = json.IndexOf('"', idx + pattern.Length + 1); // skip colon, find opening quote
            if (idx < 0) return null;
            int endIdx = json.IndexOf('"', idx + 1);
            if (endIdx < 0) return null;
            return json.Substring(idx + 1, endIdx - idx - 1)
                .Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private static string ExtractValue(string json, string key)
        {
            string pattern = $"\"{key}\"";
            int idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0) return null;
            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx < 0) return null;
            int start = colonIdx + 1;
            while (start < json.Length && json[start] == ' ') start++;
            int end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '\n' && json[end] != '}') end++;
            return json.Substring(start, end - start).Trim();
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        #endregion
    }
}
