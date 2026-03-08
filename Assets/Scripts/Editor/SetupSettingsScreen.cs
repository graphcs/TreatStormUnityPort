using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Screens;

namespace SnackAttack.Editor
{
    public static class SetupSettingsScreen
    {
        [MenuItem("SnackAttack/Setup Settings Screen")]
        public static void Setup()
        {
            // Find or create SettingsPanel under UICanvas
            var uiCanvas = GameObject.Find("UICanvas");
            if (uiCanvas == null)
            {
                Debug.LogError("UICanvas not found in scene!");
                return;
            }

            var panel = uiCanvas.transform.Find("SettingsPanel")?.gameObject;
            if (panel == null)
            {
                panel = new GameObject("SettingsPanel");
                panel.transform.SetParent(uiCanvas.transform, false);
            }

            // Load SOs
            var layoutSO = AssetDatabase.LoadAssetAtPath<UILayoutSO>("Assets/ScriptableObjects/Config/UILayout.asset");
            var colorsSO = AssetDatabase.LoadAssetAtPath<UIColorsSO>("Assets/ScriptableObjects/Config/UIColors.asset");

            // Layout values
            float containerY = layoutSO != null ? layoutSO.settingsContainerY : -220f;
            Vector2 containerSize = layoutSO != null ? layoutSO.settingsContainerSize : new Vector2(960f, 700f);
            float titleY = layoutSO != null ? layoutSO.settingsTitleY : -120f;
            Vector2 titleSize = layoutSO != null ? layoutSO.settingsTitleSize : new Vector2(720f, 120f);
            float itemStartY = layoutSO != null ? layoutSO.settingsItemStartY : -404f;
            float itemSpacing = layoutSO != null ? layoutSO.settingsItemSpacing : 70f;
            float labelX = layoutSO != null ? layoutSO.settingsLabelX : 264f;
            float valueX = layoutSO != null ? layoutSO.settingsValueX : 744f;
            float sliderWidth = layoutSO != null ? layoutSO.settingsSliderWidth : 216f;
            float sliderHeight = layoutSO != null ? layoutSO.settingsSliderHeight : 20f;
            float percentOffsetX = layoutSO != null ? layoutSO.settingsPercentOffsetX : 40f;
            float backY = layoutSO != null ? layoutSO.settingsBackY : -880f;
            float footerY = layoutSO != null ? layoutSO.settingsFooterY : -960f;
            float itemFontSize = layoutSO != null ? layoutSO.settingsItemFontSize : 28f;
            float backFontSize = layoutSO != null ? layoutSO.settingsBackFontSize : 32f;
            float footerFontSize = layoutSO != null ? layoutSO.settingsFooterFontSize : 18f;
            float percentFontSize = layoutSO != null ? layoutSO.settingsPercentFontSize : 18f;
            Vector2 selectSize = layoutSO != null ? layoutSO.settingsSelectSize : new Vector2(34.56f, 28.8f);

            // Colors
            Color menuNormal = colorsSO != null ? colorsSO.menuNormal : new Color32(77, 43, 31, 255);
            Color onColor = colorsSO != null ? colorsSO.popupPositiveColor : new Color32(81, 180, 71, 255);
            Color sliderBgColor = colorsSO != null ? colorsSO.sliderBackground : new Color32(220, 165, 86, 255);

            // Load sprites
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Settings background.png");
            var containerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Menu tall.png");
            var titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/settings text.png");
            var selectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Select.png");
            var daydreamFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Daydream SDF.asset");

            if (bgSprite == null) Debug.LogWarning("Settings background.png not found");
            if (containerSprite == null) Debug.LogWarning("Menu tall.png not found");
            if (titleSprite == null) Debug.LogWarning("settings text.png not found");
            if (selectSprite == null) Debug.LogWarning("Select.png not found");
            if (daydreamFont == null) Debug.LogWarning("Daydream SDF.asset not found");

            // Ensure panel setup
            var panelRect = EnsureRectTransform(panel);
            SetStretchAll(panelRect);
            EnsureComponent<CanvasGroup>(panel);
            var screen = EnsureComponent<SettingsScreen>(panel);

            // --- ST_Background ---
            var bgGO = FindOrCreate(panel.transform, "ST_Background");
            var bgRect = EnsureRectTransform(bgGO);
            SetStretchAll(bgRect);
            var bgImg = EnsureComponent<Image>(bgGO);
            bgImg.sprite = bgSprite;
            bgImg.raycastTarget = true;

            // --- ST_Container ---
            var contGO = FindOrCreate(panel.transform, "ST_Container");
            var contRect = EnsureRectTransform(contGO);
            SetTopCenter(contRect, 0, containerY, containerSize.x, containerSize.y);
            var contImg = EnsureComponent<Image>(contGO);
            contImg.sprite = containerSprite;
            contImg.preserveAspect = true;
            contImg.raycastTarget = false;

            // --- ST_TitleImage ---
            var titleGO = FindOrCreate(panel.transform, "ST_TitleImage");
            var titleRect = EnsureRectTransform(titleGO);
            SetTopCenter(titleRect, 0, titleY, titleSize.x, titleSize.y);
            var titleImg = EnsureComponent<Image>(titleGO);
            titleImg.sprite = titleSprite;
            titleImg.preserveAspect = true;
            titleImg.raycastTarget = false;

            // --- Toggle Items (0, 1) ---
            string[] toggleNames = { "Music", "Sound Effects" };
            TMP_Text[] toggleLabels = new TMP_Text[2];
            TMP_Text[] toggleValues = new TMP_Text[2];

            for (int i = 0; i < 2; i++)
            {
                float y = itemStartY - i * itemSpacing;
                var itemGO = FindOrCreate(panel.transform, $"ST_Item{i}");
                var itemRect = EnsureRectTransform(itemGO);
                SetTopLeft(itemRect, 0, y, 1200, 40);

                var labelGO = FindOrCreate(itemGO.transform, $"ST_Item{i}_Label");
                var labelRect = EnsureRectTransform(labelGO);
                labelRect.anchorMin = new Vector2(0, 0.5f);
                labelRect.anchorMax = new Vector2(0, 0.5f);
                labelRect.pivot = new Vector2(0, 0.5f);
                labelRect.anchoredPosition = new Vector2(labelX, 0);
                labelRect.sizeDelta = new Vector2(400, 40);
                var labelTmp = EnsureComponent<TextMeshProUGUI>(labelGO);
                labelTmp.font = daydreamFont;
                labelTmp.fontSize = itemFontSize;
                labelTmp.color = menuNormal;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.raycastTarget = false;
                labelTmp.text = toggleNames[i];
                toggleLabels[i] = labelTmp;

                var valGO = FindOrCreate(itemGO.transform, $"ST_Item{i}_Value");
                var valRect = EnsureRectTransform(valGO);
                valRect.anchorMin = new Vector2(0, 0.5f);
                valRect.anchorMax = new Vector2(0, 0.5f);
                valRect.pivot = new Vector2(0, 0.5f);
                valRect.anchoredPosition = new Vector2(valueX, 0);
                valRect.sizeDelta = new Vector2(200, 40);
                var valTmp = EnsureComponent<TextMeshProUGUI>(valGO);
                valTmp.font = daydreamFont;
                valTmp.fontSize = itemFontSize;
                valTmp.color = onColor;
                valTmp.alignment = TextAlignmentOptions.MidlineLeft;
                valTmp.raycastTarget = false;
                valTmp.text = "ON";
                toggleValues[i] = valTmp;
            }

            // --- Slider Items (2, 3, 4) ---
            string[] sliderNames = { "Music Volume", "SFX Volume", "Master Volume" };
            TMP_Text[] sliderLabels = new TMP_Text[3];
            Image[] sliderBGs = new Image[3];
            Image[] sliderFills = new Image[3];
            TMP_Text[] sliderPercents = new TMP_Text[3];

            for (int i = 0; i < 3; i++)
            {
                int itemIdx = i + 2;
                float y = itemStartY - itemIdx * itemSpacing;
                var itemGO = FindOrCreate(panel.transform, $"ST_Item{itemIdx}");
                var itemRect = EnsureRectTransform(itemGO);
                SetTopLeft(itemRect, 0, y, 1200, 40);

                // Label
                var labelGO = FindOrCreate(itemGO.transform, $"ST_Item{itemIdx}_Label");
                var labelRect = EnsureRectTransform(labelGO);
                labelRect.anchorMin = new Vector2(0, 0.5f);
                labelRect.anchorMax = new Vector2(0, 0.5f);
                labelRect.pivot = new Vector2(0, 0.5f);
                labelRect.anchoredPosition = new Vector2(labelX, 0);
                labelRect.sizeDelta = new Vector2(400, 40);
                var labelTmp = EnsureComponent<TextMeshProUGUI>(labelGO);
                labelTmp.font = daydreamFont;
                labelTmp.fontSize = itemFontSize;
                labelTmp.color = menuNormal;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.raycastTarget = false;
                labelTmp.text = sliderNames[i];
                sliderLabels[i] = labelTmp;

                // Slider BG
                var bgSliderGO = FindOrCreate(itemGO.transform, $"ST_Item{itemIdx}_SliderBG");
                var bgSliderRect = EnsureRectTransform(bgSliderGO);
                bgSliderRect.anchorMin = new Vector2(0, 0.5f);
                bgSliderRect.anchorMax = new Vector2(0, 0.5f);
                bgSliderRect.pivot = new Vector2(0, 0.5f);
                bgSliderRect.anchoredPosition = new Vector2(valueX, 0);
                bgSliderRect.sizeDelta = new Vector2(sliderWidth, sliderHeight);
                var bgSliderImg = EnsureComponent<Image>(bgSliderGO);
                bgSliderImg.color = sliderBgColor;
                bgSliderImg.raycastTarget = false;
                sliderBGs[i] = bgSliderImg;

                // Slider Fill
                var fillGO = FindOrCreate(itemGO.transform, $"ST_Item{itemIdx}_SliderFill");
                var fillRect = EnsureRectTransform(fillGO);
                fillRect.anchorMin = new Vector2(0, 0.5f);
                fillRect.anchorMax = new Vector2(0, 0.5f);
                fillRect.pivot = new Vector2(0, 0.5f);
                fillRect.anchoredPosition = new Vector2(valueX, 0);
                fillRect.sizeDelta = new Vector2(sliderWidth, sliderHeight);
                var fillImg = EnsureComponent<Image>(fillGO);
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillAmount = 0.8f;
                fillImg.color = menuNormal;
                fillImg.raycastTarget = false;
                sliderFills[i] = fillImg;

                // Percent text
                var pctGO = FindOrCreate(itemGO.transform, $"ST_Item{itemIdx}_Percent");
                var pctRect = EnsureRectTransform(pctGO);
                pctRect.anchorMin = new Vector2(0, 0.5f);
                pctRect.anchorMax = new Vector2(0, 0.5f);
                pctRect.pivot = new Vector2(0, 0.5f);
                pctRect.anchoredPosition = new Vector2(valueX + sliderWidth + percentOffsetX, 0);
                pctRect.sizeDelta = new Vector2(100, 40);
                var pctTmp = EnsureComponent<TextMeshProUGUI>(pctGO);
                pctTmp.font = daydreamFont;
                pctTmp.fontSize = percentFontSize;
                pctTmp.color = menuNormal;
                pctTmp.alignment = TextAlignmentOptions.MidlineLeft;
                pctTmp.raycastTarget = false;
                pctTmp.text = "80%";
                sliderPercents[i] = pctTmp;
            }

            // --- ST_Back ---
            var backGO = FindOrCreate(panel.transform, "ST_Back");
            var backRect = EnsureRectTransform(backGO);
            SetTopCenter(backRect, 0, backY, 400, 50);
            var backTmp = EnsureComponent<TextMeshProUGUI>(backGO);
            backTmp.font = daydreamFont;
            backTmp.fontSize = backFontSize;
            backTmp.color = menuNormal;
            backTmp.alignment = TextAlignmentOptions.Center;
            backTmp.raycastTarget = true;
            backTmp.text = "Back";

            // --- ST_SelectIndicator ---
            var selectGO = FindOrCreate(panel.transform, "ST_SelectIndicator");
            var selectRect = EnsureRectTransform(selectGO);
            SetTopLeft(selectRect, 0, itemStartY, selectSize.x, selectSize.y);
            var selectImg = EnsureComponent<Image>(selectGO);
            selectImg.sprite = selectSprite;
            selectImg.preserveAspect = true;
            selectImg.raycastTarget = false;

            // --- ST_Footer ---
            var footerGO = FindOrCreate(panel.transform, "ST_Footer");
            var footerRect = EnsureRectTransform(footerGO);
            SetTopCenter(footerRect, 0, footerY, 800, 40);
            var footerTmp = EnsureComponent<TextMeshProUGUI>(footerGO);
            footerTmp.font = daydreamFont;
            footerTmp.fontSize = footerFontSize;
            Color footerColor = colorsSO != null ? colorsSO.menuSelected : new Color32(147, 76, 48, 255);
            footerTmp.color = footerColor;
            footerTmp.alignment = TextAlignmentOptions.Center;
            footerTmp.raycastTarget = false;
            footerTmp.text = "Arrow Keys + Enter to Select";

            // --- Wire serialized fields ---
            var so = new SerializedObject(screen);

            var screenStateProp = so.FindProperty("screenState");
            if (screenStateProp != null)
                screenStateProp.enumValueIndex = (int)GameState.Settings;

            SetRef(so, "_background", bgImg);
            SetRef(so, "_container", contImg);
            SetRef(so, "_titleImage", titleImg);

            SetRef(so, "_musicLabel", toggleLabels[0]);
            SetRef(so, "_musicValue", toggleValues[0]);
            SetRef(so, "_sfxLabel", toggleLabels[1]);
            SetRef(so, "_sfxValue", toggleValues[1]);

            SetRef(so, "_musicVolLabel", sliderLabels[0]);
            SetRef(so, "_musicVolSliderBG", sliderBGs[0]);
            SetRef(so, "_musicVolSliderFill", sliderFills[0]);
            SetRef(so, "_musicVolPercent", sliderPercents[0]);

            SetRef(so, "_sfxVolLabel", sliderLabels[1]);
            SetRef(so, "_sfxVolSliderBG", sliderBGs[1]);
            SetRef(so, "_sfxVolSliderFill", sliderFills[1]);
            SetRef(so, "_sfxVolPercent", sliderPercents[1]);

            SetRef(so, "_masterVolLabel", sliderLabels[2]);
            SetRef(so, "_masterVolSliderBG", sliderBGs[2]);
            SetRef(so, "_masterVolSliderFill", sliderFills[2]);
            SetRef(so, "_masterVolPercent", sliderPercents[2]);

            SetRef(so, "_backText", backTmp);
            SetRef(so, "_selectIndicator", selectImg);
            SetRef(so, "_footerText", footerTmp);

            so.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("SettingsScreen setup complete!");
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
            if (rect == null)
                rect = go.AddComponent<RectTransform>();
            return rect;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null)
                comp = go.AddComponent<T>();
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

        private static void SetTopLeft(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null && value != null)
                prop.objectReferenceValue = value;
        }
    }
}
