using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Screens;

namespace SnackAttack.Editor
{
    public static class SetupGameOverScreen
    {
        [MenuItem("SnackAttack/Setup Game Over Screen")]
        public static void Setup()
        {
            // Find GameOverPanel
            var panel = GameObject.Find("GameOverPanel");
            if (panel == null)
            {
                Debug.LogError("GameOverPanel not found in scene!");
                return;
            }

            // Load SO assets for layout/colors
            var layoutSO = AssetDatabase.LoadAssetAtPath<UILayoutSO>("Assets/ScriptableObjects/Config/UILayout.asset");
            var colorsSO = AssetDatabase.LoadAssetAtPath<UIColorsSO>("Assets/ScriptableObjects/Config/UIColors.asset");

            // Fallback values if SOs not found
            float winnerNameY = layoutSO != null ? layoutSO.winnerNameY : -180f;
            Vector2 winnerNameSize = layoutSO != null ? layoutSO.winnerNameSize : new Vector2(800f, 100f);
            float winnerNameFontSize = layoutSO != null ? layoutSO.winnerNameFontSize : 80f;
            float winsImageY = layoutSO != null ? layoutSO.winsImageY : -230f;
            float menuBarY = layoutSO != null ? layoutSO.menuBarY : -330f;
            float menuBarWidth = layoutSO != null ? layoutSO.menuBarWidth : 912f;
            float roundsTextY = layoutSO != null ? layoutSO.roundsTextY : -330f;
            Vector2 roundsTextSize = layoutSO != null ? layoutSO.roundsTextSize : new Vector2(800f, 50f);
            float roundsTextFontSize = layoutSO != null ? layoutSO.roundsTextFontSize : 28f;
            float scoreBoxP1X = layoutSO != null ? layoutSO.scoreBoxP1X : 72f;
            float scoreBoxP2X = layoutSO != null ? layoutSO.scoreBoxP2X : 565f;
            float scoreBoxY = layoutSO != null ? layoutSO.scoreBoxY : -350f;
            Vector2 scoreBoxSize = layoutSO != null ? layoutSO.scoreBoxSize : new Vector2(563f, 521f);
            float scoreNameY = layoutSO != null ? layoutSO.scoreNameY : -198f;
            float scoreNameFontSize = layoutSO != null ? layoutSO.scoreNameFontSize : 43f;
            float scoreLabelY = layoutSO != null ? layoutSO.scoreLabelY : -260f;
            float scoreLabelFontSize = layoutSO != null ? layoutSO.scoreLabelFontSize : 24f;
            float scoreValueY = layoutSO != null ? layoutSO.scoreValueY : -323f;
            float scoreValueFontSize = layoutSO != null ? layoutSO.scoreValueFontSize : 61f;
            float playAgainY = layoutSO != null ? layoutSO.playAgainY : -850f;
            float mainMenuY = layoutSO != null ? layoutSO.mainMenuY : -910f;
            float menuOptionFontSize = layoutSO != null ? layoutSO.menuOptionFontSize : 28f;
            Vector2 selectIndicatorSize = layoutSO != null ? layoutSO.selectIndicatorSize : new Vector2(36f, 30f);
            float winsImageScale = layoutSO != null ? layoutSO.winsImageScale : 0.6f;

            Color winnerColor = colorsSO != null ? colorsSO.winnerColor : new Color32(147, 76, 48, 255);
            Color roundsColor = colorsSO != null ? colorsSO.roundsColor : new Color32(77, 43, 31, 255);

            // Ensure RectTransform on panel
            var panelRect = EnsureRectTransform(panel);
            SetStretchAll(panelRect);

            // Load assets
            var winScreenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Win screen.png");
            var menuSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Menu square.png");
            var winsSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Wins!.png");
            var menuBarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Menu bar.png");
            var selectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Select.png");
            var daydreamFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Daydream SDF.asset");

            if (winScreenSprite == null) Debug.LogWarning("Win screen.png not found");
            if (menuSquareSprite == null) Debug.LogWarning("Menu square.png not found");
            if (winsSprite == null) Debug.LogWarning("Wins!.png not found");
            if (menuBarSprite == null) Debug.LogWarning("Menu bar.png not found");
            if (selectSprite == null) Debug.LogWarning("Select.png not found");
            if (daydreamFont == null) Debug.LogWarning("Daydream SDF.asset not found");

            // --- GO_Background ---
            var bgGO = panel.transform.Find("GO_Background")?.gameObject;
            if (bgGO != null)
            {
                var bgRect = EnsureRectTransform(bgGO);
                SetStretchAll(bgRect);
                var bgImg = bgGO.GetComponent<Image>();
                if (bgImg == null) bgImg = bgGO.AddComponent<Image>();
                bgImg.sprite = winScreenSprite;
                bgImg.raycastTarget = true;
            }

            // --- GO_BalloonContainer ---
            var balloonGO = panel.transform.Find("GO_BalloonContainer")?.gameObject;
            if (balloonGO != null)
            {
                var r = EnsureRectTransform(balloonGO);
                SetStretchAll(r);
            }

            // --- GO_WinnerName ---
            var winnerGO = panel.transform.Find("GO_WinnerName")?.gameObject;
            TMP_Text winnerText = null;
            if (winnerGO != null)
            {
                var r = EnsureRectTransform(winnerGO);
                SetTopCenter(r, 0, winnerNameY, winnerNameSize.x, winnerNameSize.y);
                winnerText = winnerGO.GetComponent<TMP_Text>();
                if (winnerText != null)
                {
                    winnerText.font = daydreamFont;
                    winnerText.fontSize = winnerNameFontSize;
                    winnerText.color = winnerColor;
                    winnerText.alignment = TextAlignmentOptions.Center;
                    winnerText.raycastTarget = false;
                    winnerText.text = "Winner Name";
                }
            }

            // --- GO_WinsImage ---
            var winsGO = panel.transform.Find("GO_WinsImage")?.gameObject;
            Image winsImg = null;
            if (winsGO != null)
            {
                var r = EnsureRectTransform(winsGO);
                SetTopCenter(r, 0, winsImageY, 200, 80);
                winsImg = winsGO.GetComponent<Image>();
                if (winsImg == null) winsImg = winsGO.AddComponent<Image>();
                winsImg.sprite = winsSprite;
                winsImg.preserveAspect = true;
                winsImg.raycastTarget = false;
                if (winsSprite != null)
                {
                    float nativeW = winsSprite.rect.width;
                    float nativeH = winsSprite.rect.height;
                    r.sizeDelta = new Vector2(nativeW * winsImageScale, nativeH * winsImageScale);
                }
            }

            // --- GO_MenuBar ---
            var menuBarGO = panel.transform.Find("GO_MenuBar")?.gameObject;
            Image menuBarImg = null;
            if (menuBarGO != null)
            {
                var r = EnsureRectTransform(menuBarGO);
                SetTopCenter(r, 0, menuBarY, menuBarWidth, 80);
                menuBarImg = menuBarGO.GetComponent<Image>();
                if (menuBarImg == null) menuBarImg = menuBarGO.AddComponent<Image>();
                menuBarImg.sprite = menuBarSprite;
                menuBarImg.preserveAspect = true;
                menuBarImg.raycastTarget = false;
                if (menuBarSprite != null)
                {
                    float aspect = menuBarSprite.rect.width / menuBarSprite.rect.height;
                    r.sizeDelta = new Vector2(menuBarWidth, menuBarWidth / aspect);
                }
            }

            // --- GO_RoundsText ---
            var roundsGO = panel.transform.Find("GO_RoundsText")?.gameObject;
            TMP_Text roundsText = null;
            if (roundsGO != null)
            {
                var r = EnsureRectTransform(roundsGO);
                SetTopCenter(r, 0, roundsTextY, roundsTextSize.x, roundsTextSize.y);
                roundsText = roundsGO.GetComponent<TMP_Text>();
                if (roundsText != null)
                {
                    roundsText.font = daydreamFont;
                    roundsText.fontSize = roundsTextFontSize;
                    roundsText.color = roundsColor;
                    roundsText.alignment = TextAlignmentOptions.Center;
                    roundsText.raycastTarget = false;
                    roundsText.text = "P1  2  vs  1  P2";
                }
            }

            // --- GO_P1ScoreBox ---
            var p1BoxGO = panel.transform.Find("GO_P1ScoreBox")?.gameObject;
            RectTransform p1BoxRect = null;
            if (p1BoxGO != null)
            {
                p1BoxRect = EnsureRectTransform(p1BoxGO);
                p1BoxRect.anchorMin = new Vector2(0, 1);
                p1BoxRect.anchorMax = new Vector2(0, 1);
                p1BoxRect.pivot = new Vector2(0, 1);
                p1BoxRect.anchoredPosition = new Vector2(scoreBoxP1X, scoreBoxY);
                p1BoxRect.sizeDelta = scoreBoxSize;
                var img = p1BoxGO.GetComponent<Image>();
                if (img == null) img = p1BoxGO.AddComponent<Image>();
                img.sprite = menuSquareSprite;
                img.preserveAspect = false;
                img.raycastTarget = false;
            }

            // P1 children
            SetupScoreCardChild(p1BoxGO, "GO_P1Name", daydreamFont, winnerColor, scoreNameFontSize, scoreNameY, 500, 60, "Dog Name");
            SetupScoreCardChild(p1BoxGO, "GO_P1ScoreLabel", daydreamFont, winnerColor, scoreLabelFontSize, scoreLabelY, 300, 40, "score");
            SetupScoreCardChild(p1BoxGO, "GO_P1ScoreValue", daydreamFont, winnerColor, scoreValueFontSize, scoreValueY, 400, 80, "0");

            // --- GO_P2ScoreBox ---
            var p2BoxGO = panel.transform.Find("GO_P2ScoreBox")?.gameObject;
            RectTransform p2BoxRect = null;
            if (p2BoxGO != null)
            {
                p2BoxRect = EnsureRectTransform(p2BoxGO);
                p2BoxRect.anchorMin = new Vector2(0, 1);
                p2BoxRect.anchorMax = new Vector2(0, 1);
                p2BoxRect.pivot = new Vector2(0, 1);
                p2BoxRect.anchoredPosition = new Vector2(scoreBoxP2X, scoreBoxY);
                p2BoxRect.sizeDelta = scoreBoxSize;
                var img = p2BoxGO.GetComponent<Image>();
                if (img == null) img = p2BoxGO.AddComponent<Image>();
                img.sprite = menuSquareSprite;
                img.preserveAspect = false;
                img.raycastTarget = false;
            }

            // P2 children
            SetupScoreCardChild(p2BoxGO, "GO_P2Name", daydreamFont, winnerColor, scoreNameFontSize, scoreNameY, 500, 60, "Dog Name");
            SetupScoreCardChild(p2BoxGO, "GO_P2ScoreLabel", daydreamFont, winnerColor, scoreLabelFontSize, scoreLabelY, 300, 40, "score");
            SetupScoreCardChild(p2BoxGO, "GO_P2ScoreValue", daydreamFont, winnerColor, scoreValueFontSize, scoreValueY, 400, 80, "0");

            // --- GO_PlayAgain ---
            var playAgainGO = panel.transform.Find("GO_PlayAgain")?.gameObject;
            TMP_Text playAgainText = null;
            if (playAgainGO != null)
            {
                var r = EnsureRectTransform(playAgainGO);
                SetTopCenter(r, 0, playAgainY, 400, 50);
                playAgainText = playAgainGO.GetComponent<TMP_Text>();
                if (playAgainText != null)
                {
                    playAgainText.font = daydreamFont;
                    playAgainText.fontSize = menuOptionFontSize;
                    playAgainText.color = roundsColor;
                    playAgainText.alignment = TextAlignmentOptions.Center;
                    playAgainText.raycastTarget = true;
                    playAgainText.text = "Play Again";
                }
            }

            // --- GO_MainMenu ---
            var mainMenuGO = panel.transform.Find("GO_MainMenu")?.gameObject;
            TMP_Text mainMenuText = null;
            if (mainMenuGO != null)
            {
                var r = EnsureRectTransform(mainMenuGO);
                SetTopCenter(r, 0, mainMenuY, 400, 50);
                mainMenuText = mainMenuGO.GetComponent<TMP_Text>();
                if (mainMenuText != null)
                {
                    mainMenuText.font = daydreamFont;
                    mainMenuText.fontSize = menuOptionFontSize;
                    mainMenuText.color = roundsColor;
                    mainMenuText.alignment = TextAlignmentOptions.Center;
                    mainMenuText.raycastTarget = true;
                    mainMenuText.text = "Main Menu";
                }
            }

            // --- GO_SelectIndicator ---
            var selectGO = panel.transform.Find("GO_SelectIndicator")?.gameObject;
            Image selectImg = null;
            if (selectGO != null)
            {
                var r = EnsureRectTransform(selectGO);
                SetTopCenter(r, 0, playAgainY, selectIndicatorSize.x, selectIndicatorSize.y);
                selectImg = selectGO.GetComponent<Image>();
                if (selectImg == null) selectImg = selectGO.AddComponent<Image>();
                selectImg.sprite = selectSprite;
                selectImg.preserveAspect = true;
                selectImg.raycastTarget = false;
            }

            // --- GO_ConfettiContainer ---
            var confettiGO = panel.transform.Find("GO_ConfettiContainer")?.gameObject;
            if (confettiGO != null)
            {
                var r = EnsureRectTransform(confettiGO);
                SetStretchAll(r);
            }

            // --- Wire serialized fields on GameOverScreen ---
            var screen = panel.GetComponent<GameOverScreen>();
            if (screen != null)
            {
                var so = new SerializedObject(screen);

                // Set screenState to GameOver (enum index 7)
                var screenStateProp = so.FindProperty("screenState");
                if (screenStateProp != null)
                    screenStateProp.enumValueIndex = (int)GameState.GameOver;

                SetRef(so, "_background", bgGO?.GetComponent<Image>());
                SetRef(so, "_winnerName", winnerText);
                SetRef(so, "_winsImage", winsImg);
                SetRef(so, "_menuBar", menuBarImg);
                SetRef(so, "_roundsText", roundsText);
                SetRef(so, "_p1ScoreBox", p1BoxRect);
                SetRef(so, "_p1Name", p1BoxGO?.transform.Find("GO_P1Name")?.GetComponent<TMP_Text>());
                SetRef(so, "_p1ScoreLabel", p1BoxGO?.transform.Find("GO_P1ScoreLabel")?.GetComponent<TMP_Text>());
                SetRef(so, "_p1ScoreValue", p1BoxGO?.transform.Find("GO_P1ScoreValue")?.GetComponent<TMP_Text>());
                SetRef(so, "_p2ScoreBox", p2BoxRect);
                SetRef(so, "_p2Name", p2BoxGO?.transform.Find("GO_P2Name")?.GetComponent<TMP_Text>());
                SetRef(so, "_p2ScoreLabel", p2BoxGO?.transform.Find("GO_P2ScoreLabel")?.GetComponent<TMP_Text>());
                SetRef(so, "_p2ScoreValue", p2BoxGO?.transform.Find("GO_P2ScoreValue")?.GetComponent<TMP_Text>());
                SetRef(so, "_playAgainText", playAgainText);
                SetRef(so, "_mainMenuText", mainMenuText);
                SetRef(so, "_selectIndicator", selectImg);
                SetRef(so, "_balloonContainer", balloonGO?.GetComponent<RectTransform>());
                SetRef(so, "_confettiContainer", confettiGO?.GetComponent<RectTransform>());

                so.ApplyModifiedProperties();
            }

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("GameOverScreen setup complete!");
        }

        private static RectTransform EnsureRectTransform(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }
            return rect;
        }

        private static void SetStretchAll(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        private static void SetTopCenter(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = new Vector2(0.5f, 1f);
            r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void SetupScoreCardChild(GameObject parent, string childName,
            TMP_FontAsset font, Color color, float fontSize, float y, float w, float h, string defaultText)
        {
            if (parent == null) return;
            var child = parent.transform.Find(childName)?.gameObject;
            if (child == null) return;

            var r = EnsureRectTransform(child);
            r.anchorMin = new Vector2(0.5f, 1f);
            r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0, y);
            r.sizeDelta = new Vector2(w, h);

            var tmp = child.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.font = font;
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                tmp.text = defaultText;
            }
        }

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && value != null)
                prop.objectReferenceValue = value;
        }
    }
}
