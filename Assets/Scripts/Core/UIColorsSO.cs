using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "UIColors", menuName = "SnackAttack/UI Colors")]
    public class UIColorsSO : ScriptableObject
    {
        [Header("General UI")]
        public Color winnerColor = new Color32(147, 76, 48, 255);       // #934C30
        public Color tieColor = new Color32(255, 200, 0, 255);          // #FFC800
        public Color roundsColor = new Color32(77, 43, 31, 255);        // #4D2B1F
        public Color menuNormal = new Color32(77, 43, 31, 255);         // #4D2B1F
        public Color menuSelected = new Color32(147, 76, 48, 255);      // #934C30

        [Header("HUD")]
        public Color timerColor = new Color32(77, 43, 31, 255);         // #4D2B1F
        public Color scoreColor = new Color32(147, 76, 48, 255);        // #934C30
        public Color countdownColor = new Color32(251, 205, 100, 255);  // #FBCD64
        public Color popupPositiveColor = new Color32(81, 180, 71, 255);  // #51B447
        public Color popupNegativeColor = new Color32(222, 97, 91, 255);  // #DE615B
        public Color popupOutlineColor = Color.white;
        public Color pauseTitleColor = new Color32(255, 200, 0, 255);    // #FFC800

        [Header("Character Select")]
        public Color p1Color = new Color(0.392f, 0.706f, 1.0f);
        public Color p2Color = new Color(1.0f, 0.471f, 0.471f);
        public Color bothColor = new Color(0.784f, 0.588f, 1.0f);
        public Color unselectedBorder = new Color(0.235f, 0.275f, 0.392f);
        public Color highlightGold = new Color(1.0f, 0.863f, 0.314f);
        public Color backDefault = new Color(0.302f, 0.169f, 0.122f);
        public Color backHover = new Color(0.576f, 0.298f, 0.188f);
        public Color createDogDefault = new Color(0.784f, 0.667f, 0.235f);
        public Color createDogHover = new Color(1.0f, 0.863f, 0.314f);

        [Header("Leash")]
        public Color normalRopeColor = new Color32(139, 90, 43, 255);
        public Color extendedRopeColor = new Color32(80, 200, 80, 255);
        public Color yankedRopeColor = new Color32(200, 80, 80, 255);
        public Color shadowColor = new Color32(50, 30, 20, 255);
        public Color normalHighlightColor = new Color32(101, 67, 33, 255);
        public Color extendedHighlightColor = new Color32(60, 180, 60, 255);
        public Color yankedHighlightColor = new Color32(180, 60, 60, 255);

        [Header("Settings")]
        public Color sliderBackground = Color.white;  // #DCA556

        [Header("Avatar Upload")]
        public Color avatarBgColor = new Color32(20, 30, 60, 255);
        public Color avatarAccentGold = new Color32(255, 200, 80, 255);
        public Color avatarInputActiveBg = new Color32(60, 80, 130, 255);
        public Color avatarInputInactiveBg = new Color32(40, 50, 80, 255);
        public Color avatarButtonNormal = new Color32(80, 140, 80, 255);
        public Color avatarButtonHover = new Color32(100, 180, 100, 255);
        public Color avatarBackNormal = new Color32(100, 70, 60, 255);
        public Color avatarBackHover = new Color32(130, 100, 80, 255);
        public Color avatarSuccessColor = new Color32(100, 255, 100, 255);
        public Color avatarErrorColor = new Color32(255, 100, 100, 255);
        public Color avatarProgressFill = new Color32(100, 180, 255, 255);
        public Color avatarHintColor = new Color32(120, 120, 140, 255);
        public Color avatarDisabledBtn = new Color32(60, 60, 80, 255);
        public Color avatarApiLinkColor = new Color32(150, 200, 255, 255);

        [Header("Avatar Showcase")]
        public Color showcaseOverlayColor = new Color32(0, 0, 0, 120);
        public Color showcaseAccentGold = new Color32(255, 200, 60, 255);
        public Color showcaseSubtitleColor = new Color32(180, 160, 120, 255);
        public Color showcaseStatBarBg = new Color32(40, 35, 60, 255);
        public Color showcaseStatBarFill = new Color32(255, 200, 60, 255);
        public Color showcaseStatsPanelBg = new Color32(20, 15, 40, 180);
        public Color showcaseBackNormal = new Color32(147, 76, 48, 255);
        public Color showcaseBackHover = new Color32(200, 120, 70, 255);
        public Color showcaseFooterColor = new Color32(100, 80, 60, 255);

        [Header("Celebration")]
        public Color32[] festiveColors = new Color32[]
        {
            new(255, 80, 80, 255), new(80, 200, 255, 255), new(255, 220, 60, 255),
            new(120, 255, 120, 255), new(255, 140, 200, 255), new(180, 120, 255, 255),
            new(255, 160, 60, 255)
        };
    }
}
