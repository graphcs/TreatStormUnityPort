using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Interaction;
using SnackAttack.Screens;

namespace SnackAttack.Editor
{
    public static class SetupTwitchIntegration
    {
        [MenuItem("SnackAttack/Setup Twitch Integration")]
        public static void Setup()
        {
            // 1. Create TwitchConfig SO
            var configPath = "Assets/ScriptableObjects/Config/TwitchConfig.asset";
            var configSO = AssetDatabase.LoadAssetAtPath<TwitchConfigSO>(configPath);
            if (configSO == null)
            {
                configSO = ScriptableObject.CreateInstance<TwitchConfigSO>();
                AssetDatabase.CreateAsset(configSO, configPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created TwitchConfig.asset");
            }

            // 2. Add TwitchChatManager to GameManager GO and wire config
            var gmGo = GameObject.Find("GameManager");
            if (gmGo == null)
            {
                Debug.LogError("GameManager not found in scene!");
                return;
            }

            var twitchMgr = gmGo.GetComponent<TwitchChatManager>();
            if (twitchMgr == null)
                twitchMgr = gmGo.AddComponent<TwitchChatManager>();

            // Wire twitchConfig on GameManager
            var gm = gmGo.GetComponent<GameManager>();
            if (gm != null)
            {
                var gmSo = new SerializedObject(gm);
                SetRef(gmSo, "twitchConfig", configSO);
                gmSo.ApplyModifiedProperties();
            }

            // 3. Find UICanvas
            var uiCanvas = GameObject.Find("UICanvas");
            if (uiCanvas == null)
            {
                Debug.LogError("UICanvas not found in scene!");
                return;
            }

            // Load assets
            var layoutSO = AssetDatabase.LoadAssetAtPath<UILayoutSO>("Assets/ScriptableObjects/Config/UILayout.asset");
            var colorsSO = AssetDatabase.LoadAssetAtPath<UIColorsSO>("Assets/ScriptableObjects/Config/UIColors.asset");
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Settings background.png");
            var selectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Select.png");
            var daydreamFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Daydream SDF.asset");

            Color menuNormal = colorsSO != null ? colorsSO.menuNormal : new Color32(77, 43, 31, 255);
            float itemFontSize = layoutSO != null ? layoutSO.settingsItemFontSize : 28f;
            float labelX = layoutSO != null ? layoutSO.settingsLabelX : 264f;
            float valueX = layoutSO != null ? layoutSO.settingsValueX : 744f;

            // 4. Create TwitchSetupPanel under UICanvas
            var panel = FindOrCreate(uiCanvas.transform, "TwitchSetupPanel");
            var panelRect = EnsureRectTransform(panel);
            SetStretchAll(panelRect);
            EnsureComponent<CanvasGroup>(panel);
            var screen = EnsureComponent<TwitchSetupScreen>(panel);

            // TW_Background
            var bgGO = FindOrCreate(panel.transform, "TW_Background");
            var bgRect = EnsureRectTransform(bgGO);
            SetStretchAll(bgRect);
            var bgImg = EnsureComponent<Image>(bgGO);
            bgImg.sprite = bgSprite;
            bgImg.raycastTarget = true;

            // TW_Title
            var titleGO = FindOrCreate(panel.transform, "TW_Title");
            var titleRect = EnsureRectTransform(titleGO);
            SetTopCenter(titleRect, 0, -120, 600, 60);
            var titleTmp = EnsureComponent<TextMeshProUGUI>(titleGO);
            titleTmp.font = daydreamFont;
            titleTmp.fontSize = 36;
            titleTmp.color = menuNormal;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;
            titleTmp.text = "TWITCH SETUP";

            // Items
            string[] itemLabels = { "Enabled", "Channel", "OAuth Token", "Bot Username", "Test Connection" };
            float[] itemYs = { -350, -410, -470, -530, -590 };
            TMP_Text[] labels = new TMP_Text[5];
            TMP_Text[] values = new TMP_Text[5];

            for (int i = 0; i < 5; i++)
            {
                var itemGO = FindOrCreate(panel.transform, $"TW_Item{i}");
                var itemRect = EnsureRectTransform(itemGO);
                SetTopLeft(itemRect, 0, itemYs[i], 1200, 40);

                var labelGO = FindOrCreate(itemGO.transform, $"TW_Item{i}_Label");
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
                labelTmp.text = itemLabels[i];
                labels[i] = labelTmp;

                // Value text for items 0-3 (not for Test Connection)
                if (i < 4)
                {
                    var valGO = FindOrCreate(itemGO.transform, $"TW_Item{i}_Value");
                    var valRect = EnsureRectTransform(valGO);
                    valRect.anchorMin = new Vector2(0, 0.5f);
                    valRect.anchorMax = new Vector2(0, 0.5f);
                    valRect.pivot = new Vector2(0, 0.5f);
                    valRect.anchoredPosition = new Vector2(valueX, 0);
                    valRect.sizeDelta = new Vector2(400, 40);
                    var valTmp = EnsureComponent<TextMeshProUGUI>(valGO);
                    valTmp.font = daydreamFont;
                    valTmp.fontSize = itemFontSize;
                    valTmp.color = menuNormal;
                    valTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    valTmp.raycastTarget = false;
                    valTmp.text = i == 0 ? "OFF" : "<not set>";
                    values[i] = valTmp;
                }
            }

            // TW_Status
            var statusGO = FindOrCreate(panel.transform, "TW_Status");
            var statusRect = EnsureRectTransform(statusGO);
            SetTopCenter(statusRect, 0, -650, 600, 40);
            var statusTmp = EnsureComponent<TextMeshProUGUI>(statusGO);
            statusTmp.font = daydreamFont;
            statusTmp.fontSize = 24;
            statusTmp.color = configSO.disconnectedColor;
            statusTmp.alignment = TextAlignmentOptions.Center;
            statusTmp.raycastTarget = false;
            statusTmp.text = "Disconnected";

            // TW_InputOverlay
            var overlayGO = FindOrCreate(panel.transform, "TW_InputOverlay");
            var overlayRect = EnsureRectTransform(overlayGO);
            SetStretchAll(overlayRect);
            overlayGO.SetActive(false);

            // TW_InputBg
            var inputBgGO = FindOrCreate(overlayGO.transform, "TW_InputBg");
            var inputBgRect = EnsureRectTransform(inputBgGO);
            SetStretchAll(inputBgRect);
            var inputBgImg = EnsureComponent<Image>(inputBgGO);
            inputBgImg.color = new Color(0, 0, 0, 0.7f);
            inputBgImg.raycastTarget = true;

            // TW_InputLabel
            var inputLabelGO = FindOrCreate(overlayGO.transform, "TW_InputLabel");
            var inputLabelRect = EnsureRectTransform(inputLabelGO);
            SetTopCenter(inputLabelRect, 0, -420, 500, 40);
            var inputLabelTmp = EnsureComponent<TextMeshProUGUI>(inputLabelGO);
            inputLabelTmp.font = daydreamFont;
            inputLabelTmp.fontSize = 24;
            inputLabelTmp.color = Color.white;
            inputLabelTmp.alignment = TextAlignmentOptions.Center;
            inputLabelTmp.raycastTarget = false;
            inputLabelTmp.text = "Enter value:";

            // TW_InputField
            var inputFieldGO = FindOrCreate(overlayGO.transform, "TW_InputField");
            var inputFieldRect = EnsureRectTransform(inputFieldGO);
            SetTopCenter(inputFieldRect, 0, -480, 500, 50);

            // InputField needs a text area child and placeholder
            var textAreaGO = FindOrCreate(inputFieldGO.transform, "Text Area");
            var textAreaRect = EnsureRectTransform(textAreaGO);
            SetStretchAll(textAreaRect);
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -6);
            var textAreaMask = EnsureComponent<RectMask2D>(textAreaGO);

            var inputTextGO = FindOrCreate(textAreaGO.transform, "Text");
            var inputTextRect = EnsureRectTransform(inputTextGO);
            SetStretchAll(inputTextRect);
            var inputTextTmp = EnsureComponent<TextMeshProUGUI>(inputTextGO);
            inputTextTmp.font = daydreamFont;
            inputTextTmp.fontSize = 22;
            inputTextTmp.color = Color.white;
            inputTextTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderGO = FindOrCreate(textAreaGO.transform, "Placeholder");
            var placeholderRect = EnsureRectTransform(placeholderGO);
            SetStretchAll(placeholderRect);
            var placeholderTmp = EnsureComponent<TextMeshProUGUI>(placeholderGO);
            placeholderTmp.font = daydreamFont;
            placeholderTmp.fontSize = 22;
            placeholderTmp.color = new Color(1, 1, 1, 0.4f);
            placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderTmp.text = "Type here...";
            placeholderTmp.fontStyle = FontStyles.Italic;

            // Input field background
            var inputFieldBg = EnsureComponent<Image>(inputFieldGO);
            inputFieldBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var tmpInputField = EnsureComponent<TMP_InputField>(inputFieldGO);
            tmpInputField.textViewport = textAreaRect;
            tmpInputField.textComponent = inputTextTmp;
            tmpInputField.placeholder = placeholderTmp;
            tmpInputField.fontAsset = daydreamFont;
            tmpInputField.pointSize = 22;

            // TW_Back
            var backGO = FindOrCreate(panel.transform, "TW_Back");
            var backRect = EnsureRectTransform(backGO);
            SetTopCenter(backRect, 0, -880, 400, 50);
            var backTmp = EnsureComponent<TextMeshProUGUI>(backGO);
            backTmp.font = daydreamFont;
            backTmp.fontSize = 32;
            backTmp.color = menuNormal;
            backTmp.alignment = TextAlignmentOptions.Center;
            backTmp.raycastTarget = true;
            backTmp.text = "Back";

            // TW_SelectIndicator
            var selectGO = FindOrCreate(panel.transform, "TW_SelectIndicator");
            var selectRect = EnsureRectTransform(selectGO);
            SetTopLeft(selectRect, 0, -350, 34.56f, 28.8f);
            var selectImg = EnsureComponent<Image>(selectGO);
            selectImg.sprite = selectSprite;
            selectImg.preserveAspect = true;
            selectImg.raycastTarget = false;

            // TW_Footer
            var footerGO = FindOrCreate(panel.transform, "TW_Footer");
            var footerRect = EnsureRectTransform(footerGO);
            SetTopCenter(footerRect, 0, -960, 800, 40);
            var footerTmp = EnsureComponent<TextMeshProUGUI>(footerGO);
            footerTmp.font = daydreamFont;
            footerTmp.fontSize = 18;
            Color footerColor = colorsSO != null ? colorsSO.menuSelected : new Color32(147, 76, 48, 255);
            footerTmp.color = footerColor;
            footerTmp.alignment = TextAlignmentOptions.Center;
            footerTmp.raycastTarget = false;
            footerTmp.text = "Arrow Keys + Enter to Edit | Escape to Cancel";

            // Wire serialized fields on TwitchSetupScreen
            var so = new SerializedObject(screen);

            var screenStateProp = so.FindProperty("screenState");
            if (screenStateProp != null)
                screenStateProp.enumValueIndex = (int)GameState.TwitchSetup;

            SetRef(so, "_enabledLabel", labels[0]);
            SetRef(so, "_enabledValue", values[0]);
            SetRef(so, "_channelLabel", labels[1]);
            SetRef(so, "_channelValue", values[1]);
            SetRef(so, "_tokenLabel", labels[2]);
            SetRef(so, "_tokenValue", values[2]);
            SetRef(so, "_usernameLabel", labels[3]);
            SetRef(so, "_usernameValue", values[3]);
            SetRef(so, "_testLabel", labels[4]);
            SetRef(so, "_statusText", statusTmp);
            SetRef(so, "_inputOverlay", overlayGO);
            SetRef(so, "_inputField", tmpInputField);
            SetRef(so, "_inputLabel", inputLabelTmp);
            SetRef(so, "_backText", backTmp);
            SetRef(so, "_selectIndicator", selectImg);
            SetRef(so, "_footerText", footerTmp);

            so.ApplyModifiedProperties();

            // 5. Add Twitch Setup item to SettingsPanel
            var settingsPanel = uiCanvas.transform.Find("SettingsPanel")?.gameObject;
            if (settingsPanel != null)
            {
                var settingsScreen = settingsPanel.GetComponent<SettingsScreen>();

                // Create ST_Item5 for "Twitch Setup"
                float twitchItemY = layoutSO != null ? layoutSO.settingsItemStartY - 5 * layoutSO.settingsItemSpacing : -754f;
                var twitchItemGO = FindOrCreate(settingsPanel.transform, "ST_Item5");
                var twitchItemRect = EnsureRectTransform(twitchItemGO);
                SetTopLeft(twitchItemRect, 0, twitchItemY, 1200, 40);

                var twitchLabelGO = FindOrCreate(twitchItemGO.transform, "ST_Item5_Label");
                var twitchLabelRect = EnsureRectTransform(twitchLabelGO);
                twitchLabelRect.anchorMin = new Vector2(0, 0.5f);
                twitchLabelRect.anchorMax = new Vector2(0, 0.5f);
                twitchLabelRect.pivot = new Vector2(0, 0.5f);
                twitchLabelRect.anchoredPosition = new Vector2(labelX, 0);
                twitchLabelRect.sizeDelta = new Vector2(400, 40);
                var twitchLabelTmp = EnsureComponent<TextMeshProUGUI>(twitchLabelGO);
                twitchLabelTmp.font = daydreamFont;
                twitchLabelTmp.fontSize = itemFontSize;
                twitchLabelTmp.color = menuNormal;
                twitchLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                twitchLabelTmp.raycastTarget = false;
                twitchLabelTmp.text = "Twitch Setup";

                // Wire the new label into SettingsScreen
                if (settingsScreen != null)
                {
                    var settingsSo = new SerializedObject(settingsScreen);
                    SetRef(settingsSo, "_twitchSetupLabel", twitchLabelTmp);
                    settingsSo.ApplyModifiedProperties();
                }
            }

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("Twitch Integration setup complete! TwitchConfig.asset created, TwitchChatManager added, TwitchSetupPanel built, Settings item added.");
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
