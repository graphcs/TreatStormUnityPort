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

        [Header("Select Indicator")]
        public float selectorOffset = 6f;
        public float selectorPadding = 10f;

        [Header("HUD")]
        public Vector2 popupSize = new(200f, 50f);
        public float outlineWidth = 0.25f;
    }
}
