using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Avatar
{
    public class AvatarGenerationProgress
    {
        public int currentStep;
        public int totalSteps = 7;
        public string stepDescription = "Initializing...";
        public bool isComplete;
        public bool isError;
        public string errorMessage;
    }

    public class AvatarGenerationResult
    {
        public bool success;
        public string characterId;
        public string characterName;
        public string errorMessage;
        public CharacterSO character; // Runtime SO instance

        // Raw PNG bytes for persistence
        public byte[] profileBytes;
        public byte[] runBytes;
        public byte[] eatBytes;
        public byte[] walkBytes;  // may be null
        public byte[] boostBytes; // may be null
        public string breed;
    }

    public class AvatarGenerator : MonoBehaviour
    {
        // Target sizes matching existing sprites
        private static readonly Vector2Int PROFILE_SIZE = new(350, 350);
        private static readonly Vector2Int SPRITE_SHEET_SIZE = new(1500, 500);
        private static readonly Vector2Int WALK_SHEET_SIZE = new(1430, 286);
        private static readonly Vector2Int BOOST_SPRITE_SIZE = new(500, 500);

        #region Prompts (verbatim from avatar_generator.py)

        private const string STYLE_CORE =
            "You MUST match the EXACT same pixel art style shown in the reference sprite image I am providing. " +
            "Study the reference image carefully: copy its pixel density, thick black outlines, " +
            "level of detail, chunky proportions, warm retro color palette, shading technique, and overall vibe. " +
            "The output must look like it belongs in the same game as the reference sprite \u2014 " +
            "same resolution feel, same outline thickness, same level of pixelation. " +
            "CRITICAL: The background MUST be fully transparent (alpha = 0). " +
            "Do NOT draw any ground, shadow, floor, scenery, or solid background color. " +
            "The dog character must be the ONLY element in the image, floating on a transparent background. " +
            "Output as PNG with transparency.";

        private const string DESCRIBE_DOG_PROMPT =
            "Look at this photo of a dog. Describe it in PRECISE detail so another artist can draw " +
            "the EXACT same dog without seeing the photo. Include: breed/mix, size proportions (stocky/slim/etc), " +
            "exact fur colors and pattern (e.g. 'golden tan body with white chest patch and black muzzle'), " +
            "ear shape (floppy/pointed/folded), tail type, " +
            "any unique markings (spots, patches, mask, socks). " +
            "Keep the description to 3-4 sentences, very specific.";

        private const string PROFILE_PROMPT =
            "I am providing two images:\n" +
            "IMAGE 1 (first image): A photo of a real dog.\n" +
            "IMAGE 2 (second image): A reference pixel art sprite from my game \u2014 match this EXACT art style.\n\n" +
            "DOG IDENTITY (you MUST match this dog exactly): {0}\n\n" +
            "Create a single pixel art PORTRAIT of this specific dog, drawn in the exact same style " +
            "as the reference sprite. The portrait should show the dog sitting and facing slightly toward " +
            "the viewer (3/4 front view), looking cute and happy with a visible tongue. " +
            "The dog should be centered and fill most of the square image. " +
            "This will be used as a character select portrait in a retro arcade game.\n\n" +
            "{1}\n" +
            "Output a single SQUARE image.";

        private const string RUN_SPRITE_PROMPT =
            "I am providing three images:\n" +
            "IMAGE 1 (first image): A photo of a real dog.\n" +
            "IMAGE 2 (second image): The pixel art version of this dog that I already created \u2014 " +
            "you MUST draw the EXACT SAME dog character with identical colors, proportions, and markings.\n" +
            "IMAGE 3 (third image): A reference RUN SPRITE SHEET from my game \u2014 match this EXACT layout.\n\n" +
            "DOG IDENTITY (you MUST match this dog exactly): {0}\n\n" +
            "Create a HORIZONTAL SPRITE SHEET containing EXACTLY 3 animation frames of this dog RUNNING, " +
            "arranged side by side in a single wide image (width = 3x height). " +
            "Study the reference sprite sheet layout: 3 equally-sized frames placed left to right. " +
            "Each frame shows the dog in side-view profile, facing RIGHT, in a different phase of a run cycle: " +
            "Frame 1 (left): normal stance. " +
            "Frame 2 (center): legs mid-stride, body bouncing up. " +
            "Frame 3 (right): opposite leg positions from frame 1. " +
            "The dog must be the SAME SIZE and in the SAME POSITION in all 3 frames. " +
            "Only the legs should change between frames.\n\n" +
            "CRITICAL: The dog in this sprite sheet must be the EXACT SAME character as in IMAGE 2. " +
            "Same colors, same fur pattern, same ear shape, same proportions.\n\n" +
            "{1}\n" +
            "Output a SINGLE WIDE image with all 3 frames side by side.";

        private const string EAT_SPRITE_PROMPT =
            "I am providing three images:\n" +
            "IMAGE 1 (first image): A photo of a real dog.\n" +
            "IMAGE 2 (second image): The pixel art version of this dog that I already created \u2014 " +
            "you MUST draw the EXACT SAME dog character with identical colors, proportions, and markings.\n" +
            "IMAGE 3 (third image): A reference EAT/ATTACK SPRITE SHEET from my game \u2014 match this EXACT layout.\n\n" +
            "DOG IDENTITY (you MUST match this dog exactly): {0}\n\n" +
            "Create a HORIZONTAL SPRITE SHEET containing EXACTLY 3 animation frames of this dog EATING/BITING, " +
            "arranged side by side in a single wide image (width = 3x height). " +
            "Study the reference sprite sheet layout: 3 equally-sized frames placed left to right. " +
            "Each frame shows the dog in side-view profile, facing RIGHT: " +
            "Frame 1 (left): Idle pose. Standing on all four short legs with a neutral expression and tail slightly raised. " +
            "Frame 2 (center): Lunge/Attack pose. Body lowered, leaning forward with mouth wide open (showing pink tongue). Ears and tail sweeping back from momentum. " +
            "Frame 3 (right): Leap pose. Jumping on hind legs, body angled upward. Front paws raised together, mouth open, ears flipped back." +
            "The dog must be the SAME SIZE and in the SAME POSITION in all 3 frames. " +
            "Only the mouth/head should change between frames.\n\n" +
            "CRITICAL: The dog in this sprite sheet must be the EXACT SAME character as in IMAGE 2. " +
            "Same colors, same fur pattern, same ear shape, same proportions.\n\n" +
            "{1}\n" +
            "Output a SINGLE WIDE image with all 3 frames side by side.";

        private const string WALK_SPRITE_PROMPT =
            "I am providing three images:\n" +
            "IMAGE 1 (first image): A photo of a real dog.\n" +
            "IMAGE 2 (second image): The pixel art version of this dog that I already created \u2014 " +
            "you MUST draw the EXACT SAME dog character with identical colors, proportions, and markings.\n" +
            "IMAGE 3 (third image): A reference RUN SPRITE from my game \u2014 match this EXACT art style.\n\n" +
            "DOG IDENTITY (you MUST match this dog exactly): {0}\n\n" +
            "Create a HORIZONTAL SPRITE SHEET containing EXACTLY 5 animation frames of this dog WALKING " +
            "Frame 1: The Stretch (Extended): The front-right leg is extended forward, just touching the ground. The back-right leg is extended fully behind, pushing off the toe. The tail is curled upward." +
            "Frame 2: The Plant (Down): The body weight shifts onto the front-right leg, which is now vertical under the shoulder. The back-left leg begins to pull forward, while the back-right leg is mid-lift." +
            "Frame 3: The Passing (Mid): The front-right leg angles backward. The front-left leg is lifted and passing the stationary leg. The back-left leg is bent and moving forward under the belly." +
            "Frame 4: The Lift (Up): The front-left leg is lifted high, knee bent, preparing to step forward. The back-left leg is reaching toward its forward landing position. The tail stays curled." +
            "Frame 5: The Reset (Contact): The front-left leg is fully extended forward for the next step. The back-right leg is planted firmly. This mirrors Frame 1 but with the opposite leg set, completing the loop." +
            "(not running \u2014 a slower, gentle walk), arranged side by side in a single wide image. " +
            "Each frame shows the dog in side-view profile, facing RIGHT, in a different phase of a walk cycle. " +
            "The dog must be the SAME SIZE in all 5 frames. Only the legs move between frames, " +
            "showing a smooth walking gait.\n\n" +
            "CRITICAL: The dog in this sprite sheet must be the EXACT SAME character as in IMAGE 2. " +
            "Same colors, same fur pattern, same ear shape, same proportions.\n\n" +
            "{1}\n" +
            "Output a SINGLE WIDE image with all 5 frames side by side.";

        private const string BOOST_SPRITE_PROMPT =
            "I am providing multiple references:\n" +
            "IMAGE 1: Real dog photo.\n" +
            "IMAGE 2: Generated pixel-art profile of this exact dog.\n" +
            "IMAGE 3: Pixel-art run reference from the game style.\n\n" +
            "DOG IDENTITY (you MUST match this dog exactly): {0}\n\n" +
            "Create ONE single side-view pixel-art sprite of this dog facing RIGHT, with wings naturally emerging " +
            "from the shoulder/back area. The wings must look attached to the body (not floating), with feather roots " +
            "blending into fur. Keep exactly the same character identity and palette as IMAGE 2.\n\n" +
            "This is a boost state sprite for a retro arcade game.\n" +
            "Output a SINGLE SQUARE image (transparent background).\n\n" +
            "{1}";

        #endregion

        // Cached reference sprite base64
        private string _refProfileB64;
        private string _refRunB64;
        private string _refEatB64;

        /// <summary>
        /// Start the full 7-step avatar generation pipeline.
        /// </summary>
        public Coroutine StartGeneration(string photoPath, string dogName, string apiKey,
            Action<AvatarGenerationProgress> onProgress,
            Action<AvatarGenerationResult> onComplete)
        {
            return StartCoroutine(RunPipeline(photoPath, dogName, apiKey, onProgress, onComplete));
        }

        private IEnumerator RunPipeline(string photoPath, string dogName, string apiKey,
            Action<AvatarGenerationProgress> onProgress,
            Action<AvatarGenerationResult> onComplete)
        {
            var progress = new AvatarGenerationProgress();
            var client = new OpenRouterClient(apiKey);
            string displayName = dogName.Trim();
            // Title case
            if (displayName.Length > 0)
                displayName = char.ToUpper(displayName[0]) + (displayName.Length > 1 ? displayName.Substring(1) : "");

            string characterId = GenerateCharacterId(dogName);

            // Load photo
            byte[] photoRaw;
            try
            {
                photoRaw = File.ReadAllBytes(photoPath);
            }
            catch (Exception e)
            {
                progress.isError = true;
                progress.errorMessage = $"Failed to read photo: {e.Message}";
                onProgress?.Invoke(progress);
                onComplete?.Invoke(new AvatarGenerationResult { success = false, errorMessage = progress.errorMessage });
                yield break;
            }
            string photoB64 = Convert.ToBase64String(photoRaw);

            // Load reference sprites
            LoadReferenceSprites();

            // ---- Step 1: Describe dog ----
            progress.currentStep = 1;
            progress.stepDescription = "Analyzing your dog's features...";
            onProgress?.Invoke(progress);

            string dogDescription = null;
            string descError = null;
            yield return client.AnalyzeImage(photoB64, DESCRIBE_DOG_PROMPT,
                r => dogDescription = r, e => descError = e);

            if (string.IsNullOrEmpty(dogDescription))
            {
                dogDescription = "a cute dog matching the photo provided";
                if (descError != null)
                    Debug.LogWarning($"[AvatarGenerator] Dog description failed: {descError}, using fallback");
            }
            Debug.Log($"[AvatarGenerator] Dog description: {dogDescription}");

            // ---- Step 2: Profile portrait ----
            progress.currentStep = 2;
            progress.stepDescription = $"Creating {displayName}'s portrait...";
            onProgress?.Invoke(progress);

            string profilePrompt = string.Format(PROFILE_PROMPT, dogDescription, STYLE_CORE);
            var profileRefs = new List<string>();
            if (_refProfileB64 != null) profileRefs.Add(_refProfileB64);

            byte[] profileBytes = null;
            string genError = null;
            yield return client.GenerateImageFromPhoto(photoB64, profilePrompt,
                profileRefs, "1:1", b => profileBytes = b, e => genError = e);

            if (profileBytes == null)
            {
                string err = genError ?? "Failed to generate profile portrait.";
                FailPipeline(progress, err, onProgress, onComplete);
                yield break;
            }
            profileBytes = FitToSize(profileBytes, PROFILE_SIZE, false);

            // Encode generated profile for identity reference in subsequent steps
            string profileRefB64 = Convert.ToBase64String(profileBytes);

            // ---- Step 3: Run sprite sheet ----
            progress.currentStep = 3;
            progress.stepDescription = $"Creating {displayName}'s run animation...";
            onProgress?.Invoke(progress);

            string runPrompt = string.Format(RUN_SPRITE_PROMPT, dogDescription, STYLE_CORE);
            var runRefs = new List<string> { profileRefB64 };
            if (_refRunB64 != null) runRefs.Add(_refRunB64);

            byte[] runBytes = null;
            genError = null;
            yield return client.GenerateImageFromPhoto(photoB64, runPrompt,
                runRefs, "21:9", b => runBytes = b, e => genError = e);

            if (runBytes == null)
            {
                string err = genError ?? "Failed to generate run animation.";
                FailPipeline(progress, err, onProgress, onComplete);
                yield break;
            }
            runBytes = FitToSize(runBytes, SPRITE_SHEET_SIZE, true);

            // ---- Step 4: Eat sprite sheet ----
            progress.currentStep = 4;
            progress.stepDescription = $"Creating {displayName}'s eat animation...";
            onProgress?.Invoke(progress);

            string eatPrompt = string.Format(EAT_SPRITE_PROMPT, dogDescription, STYLE_CORE);
            var eatRefs = new List<string> { profileRefB64 };
            if (_refEatB64 != null) eatRefs.Add(_refEatB64);

            byte[] eatBytes = null;
            genError = null;
            yield return client.GenerateImageFromPhoto(photoB64, eatPrompt,
                eatRefs, "21:9", b => eatBytes = b, e => genError = e);

            if (eatBytes == null)
            {
                string err = genError ?? "Failed to generate eat animation.";
                FailPipeline(progress, err, onProgress, onComplete);
                yield break;
            }
            eatBytes = FitToSize(eatBytes, SPRITE_SHEET_SIZE, true);

            // ---- Step 5: Walk sprite sheet (optional) ----
            progress.currentStep = 5;
            progress.stepDescription = $"Creating {displayName}'s walk animation...";
            onProgress?.Invoke(progress);

            string walkPrompt = string.Format(WALK_SPRITE_PROMPT, dogDescription, STYLE_CORE);
            var walkRefs = new List<string> { profileRefB64 };
            if (_refRunB64 != null) walkRefs.Add(_refRunB64);

            byte[] walkBytes = null;
            yield return client.GenerateImageFromPhoto(photoB64, walkPrompt,
                walkRefs, "21:9", b => walkBytes = b, e =>
                    Debug.LogWarning($"[AvatarGenerator] Walk sprite failed (optional): {e}"));

            if (walkBytes != null)
                walkBytes = FitToSize(walkBytes, WALK_SHEET_SIZE, true);

            // ---- Step 6: Boost sprite (optional) ----
            progress.currentStep = 6;
            progress.stepDescription = $"Creating {displayName}'s winged boost form...";
            onProgress?.Invoke(progress);

            string boostPrompt = string.Format(BOOST_SPRITE_PROMPT, dogDescription, STYLE_CORE);
            var boostRefs = new List<string> { profileRefB64 };
            if (_refRunB64 != null) boostRefs.Add(_refRunB64);

            byte[] boostBytes = null;
            yield return client.GenerateImageFromPhoto(photoB64, boostPrompt,
                boostRefs, "1:1", b => boostBytes = b, e =>
                    Debug.LogWarning($"[AvatarGenerator] Boost sprite failed (optional): {e}"));

            if (boostBytes != null)
                boostBytes = FitToSize(boostBytes, BOOST_SPRITE_SIZE, false);

            // ---- Step 7: Register character ----
            progress.currentStep = 7;
            progress.stepDescription = $"Registering {displayName}...";
            onProgress?.Invoke(progress);

            CharacterSO character;
            try
            {
                character = CreateCharacterSO(characterId, displayName, dogDescription,
                    profileBytes, runBytes, eatBytes, walkBytes, boostBytes);

                // Add to database
                var db = GameManager.Instance.CharacterDatabase;
                if (db != null && db.GetById(characterId) == null)
                    db.characters.Add(character);
            }
            catch (Exception e)
            {
                FailPipeline(progress, $"Failed to register character: {e.Message}", onProgress, onComplete);
                yield break;
            }

            // Save to disk for persistence
            try
            {
                AvatarPersistence.SaveAvatar(characterId, displayName, dogDescription,
                    profileBytes, runBytes, eatBytes, walkBytes, boostBytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarGenerator] Failed to persist avatar: {e.Message}");
            }

            // Done
            progress.isComplete = true;
            progress.stepDescription = "Complete!";
            onProgress?.Invoke(progress);

            onComplete?.Invoke(new AvatarGenerationResult
            {
                success = true,
                characterId = characterId,
                characterName = displayName,
                character = character,
                breed = dogDescription,
                profileBytes = profileBytes,
                runBytes = runBytes,
                eatBytes = eatBytes,
                walkBytes = walkBytes,
                boostBytes = boostBytes
            });
        }

        private void FailPipeline(AvatarGenerationProgress progress, string error,
            Action<AvatarGenerationProgress> onProgress, Action<AvatarGenerationResult> onComplete)
        {
            Debug.LogError($"[AvatarGenerator] Pipeline failed: {error}");
            progress.isError = true;
            progress.errorMessage = error;
            onProgress?.Invoke(progress);
            onComplete?.Invoke(new AvatarGenerationResult { success = false, errorMessage = error });
        }

        private void LoadReferenceSprites()
        {
            if (_refProfileB64 != null) return; // already cached

            string refsDir = Path.Combine(Application.streamingAssetsPath, "AvatarRefs");

            _refProfileB64 = LoadRefFile(Path.Combine(refsDir, "Jazzy.png"));
            _refRunB64 = LoadRefFile(Path.Combine(refsDir, "Jazzy run sprite.png"));
            _refEatB64 = LoadRefFile(Path.Combine(refsDir, "Jazzy eat_attack sprite.png"));
        }

        private string LoadRefFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[AvatarGenerator] Reference sprite not found: {path}");
                return null;
            }
            try
            {
                return Convert.ToBase64String(File.ReadAllBytes(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AvatarGenerator] Failed to load reference: {e.Message}");
                return null;
            }
        }

        #region Image Processing

        /// <summary>
        /// Resize/fit raw PNG bytes to target dimensions.
        /// For sprite sheets (isSpriteSheet=true): scale to match target width, pad/crop height.
        /// For squares: proportionally fit and center.
        /// </summary>
        private byte[] FitToSize(byte[] pngBytes, Vector2Int targetSize, bool isSpriteSheet)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(pngBytes))
            {
                Destroy(tex);
                return pngBytes;
            }

            int tw = targetSize.x, th = targetSize.y;
            int iw = tex.width, ih = tex.height;

            if (iw == tw && ih == th)
            {
                Destroy(tex);
                return pngBytes;
            }

            Texture2D result;
            if (isSpriteSheet)
            {
                // Scale to match target width, pad/crop height
                float scale = (float)tw / iw;
                int newW = tw;
                int newH = Mathf.RoundToInt(ih * scale);

                var scaled = ResizeTexture(tex, newW, newH);
                Destroy(tex);

                if (newH == th)
                {
                    result = scaled;
                }
                else
                {
                    // Create canvas and center vertically
                    result = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                    var clearPixels = new Color32[tw * th];
                    result.SetPixels32(clearPixels); // transparent

                    if (newH > th)
                    {
                        // Crop from center
                        int cropY = (newH - th) / 2;
                        var pixels = scaled.GetPixels(0, cropY, tw, th);
                        result.SetPixels(0, 0, tw, th, pixels);
                    }
                    else
                    {
                        // Pad — place centered
                        int offsetY = (th - newH) / 2;
                        var pixels = scaled.GetPixels(0, 0, newW, newH);
                        result.SetPixels(0, offsetY, newW, newH, pixels);
                    }
                    result.Apply();
                    Destroy(scaled);
                }
            }
            else
            {
                // Proportionally fit and center on transparent canvas
                float scale = Mathf.Min((float)tw / iw, (float)th / ih);
                int newW = Mathf.RoundToInt(iw * scale);
                int newH = Mathf.RoundToInt(ih * scale);

                var scaled = ResizeTexture(tex, newW, newH);
                Destroy(tex);

                result = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                var clearPixels = new Color32[tw * th];
                result.SetPixels32(clearPixels);

                int offsetX = (tw - newW) / 2;
                int offsetY = (th - newH) / 2;
                var pixels = scaled.GetPixels(0, 0, newW, newH);
                result.SetPixels(offsetX, offsetY, newW, newH, pixels);
                result.Apply();
                Destroy(scaled);
            }

            byte[] output = result.EncodeToPNG();
            Destroy(result);
            return output;
        }

        private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            var rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            var result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            result.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        #endregion

        #region Sprite Slicing & Character Creation

        private CharacterSO CreateCharacterSO(string id, string displayName, string breed,
            byte[] profilePng, byte[] runPng, byte[] eatPng, byte[] walkPng, byte[] boostPng)
        {
            var character = ScriptableObject.CreateInstance<CharacterSO>();
            character.id = id;
            character.displayName = displayName;
            character.breed = breed;
            character.baseSpeed = 1.0f;
            character.color = new Color32(200, 180, 150, 255);
            character.hitboxSize = new Vector2(130, 130);
            character.gameplaySize = 163f;
            character.name = displayName;

            // Profile portrait
            character.portrait = LoadSpriteFromPng(profilePng, "portrait");

            // Run: 3 frames from 1500x500
            character.runSprites = SliceSpriteSheet(runPng, 3, "run");

            // Eat: 3 frames from 1500x500
            character.eatSprites = SliceSpriteSheet(eatPng, 3, "eat");

            // Walk: 5 frames from 1430x286 (optional)
            if (walkPng != null)
                character.walkSprites = SliceSpriteSheet(walkPng, 5, "walk");
            else
                character.walkSprites = new Sprite[0];

            // Boost: single sprite (optional)
            if (boostPng != null)
                character.boostSprite = LoadSpriteFromPng(boostPng, "boost");

            // No chili reaction sprites for custom characters
            character.chiliReactionSprites = new Sprite[0];

            return character;
        }

        private Sprite LoadSpriteFromPng(byte[] pngBytes, string name)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(pngBytes);
            tex.filterMode = FilterMode.Point;
            tex.name = name;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite[] SliceSpriteSheet(byte[] pngBytes, int frameCount, string baseName)
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
                // Sprite.Create uses bottom-left origin, left-to-right
                var rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
                sprites[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                sprites[i].name = $"{baseName}_{i}";
            }

            return sprites;
        }

        #endregion

        #region Character ID

        private string GenerateCharacterId(string dogName)
        {
            string id = dogName.ToLower().Trim().Replace(" ", "_");
            var sb = new System.Text.StringBuilder();
            foreach (char c in id)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }
            id = sb.ToString();
            if (string.IsNullOrEmpty(id)) id = "custom_dog";

            // Check for uniqueness
            var db = GameManager.Instance?.CharacterDatabase;
            if (db != null)
            {
                string baseId = id;
                int counter = 1;
                while (db.GetById(id) != null)
                {
                    id = $"{baseId}_{counter}";
                    counter++;
                }
            }

            return id;
        }

        #endregion
    }
}
