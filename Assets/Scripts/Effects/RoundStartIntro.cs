using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Effects
{
    public class RoundStartIntro : MonoBehaviour
    {
        private StormIntroSequence _sequence;

        public bool IsComplete => _sequence != null && _sequence.IsComplete;

        public void Initialize(RectTransform root, IntroSettingsSO settings,
            CharacterSO p1Char, CharacterSO p2Char,
            float dog1TargetX = -1f, float dog2TargetX = -1f,
            float dogTopY = -1f)
        {
            Sprite[] p1RunSprites = p1Char != null ? p1Char.runSprites : null;
            Sprite[] p2RunSprites = p2Char != null ? p2Char.runSprites : null;

            float dog1Size = p1Char != null ? p1Char.gameplaySize : 216f;
            float dog2Size = p2Char != null ? p2Char.gameplaySize : 216f;

            // Use gameplay-derived dogTopY if provided, otherwise fall back to groundYFraction
            float dogTopFromTop = dogTopY >= 0f
                ? dogTopY
                : 1000f * settings.groundYFraction - dog1Size * settings.dogRenderScale;

            _sequence = gameObject.AddComponent<StormIntroSequence>();
            _sequence.Initialize(root, settings,
                settings.cloudSprite1, settings.cloudSprite2,
                settings.titleSprite, settings.goSprite, settings.groundSprite,
                p1RunSprites, p2RunSprites, dogTopFromTop,
                dog1Size, dog2Size, dog1TargetX, dog2TargetX);
            _sequence.StartSequence();
        }
    }
}
