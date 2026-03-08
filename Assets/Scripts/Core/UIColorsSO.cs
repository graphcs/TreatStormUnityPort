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
        public Color normalRopeColor = new Color(0.396f, 0.263f, 0.129f);
        public Color extendedRopeColor = new Color(0.235f, 0.706f, 0.235f);
        public Color yankedRopeColor = new Color(0.706f, 0.235f, 0.235f);
        public Color shadowColor = new Color(0.196f, 0.118f, 0.078f);

        [Header("Celebration")]
        public Color32[] festiveColors = new Color32[]
        {
            new(255, 80, 80, 255), new(80, 200, 255, 255), new(255, 220, 60, 255),
            new(120, 255, 120, 255), new(255, 140, 200, 255), new(180, 120, 255, 255),
            new(255, 160, 60, 255)
        };
    }
}
