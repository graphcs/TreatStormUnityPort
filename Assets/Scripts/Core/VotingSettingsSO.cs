using System;
using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "VotingSettings", menuName = "SnackAttack/Voting Settings")]
    public class VotingSettingsSO : ScriptableObject
    {
        // ── Voting Timing ──
        [Header("Voting Timing")]
        public float votingDuration = 10f;
        public float cooldownDuration = 10f;
        public float winnerDisplayDuration = 3f;

        // ── Chat Simulator ──
        [Header("Chat Simulator")]
        public float autoVoteInterval = 2.0f;
        public float autoVoteVariance = 0.5f;
        public int maxVisibleMessages = 12;
        public float messageRowHeight = 16f;

        // ── Vote Effects ──
        [Header("Vote Effects")]
        public float extendCrossDistance = 150f;
        public float yankFlashDuration = 0.3f;
        public float treatDropScale = 1.5f;
        public float triviaSpeedMagnitude = 2.0f;
        public float triviaSpeedDuration = 5f;

        // ── Overlay ──
        [Header("Crowd Chaos Overlay")]
        public Color chaosTintColor = new Color(220f / 255f, 40f / 255f, 40f / 255f);
        public float chaosTintAlpha = 75f / 255f;
        public float pulseCycleMs = 500f;
        public float pulseAlphaMin = 0.85f;
        public float pulseAlphaMax = 1.0f;
        public float countdownScalePulseCycleMs = 400f;
        public float countdownScaleMin = 1.0f;
        public float countdownScaleMax = 1.08f;

        // ── Voting Meter ──
        [Header("Voting Meter")]
        public Vector2 meterPosition = new Vector2(900f, -105f);
        public Vector2 meterSize = new Vector2(300f, 85f);
        public Color meterBorderColor = new Color(0x4D / 255f, 0x2B / 255f, 0x1F / 255f);
        public Color[] barColors = new Color[]
        {
            new Color(81f / 255f, 180f / 255f, 71f / 255f),  // (81,180,71)
            new Color(221f / 255f, 68f / 255f, 61f / 255f),  // (221,68,61)
            new Color(80f / 255f, 160f / 255f, 220f / 255f), // (80,160,220)
            new Color(220f / 255f, 180f / 255f, 60f / 255f), // (220,180,60)
        };
        public Color[] barBgColors = new Color[]
        {
            new Color(185f / 255f, 231f / 255f, 199f / 255f), // (185,231,199)
            new Color(186f / 255f, 143f / 255f, 145f / 255f), // (186,143,145)
            new Color(180f / 255f, 210f / 255f, 240f / 255f), // (180,210,240)
            new Color(240f / 255f, 220f / 255f, 160f / 255f), // (240,220,160)
        };
        public float barHeight = 20f;
        public float barGap = 10f;

        // ── Chat Panel ──
        [Header("Chat Panel")]
        public Color chatBgColor = new Color(0x19 / 255f, 0x19 / 255f, 0x23 / 255f);
        public Color chatBorderColor = new Color(0x50 / 255f, 0x50 / 255f, 0x64 / 255f);
        public Color chatHeaderBgColor = new Color(0x28 / 255f, 0x28 / 255f, 0x37 / 255f);
        public float chatPanelWidth = 200f;
        public float chatHeaderHeight = 35f;

        // ── Trivia Questions ──
        [Header("Trivia")]
        public TriviaQuestion[] triviaQuestions = new TriviaQuestion[]
        {
            new TriviaQuestion("Who loves lasagna?", new[] { "Jazzy", "Biggie", "Prissy", "Snowy" }, 0),
            new TriviaQuestion("What falls from sky?", new[] { "Snacks", "Rain", "Cats", "Rocks" }, 0),
            new TriviaQuestion("Best pizza topping?", new[] { "Cheese", "Pineapple", "Anchovy", "Olives" }, 0),
            new TriviaQuestion("Game Name?", new[] { "SnackAttack", "DogRun", "EatFast", "Fetch Master" }, 0),
        };

        // ── Announcement Colors ──
        [Header("Announcement Colors")]
        public Color announceGreenColor = new Color(50f / 255f, 255f / 255f, 50f / 255f);       // (50,255,50) extend
        public Color announceRedColor = new Color(255f / 255f, 50f / 255f, 50f / 255f);         // (255,50,50) yank
        public Color announceLightBlueColor = new Color(100f / 255f, 200f / 255f, 255f / 255f); // (100,200,255) treat
        public Color announceChaosColor = new Color(255f / 255f, 100f / 255f, 100f / 255f);     // (255,100,100) chaos
        public Color announceTriviaCorrectColor = new Color(100f / 255f, 255f / 255f, 100f / 255f); // (100,255,100)
        public Color announceTriviaWrongColor = new Color(255f / 255f, 100f / 255f, 100f / 255f);   // (255,100,100)

        // ── Flash Effect ──
        [Header("Flash Effect")]
        public float flashDuration = 0.3f;
        public float flashAlpha = 150f / 255f;

        // ── Voting Meter Layout ──
        [Header("Voting Meter Layout")]
        public float barMargin = 20f;
        public float meterBarY = -45f;
        public float barLabelYOffset = 5f;
        public float barLabelHeight = 14f;
        public int   barLabelFontSize = 8;
        public int   barLabelTruncateLength = 8;
        public int   statusTruncateLength = 10;

        // ── Chat Layout ──
        [Header("Chat Layout")]
        public int   messageFontSize = 9;
        public int   messageXOffset = 4;
        public float buttonHeight = 28f;
        public float buttonGap = 4f;
        public int   buttonXOffset = 4;
        public float buttonBackgroundAlpha = 0.3f;
        public int   buttonLabelFontSize = 10;

        // ── Behavior ──
        [Header("Behavior")]
        public int   maxStoredMessages = 15;
        public float smartVoteThreshold = 0.6f;

        // ── Overlay Colors ──
        [Header("Overlay Colors")]
        public Color countdownTitleColor  = new Color(255f / 255f, 235f / 255f, 235f / 255f);
        public Color countdownNumberColor = new Color(255f / 255f, 110f / 255f, 110f / 255f);
        public Color liveTitleColor       = new Color(255f / 255f, 180f / 255f, 180f / 255f);
        public Color optionsTextColor     = new Color(255f / 255f, 230f / 255f, 200f / 255f);
        public float tintAlphaLerpSpeed   = 6f;
        public float maxTintAlpha         = 140f / 255f;

        // ── System Message Colors ──
        [Header("System Message Colors")]
        public Color autoToggleOnColor      = new Color(0.3f,         0.8f,         0.3f);
        public Color systemAutoOnColor      = new Color(200f / 255f,  200f / 255f,  100f / 255f);
        public Color systemAutoOffColor     = new Color(150f / 255f,  150f / 255f,  150f / 255f);
        public Color systemIncomingColor    = new Color(255f / 255f,  120f / 255f,  120f / 255f);
        public Color systemTriviaColor      = new Color(255f / 255f,  200f / 255f,  255f / 255f);
        public Color systemLiveColor        = new Color(255f / 255f,  120f / 255f,  120f / 255f);
        public Color systemRoundInfoColor   = new Color(255f / 255f,  255f / 255f,  100f / 255f);
        public Color actionExtendColor      = new Color(50f  / 255f,  200f / 255f,  50f  / 255f);
        public Color actionExtendColor2     = new Color(100f / 255f,  255f / 255f,  100f / 255f);
        public Color actionYankColor        = new Color(200f / 255f,  50f  / 255f,  50f  / 255f);
        public Color triviaCorrectChatColor = new Color(100f / 255f,  255f / 255f,  100f / 255f);
        public Color triviaWrongChatColor   = new Color(200f / 255f,  100f / 255f,  100f / 255f);
    }

    [Serializable]
    public struct TriviaQuestion
    {
        public string question;
        public string[] options;
        public int correctIndex;
        public string correctAnswer;

        public TriviaQuestion(string question, string[] options, int correctIndex)
        {
            this.question = question;
            this.options = options;
            this.correctIndex = correctIndex;
            this.correctAnswer = options != null && correctIndex >= 0 && correctIndex < options.Length
                ? options[correctIndex] : "";
        }
    }
}
