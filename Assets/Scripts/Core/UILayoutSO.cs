using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "UILayout", menuName = "SnackAttack/UI Layout")]
    public class UILayoutSO : ScriptableObject
    {
        [Header("Character Select")]
        public int cardsPerRow = 3;
        public float cardWidth = 225f;
        public float cardHeight = 225f;
        public float spacingX = 32f;
        public float spacingY = 32f;
        public float scrollZoneTop = 310f;
        public float scrollZoneHeight = 540f;
        public Vector2 outlineDistance = new(4f, -4f);
        public float scrollSensitivity = 40f;

        [Header("Game Over — Layout")]
        public float winnerNameY = -180f;
        public Vector2 winnerNameSize = new(800f, 100f);
        public float winnerNameFontSize = 80f;
        public float winsImageY = -230f;
        public float winsImageScale = 0.6f;
        public float menuBarY = -330f;
        public float menuBarWidth = 912f;
        public float roundsTextY = -330f;
        public Vector2 roundsTextSize = new(800f, 50f);
        public float roundsTextFontSize = 28f;
        public float scoreBoxSingleX = 319f;
        public float scoreBoxY = -350f;
        public float scoreBoxP1X = 72f;
        public float scoreBoxP2X = 565f;
        public Vector2 scoreBoxSize = new(563f, 521f);
        public float scoreNameY = -198f;
        public float scoreNameFontSize = 43f;
        public float scoreLabelY = -260f;
        public float scoreLabelFontSize = 24f;
        public float scoreValueY = -323f;
        public float scoreValueFontSize = 61f;
        public float playAgainY = -850f;
        public float mainMenuY = -910f;
        public float menuOptionFontSize = 28f;
        public Vector2 selectIndicatorSize = new(36f, 30f);

        [Header("Settings")]
        public float settingsContainerY = -570f;
        public Vector2 settingsContainerSize = new(960f, 700f);
        public float settingsTitleY = -180f;
        public Vector2 settingsTitleSize = new(720f, 120f);
        public float settingsItemStartY = -404f;
        public float settingsItemSpacing = 70f;
        public float settingsLabelX = 264f;
        public float settingsValueX = 744f;
        public float settingsSliderWidth = 216f;
        public float settingsSliderHeight = 20f;
        public float settingsPercentOffsetX = 40f;
        public float settingsBackY = -880f;
        public float settingsFooterY = -960f;
        public float settingsItemFontSize = 28f;
        public float settingsBackFontSize = 32f;
        public float settingsFooterFontSize = 18f;
        public float settingsPercentFontSize = 18f;
        public Vector2 settingsSelectSize = new(34.56f, 28.8f);
        public float settingsSelectOffsetX = 6f;

        [Header("Select Indicator")]
        public float selectorOffset = 6f;
        public float selectorPadding = 10f;

        [Header("Upload Avatar")]
        public float avatarTitleY = -100f;
        public float avatarNameLabelY = -288f;
        public float avatarNameInputY = -332f;
        public Vector2 avatarNameInputSize = new(400f, 50f);
        public float avatarPhotoAreaY = -440f;
        public Vector2 avatarPlaceholderSize = new(300f, 150f);
        public float avatarBrowseBtnY = -660f;
        public Vector2 avatarBrowseBtnSize = new(200f, 45f);
        public float avatarGenerateBtnY = -740f;
        public Vector2 avatarGenerateBtnSize = new(300f, 55f);
        public float avatarBackBtnY = -920f;
        public Vector2 avatarBackBtnSize = new(160f, 40f);
        public Vector2 avatarProgressBarSize = new(500f, 30f);

        [Header("HUD")]
        public Vector2 popupSize = new(200f, 50f);
        public float outlineWidth = 0.25f;
    }
}
