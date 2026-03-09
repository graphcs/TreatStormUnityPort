using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Screens;

namespace SnackAttack.Editor
{
    public static class SetupAvatarShowcase
    {
        [MenuItem("SnackAttack/Setup Avatar Showcase")]
        public static void Setup()
        {
            var uiCanvas = GameObject.Find("UICanvas");
            if (uiCanvas == null)
            {
                Debug.LogError("UICanvas not found in scene!");
                return;
            }

            var colorsSO = AssetDatabase.LoadAssetAtPath<UIColorsSO>("Assets/ScriptableObjects/Config/UIColors.asset");
            var daydreamFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Daydream SDF.asset");
            var selectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Select.png");
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Choose your dog background.png");

            Color overlayColor = colorsSO != null ? colorsSO.showcaseOverlayColor : new Color32(0, 0, 0, 120);
            Color gold = colorsSO != null ? colorsSO.showcaseAccentGold : new Color32(255, 200, 60, 255);
            Color subtitle = colorsSO != null ? colorsSO.showcaseSubtitleColor : new Color32(180, 160, 120, 255);
            Color statBarBg = colorsSO != null ? colorsSO.showcaseStatBarBg : new Color32(40, 35, 60, 255);
            Color statBarFill = colorsSO != null ? colorsSO.showcaseStatBarFill : new Color32(255, 200, 60, 255);
            Color panelBg = colorsSO != null ? colorsSO.showcaseStatsPanelBg : new Color32(20, 15, 40, 180);
            Color backNormal = colorsSO != null ? colorsSO.showcaseBackNormal : new Color32(147, 76, 48, 255);
            Color footerColor = colorsSO != null ? colorsSO.showcaseFooterColor : new Color32(100, 80, 60, 255);

            // Panel
            var panel = FindOrCreate(uiCanvas.transform, "AvatarShowcasePanel");
            var panelRect = EnsureRectTransform(panel);
            SetStretchAll(panelRect);
            EnsureComponent<CanvasGroup>(panel);
            var screen = EnsureComponent<AvatarShowcaseScreen>(panel);

            // Background
            var bgGO = FindOrCreate(panel.transform, "AS_Background");
            SetStretchAll(EnsureRectTransform(bgGO));
            var bgImg = EnsureComponent<Image>(bgGO);
            bgImg.sprite = bgSprite;
            bgImg.raycastTarget = true;

            // Dark overlay
            var overlayGO = FindOrCreate(panel.transform, "AS_DarkOverlay");
            SetStretchAll(EnsureRectTransform(overlayGO));
            var overlayImg = EnsureComponent<Image>(overlayGO);
            overlayImg.color = overlayColor;
            overlayImg.raycastTarget = false;

            // Glow effect
            var glowGO = FindOrCreate(panel.transform, "AS_GlowEffect");
            SetTopCenter(EnsureRectTransform(glowGO), 0, -280, 440, 440);
            var glowImg = EnsureComponent<Image>(glowGO);
            glowImg.color = new Color(gold.r, gold.g, gold.b, 0.3f);
            glowImg.raycastTarget = false;

            // Name text (above portrait)
            var nameGO = FindOrCreate(panel.transform, "AS_NameText");
            SetTopCenter(EnsureRectTransform(nameGO), 0, -60, 800, 70);
            var nameTmp = SetupTMP(nameGO, daydreamFont, 52, gold, "CHARACTER NAME");

            // Portrait
            var portraitGO = FindOrCreate(panel.transform, "AS_Portrait");
            SetTopCenter(EnsureRectTransform(portraitGO), 0, -280, 280, 280);
            var portraitImg = EnsureComponent<Image>(portraitGO);
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;
            var portraitOutline = EnsureComponent<Outline>(portraitGO);
            portraitOutline.effectColor = gold;
            portraitOutline.effectDistance = new Vector2(4, -4);

            // Breed text
            var breedGO = FindOrCreate(panel.transform, "AS_BreedText");
            SetTopCenter(EnsureRectTransform(breedGO), 0, -440, 500, 30);
            var breedTmp = SetupTMP(breedGO, daydreamFont, 16, subtitle, "Breed");

            // Stars
            var starsGO = FindOrCreate(panel.transform, "AS_StarsText");
            SetTopCenter(EnsureRectTransform(starsGO), 0, -480, 300, 30);
            var starsTmp = SetupTMP(starsGO, daydreamFont, 22, gold, "\u2605  \u2605  \u2605  \u2605  \u2605");

            // Stats panel
            var statsPanelGO = FindOrCreate(panel.transform, "AS_StatsPanel");
            SetTopCenter(EnsureRectTransform(statsPanelGO), 0, -580, 500, 200);
            var statsPanelImg = EnsureComponent<Image>(statsPanelGO);
            statsPanelImg.color = panelBg;
            var statsPanelOutline = EnsureComponent<Outline>(statsPanelGO);
            statsPanelOutline.effectColor = gold;
            statsPanelOutline.effectDistance = new Vector2(2, -2);

            // Stats (4 rows)
            string[] statNames = { "Speed", "Agility", "Appetite", "Charm" };
            TMP_Text[] statLabelsArr = new TMP_Text[4];
            Image[] statBarBGsArr = new Image[4];
            Image[] statBarFillsArr = new Image[4];
            TMP_Text[] statPctsArr = new TMP_Text[4];

            for (int i = 0; i < 4; i++)
            {
                float y = -20 - i * 45;

                var labelGO = FindOrCreate(statsPanelGO.transform, $"AS_Stat{i}_Label");
                var labelRect = EnsureRectTransform(labelGO);
                labelRect.anchorMin = new Vector2(0, 1);
                labelRect.anchorMax = new Vector2(0, 1);
                labelRect.pivot = new Vector2(0, 0.5f);
                labelRect.anchoredPosition = new Vector2(20, y);
                labelRect.sizeDelta = new Vector2(80, 30);
                statLabelsArr[i] = SetupTMP(labelGO, daydreamFont, 16, Color.white, statNames[i]);
                statLabelsArr[i].alignment = TextAlignmentOptions.MidlineLeft;

                var barBGGO = FindOrCreate(statsPanelGO.transform, $"AS_Stat{i}_BarBG");
                var barBGRect = EnsureRectTransform(barBGGO);
                barBGRect.anchorMin = new Vector2(0, 1);
                barBGRect.anchorMax = new Vector2(0, 1);
                barBGRect.pivot = new Vector2(0, 0.5f);
                barBGRect.anchoredPosition = new Vector2(110, y);
                barBGRect.sizeDelta = new Vector2(300, 18);
                statBarBGsArr[i] = EnsureComponent<Image>(barBGGO);
                statBarBGsArr[i].color = statBarBg;
                statBarBGsArr[i].raycastTarget = false;

                var barFillGO = FindOrCreate(barBGGO.transform, $"AS_Stat{i}_BarFill");
                SetStretchAll(EnsureRectTransform(barFillGO));
                statBarFillsArr[i] = EnsureComponent<Image>(barFillGO);
                statBarFillsArr[i].color = statBarFill;
                statBarFillsArr[i].type = Image.Type.Filled;
                statBarFillsArr[i].fillMethod = Image.FillMethod.Horizontal;
                statBarFillsArr[i].fillAmount = 0f;
                statBarFillsArr[i].raycastTarget = false;

                var pctGO = FindOrCreate(statsPanelGO.transform, $"AS_Stat{i}_Pct");
                var pctRect = EnsureRectTransform(pctGO);
                pctRect.anchorMin = new Vector2(0, 1);
                pctRect.anchorMax = new Vector2(0, 1);
                pctRect.pivot = new Vector2(0, 0.5f);
                pctRect.anchoredPosition = new Vector2(420, y);
                pctRect.sizeDelta = new Vector2(60, 25);
                statPctsArr[i] = SetupTMP(pctGO, daydreamFont, 12, subtitle, "0%");
                statPctsArr[i].alignment = TextAlignmentOptions.MidlineLeft;
            }

            // Run preview
            var runGO = FindOrCreate(panel.transform, "AS_RunPreview");
            SetTopCenter(EnsureRectTransform(runGO), -80, -750, 96, 96);
            var runImg = EnsureComponent<Image>(runGO);
            runImg.preserveAspect = true;
            runImg.raycastTarget = false;

            var runLabelGO = FindOrCreate(panel.transform, "AS_RunLabel");
            SetTopCenter(EnsureRectTransform(runLabelGO), -80, -810, 60, 20);
            SetupTMP(runLabelGO, daydreamFont, 12, subtitle, "RUN");

            // Eat preview
            var eatGO = FindOrCreate(panel.transform, "AS_EatPreview");
            SetTopCenter(EnsureRectTransform(eatGO), 80, -750, 96, 96);
            var eatImg = EnsureComponent<Image>(eatGO);
            eatImg.preserveAspect = true;
            eatImg.raycastTarget = false;

            var eatLabelGO = FindOrCreate(panel.transform, "AS_EatLabel");
            SetTopCenter(EnsureRectTransform(eatLabelGO), 80, -810, 60, 20);
            SetupTMP(eatLabelGO, daydreamFont, 12, subtitle, "EAT");

            // Back text
            var backGO = FindOrCreate(panel.transform, "AS_BackText");
            SetTopCenter(EnsureRectTransform(backGO), 0, -880, 200, 40);
            var backTmp = SetupTMP(backGO, daydreamFont, 22, backNormal, "Back");
            backTmp.raycastTarget = true;

            // Select indicator
            var selectGO = FindOrCreate(panel.transform, "AS_SelectIndicator");
            SetTopCenter(EnsureRectTransform(selectGO), -80, -880, 48, 40);
            var selectImg = EnsureComponent<Image>(selectGO);
            selectImg.sprite = selectSprite;
            selectImg.preserveAspect = true;
            selectImg.raycastTarget = false;
            selectImg.enabled = false;

            // Footer
            var footerGO = FindOrCreate(panel.transform, "AS_Footer");
            SetTopCenter(EnsureRectTransform(footerGO), 0, -940, 500, 25);
            SetupTMP(footerGO, daydreamFont, 12, footerColor, "Press ESC or Enter to go back");

            // ========== WIRE SERIALIZED FIELDS ==========
            var so = new SerializedObject(screen);

            var screenStateProp = so.FindProperty("screenState");
            if (screenStateProp != null)
                screenStateProp.enumValueIndex = (int)GameState.AvatarShowcase;

            SetRef(so, "_background", bgImg);
            SetRef(so, "_darkOverlay", overlayImg);
            SetRef(so, "_glowEffect", glowImg);
            SetRef(so, "_portrait", portraitImg);
            SetRef(so, "_nameText", nameTmp);
            SetRef(so, "_breedText", breedTmp);
            SetRef(so, "_starsText", starsTmp);
            SetRef(so, "_statsPanelBg", statsPanelImg);

            // Wire stat arrays
            SetArrayRef(so, "_statLabels", new Object[] { statLabelsArr[0], statLabelsArr[1], statLabelsArr[2], statLabelsArr[3] });
            SetArrayRef(so, "_statBarBGs", new Object[] { statBarBGsArr[0], statBarBGsArr[1], statBarBGsArr[2], statBarBGsArr[3] });
            SetArrayRef(so, "_statBarFills", new Object[] { statBarFillsArr[0], statBarFillsArr[1], statBarFillsArr[2], statBarFillsArr[3] });
            SetArrayRef(so, "_statPcts", new Object[] { statPctsArr[0], statPctsArr[1], statPctsArr[2], statPctsArr[3] });

            SetRef(so, "_runPreview", runImg);
            SetRef(so, "_runLabel", runLabelGO.GetComponent<TMP_Text>());
            SetRef(so, "_eatPreview", eatImg);
            SetRef(so, "_eatLabel", eatLabelGO.GetComponent<TMP_Text>());
            SetRef(so, "_backText", backTmp);
            SetRef(so, "_selectIndicator", selectImg);
            SetRef(so, "_footer", footerGO.GetComponent<TMP_Text>());

            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("AvatarShowcaseScreen setup complete!");
        }

        private static TMP_Text SetupTMP(GameObject go, TMP_FontAsset font, float size, Color color, string text)
        {
            var tmp = EnsureComponent<TextMeshProUGUI>(go);
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject FindOrCreate(Transform parent, string name)
        {
            var existing = parent.Find(name)?.gameObject;
            if (existing != null) return existing;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform EnsureRectTransform(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            return rect;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
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

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && value != null)
                prop.objectReferenceValue = value;
        }

        private static void SetArrayRef(SerializedObject so, string propName, Object[] values)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                if (elem != null && values[i] != null)
                    elem.objectReferenceValue = values[i];
            }
        }
    }
}
