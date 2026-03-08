using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Interaction;
using SnackAttack.Gameplay;

namespace SnackAttack.Editor
{
    public static class SetupVotingSystem
    {
        [MenuItem("SnackAttack/Setup Voting System")]
        public static void Setup()
        {
            // 1. Create VotingSettings asset
            var settings = CreateSettingsAsset();

            // 2. Wire GameManager reference
            WireGameManager(settings);

            // 3. Create scene hierarchy
            var font = FindDaydreamFont();
            CreateVotingMeter(font);
            CreateAnnouncementGroup(font);
            CreateChatPanel(font, settings);
            CreateCrowdChaosOverlay(font, settings);

            // 4. Wire RoundManager references
            WireRoundManager();

            Debug.Log("[SetupVotingSystem] Setup complete.");
        }

        private static VotingSettingsSO CreateSettingsAsset()
        {
            const string path = "Assets/ScriptableObjects/Config/VotingSettings.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VotingSettingsSO>(path);
            if (existing != null)
            {
                Debug.Log("[SetupVotingSystem] VotingSettings asset already exists.");
                return existing;
            }

            var so = ScriptableObject.CreateInstance<VotingSettingsSO>();
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupVotingSystem] Created VotingSettings.asset");
            return so;
        }

        private static void WireGameManager(VotingSettingsSO settings)
        {
            var gmGo = GameObject.Find("GameManager");
            if (gmGo == null) { Debug.LogError("[SetupVotingSystem] GameManager not found."); return; }

            var gm = gmGo.GetComponent<GameManager>();
            if (gm == null) { Debug.LogError("[SetupVotingSystem] GameManager component not found."); return; }

            var so = new SerializedObject(gm);
            var prop = so.FindProperty("votingSettings");
            if (prop != null)
            {
                prop.objectReferenceValue = settings;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(gm);
            }
        }

        private static TMP_FontAsset FindDaydreamFont()
        {
            var guids = AssetDatabase.FindAssets("Daydream SDF t:TMP_FontAsset");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return null;
        }

        private static void CreateVotingMeter(TMP_FontAsset font)
        {
            var hudPanel = GameObject.Find("GameplayHUDPanel");
            if (hudPanel == null) { Debug.LogWarning("[SetupVotingSystem] GameplayHUDPanel not found."); return; }

            // Check if already exists
            var existing = hudPanel.transform.Find("HUD_VotingMeter");
            if (existing != null)
            {
                Debug.Log("[SetupVotingSystem] HUD_VotingMeter already exists.");
                return;
            }

            var meterGo = new GameObject("HUD_VotingMeter");
            var meterRect = meterGo.AddComponent<RectTransform>();
            meterRect.SetParent(hudPanel.transform, false);
            meterRect.anchorMin = new Vector2(0, 1);
            meterRect.anchorMax = new Vector2(0, 1);
            meterRect.pivot = new Vector2(0, 1);
            meterRect.anchoredPosition = new Vector2(900, -105);
            meterRect.sizeDelta = new Vector2(280, 85);

            meterGo.AddComponent<CanvasGroup>();
            var meter = meterGo.AddComponent<VotingMeter>();

            // Border
            var borderGo = new GameObject("VM_Border");
            var borderRect = borderGo.AddComponent<RectTransform>();
            borderRect.SetParent(meterRect, false);
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0x4D / 255f, 0x2B / 255f, 0x1F / 255f);
            borderImg.raycastTarget = false;

            // Status text
            var statusGo = new GameObject("VM_Status");
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.SetParent(meterRect, false);
            statusRect.anchorMin = new Vector2(0, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(0, 1);
            statusRect.anchoredPosition = new Vector2(6, -4);
            statusRect.sizeDelta = new Vector2(-12, 18);
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "";
            statusTmp.fontSize = 12;
            statusTmp.alignment = TextAlignmentOptions.MidlineLeft;
            statusTmp.color = Color.white;
            statusTmp.raycastTarget = false;
            if (font != null) statusTmp.font = font;

            // Bars container
            var barsGo = new GameObject("VM_BarsContainer");
            var barsRect = barsGo.AddComponent<RectTransform>();
            barsRect.SetParent(meterRect, false);
            barsRect.anchorMin = new Vector2(0, 1);
            barsRect.anchorMax = new Vector2(1, 1);
            barsRect.pivot = new Vector2(0, 1);
            barsRect.anchoredPosition = new Vector2(6, -24);
            barsRect.sizeDelta = new Vector2(-12, 55);

            // Wire serialized fields
            var meterSo = new SerializedObject(meter);
            meterSo.FindProperty("_statusText").objectReferenceValue = statusTmp;
            meterSo.FindProperty("_barsContainer").objectReferenceValue = barsRect;
            meterSo.ApplyModifiedProperties();

            meterGo.SetActive(false);
            EditorUtility.SetDirty(meterGo);
        }

        private static void CreateAnnouncementGroup(TMP_FontAsset font)
        {
            var hudPanel = GameObject.Find("GameplayHUDPanel");
            if (hudPanel == null) return;

            var existing = hudPanel.transform.Find("HUD_AnnouncementGroup");
            if (existing != null)
            {
                // Ensure children exist
                EnsureAnnouncementChildren(existing.gameObject, font);
                return;
            }

            var annGo = new GameObject("HUD_AnnouncementGroup");
            var annRect = annGo.AddComponent<RectTransform>();
            annRect.SetParent(hudPanel.transform, false);
            annRect.anchorMin = new Vector2(0, 0.5f);
            annRect.anchorMax = new Vector2(1, 0.5f);
            annRect.pivot = new Vector2(0.5f, 0.5f);
            annRect.anchoredPosition = new Vector2(0, 50);
            annRect.sizeDelta = new Vector2(0, 120);

            EnsureAnnouncementChildren(annGo, font);
            annGo.SetActive(false);
            EditorUtility.SetDirty(annGo);
        }

        private static void EnsureAnnouncementChildren(GameObject parent, TMP_FontAsset font)
        {
            var parentRect = parent.GetComponent<RectTransform>();

            if (parent.transform.Find("ANN_Text") == null)
            {
                var textGo = new GameObject("ANN_Text");
                var textRect = textGo.AddComponent<RectTransform>();
                textRect.SetParent(parentRect, false);
                textRect.anchorMin = new Vector2(0, 0.5f);
                textRect.anchorMax = new Vector2(1, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = new Vector2(0, 10);
                textRect.sizeDelta = new Vector2(0, 60);
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "";
                tmp.fontSize = 48;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                if (font != null) tmp.font = font;
            }

            if (parent.transform.Find("ANN_Subtext") == null)
            {
                var subGo = new GameObject("ANN_Subtext");
                var subRect = subGo.AddComponent<RectTransform>();
                subRect.SetParent(parentRect, false);
                subRect.anchorMin = new Vector2(0, 0.5f);
                subRect.anchorMax = new Vector2(1, 0.5f);
                subRect.pivot = new Vector2(0.5f, 0.5f);
                subRect.anchoredPosition = new Vector2(0, -30);
                subRect.sizeDelta = new Vector2(0, 40);
                var tmp = subGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "";
                tmp.fontSize = 24;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                if (font != null) tmp.font = font;
            }

            // Wire to HUD
            var hud = parent.GetComponentInParent<Screens.GameplayHUD>();
            if (hud != null)
            {
                var hudSo = new SerializedObject(hud);
                var annGroupProp = hudSo.FindProperty("_announcementGroup");
                if (annGroupProp != null)
                    annGroupProp.objectReferenceValue = parent;

                var annTextProp = hudSo.FindProperty("_announcementText");
                var annText = parent.transform.Find("ANN_Text")?.GetComponent<TMP_Text>();
                if (annTextProp != null && annText != null)
                    annTextProp.objectReferenceValue = annText;

                var annSubProp = hudSo.FindProperty("_announcementSubtext");
                var annSub = parent.transform.Find("ANN_Subtext")?.GetComponent<TMP_Text>();
                if (annSubProp != null && annSub != null)
                    annSubProp.objectReferenceValue = annSub;

                hudSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(hud);
            }
        }

        private static void CreateChatPanel(TMP_FontAsset font, VotingSettingsSO settings)
        {
            var uiCanvas = GameObject.Find("UICanvas");
            if (uiCanvas == null) { Debug.LogWarning("[SetupVotingSystem] UICanvas not found."); return; }

            var existing = uiCanvas.transform.Find("ChatPanel");
            if (existing != null)
            {
                Debug.Log("[SetupVotingSystem] ChatPanel already exists.");
                return;
            }

            var chatGo = new GameObject("ChatPanel");
            var chatRect = chatGo.AddComponent<RectTransform>();
            chatRect.SetParent(uiCanvas.transform, false);
            chatRect.anchorMin = new Vector2(1, 0);
            chatRect.anchorMax = new Vector2(1, 1);
            chatRect.pivot = new Vector2(1, 0.5f);
            chatRect.anchoredPosition = Vector2.zero;
            chatRect.sizeDelta = new Vector2(200, 0);

            chatGo.AddComponent<CanvasGroup>();
            var chat = chatGo.AddComponent<ChatSimulator>();

            // Background
            var bgGo = new GameObject("CP_Background");
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.SetParent(chatRect, false);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = settings != null ? settings.chatBgColor : new Color(0x19 / 255f, 0x19 / 255f, 0x23 / 255f);
            bgImg.raycastTarget = false;

            // Header
            var headerGo = new GameObject("CP_Header");
            var headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.SetParent(chatRect, false);
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 35);

            var headerBgGo = new GameObject("CP_HeaderBg");
            var headerBgRect = headerBgGo.AddComponent<RectTransform>();
            headerBgRect.SetParent(headerRect, false);
            headerBgRect.anchorMin = Vector2.zero;
            headerBgRect.anchorMax = Vector2.one;
            headerBgRect.offsetMin = Vector2.zero;
            headerBgRect.offsetMax = Vector2.zero;
            var headerBgImg = headerBgGo.AddComponent<Image>();
            headerBgImg.color = settings != null ? settings.chatHeaderBgColor : new Color(0x28 / 255f, 0x28 / 255f, 0x37 / 255f);
            headerBgImg.raycastTarget = false;

            var headerTextGo = new GameObject("CP_HeaderText");
            var headerTextRect = headerTextGo.AddComponent<RectTransform>();
            headerTextRect.SetParent(headerRect, false);
            headerTextRect.anchorMin = new Vector2(0, 0);
            headerTextRect.anchorMax = new Vector2(0.7f, 1);
            headerTextRect.offsetMin = new Vector2(8, 0);
            headerTextRect.offsetMax = Vector2.zero;
            var headerTmp = headerTextGo.AddComponent<TextMeshProUGUI>();
            headerTmp.text = "CHAT SIM";
            headerTmp.fontSize = 10;
            headerTmp.alignment = TextAlignmentOptions.MidlineLeft;
            headerTmp.color = Color.white;
            headerTmp.raycastTarget = false;
            if (font != null) headerTmp.font = font;

            // Auto toggle button
            var autoGo = new GameObject("CP_AutoToggle");
            var autoRect = autoGo.AddComponent<RectTransform>();
            autoRect.SetParent(headerRect, false);
            autoRect.anchorMin = new Vector2(1, 0.5f);
            autoRect.anchorMax = new Vector2(1, 0.5f);
            autoRect.pivot = new Vector2(1, 0.5f);
            autoRect.anchoredPosition = new Vector2(-6, 0);
            autoRect.sizeDelta = new Vector2(40, 25);
            var autoImg = autoGo.AddComponent<Image>();
            autoImg.color = new Color(0.3f, 0.8f, 0.3f);

            var autoTextGo = new GameObject("AutoText");
            var autoTextRect = autoTextGo.AddComponent<RectTransform>();
            autoTextRect.SetParent(autoRect, false);
            autoTextRect.anchorMin = Vector2.zero;
            autoTextRect.anchorMax = Vector2.one;
            autoTextRect.offsetMin = Vector2.zero;
            autoTextRect.offsetMax = Vector2.zero;
            var autoTmp = autoTextGo.AddComponent<TextMeshProUGUI>();
            autoTmp.text = "AUTO";
            autoTmp.fontSize = 8;
            autoTmp.alignment = TextAlignmentOptions.Center;
            autoTmp.color = Color.white;
            autoTmp.raycastTarget = false;
            if (font != null) autoTmp.font = font;

            // Message area
            var msgGo = new GameObject("CP_MessageArea");
            var msgRect = msgGo.AddComponent<RectTransform>();
            msgRect.SetParent(chatRect, false);
            msgRect.anchorMin = new Vector2(0, 0);
            msgRect.anchorMax = new Vector2(1, 1);
            msgRect.pivot = new Vector2(0, 1);
            msgRect.offsetMin = new Vector2(0, 130);
            msgRect.offsetMax = new Vector2(0, -35);

            // Button area
            var btnGo = new GameObject("CP_ButtonArea");
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.SetParent(chatRect, false);
            btnRect.anchorMin = new Vector2(0, 0);
            btnRect.anchorMax = new Vector2(1, 0);
            btnRect.pivot = new Vector2(0, 0);
            btnRect.anchoredPosition = new Vector2(0, 20);
            btnRect.sizeDelta = new Vector2(0, 130);

            // Instructions
            var instrGo = new GameObject("CP_Instructions");
            var instrRect = instrGo.AddComponent<RectTransform>();
            instrRect.SetParent(chatRect, false);
            instrRect.anchorMin = new Vector2(0, 0);
            instrRect.anchorMax = new Vector2(1, 0);
            instrRect.pivot = new Vector2(0.5f, 0);
            instrRect.anchoredPosition = new Vector2(0, 4);
            instrRect.sizeDelta = new Vector2(0, 16);
            var instrTmp = instrGo.AddComponent<TextMeshProUGUI>();
            instrTmp.text = "Click or Toggle Auto";
            instrTmp.fontSize = 8;
            instrTmp.alignment = TextAlignmentOptions.Center;
            instrTmp.color = new Color(0.6f, 0.6f, 0.7f);
            instrTmp.raycastTarget = false;
            if (font != null) instrTmp.font = font;

            // Wire serialized fields
            var chatSo = new SerializedObject(chat);
            chatSo.FindProperty("_messageArea").objectReferenceValue = msgRect;
            chatSo.FindProperty("_buttonArea").objectReferenceValue = btnRect;
            chatSo.FindProperty("_autoToggleBg").objectReferenceValue = autoImg;
            chatSo.FindProperty("_autoToggleText").objectReferenceValue = autoTmp;
            chatSo.ApplyModifiedProperties();

            chatGo.SetActive(false);
            EditorUtility.SetDirty(chatGo);
        }

        private static void CreateCrowdChaosOverlay(TMP_FontAsset font, VotingSettingsSO settings)
        {
            var uiCanvas = GameObject.Find("UICanvas");
            if (uiCanvas == null) return;

            var existing = uiCanvas.transform.Find("CrowdChaosOverlay");
            if (existing != null)
            {
                Debug.Log("[SetupVotingSystem] CrowdChaosOverlay already exists.");
                return;
            }

            var overlayGo = new GameObject("CrowdChaosOverlay");
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.SetParent(uiCanvas.transform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            overlayGo.AddComponent<CanvasGroup>();
            var overlay = overlayGo.AddComponent<CrowdChaosOverlay>();

            // Tint image
            var tintGo = new GameObject("CC_TintOverlay");
            var tintRect = tintGo.AddComponent<RectTransform>();
            tintRect.SetParent(overlayRect, false);
            tintRect.anchorMin = Vector2.zero;
            tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = Vector2.zero;
            tintRect.offsetMax = Vector2.zero;
            var tintImg = tintGo.AddComponent<Image>();
            Color tintColor = settings != null ? settings.chaosTintColor : new Color(220f / 255f, 40f / 255f, 40f / 255f);
            float tintAlpha = settings != null ? settings.chaosTintAlpha : 75f / 255f;
            tintColor.a = tintAlpha;
            tintImg.color = tintColor;
            tintImg.raycastTarget = false;

            // Title
            var titleGo = new GameObject("CC_Title");
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.SetParent(overlayRect, false);
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -280);
            titleRect.sizeDelta = new Vector2(800, 50);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CROWD CHAOS IN";
            titleTmp.fontSize = 36;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleTmp.raycastTarget = false;
            if (font != null) titleTmp.font = font;

            // Countdown number
            var numGo = new GameObject("CC_CountdownNumber");
            var numRect = numGo.AddComponent<RectTransform>();
            numRect.SetParent(overlayRect, false);
            numRect.anchorMin = new Vector2(0.5f, 1);
            numRect.anchorMax = new Vector2(0.5f, 1);
            numRect.pivot = new Vector2(0.5f, 1);
            numRect.anchoredPosition = new Vector2(0, -380);
            numRect.sizeDelta = new Vector2(300, 160);
            var numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = "5";
            numTmp.fontSize = 140;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;
            numTmp.raycastTarget = false;
            if (font != null) numTmp.font = font;

            // Options text
            var optGo = new GameObject("CC_OptionsText");
            var optRect = optGo.AddComponent<RectTransform>();
            optRect.SetParent(overlayRect, false);
            optRect.anchorMin = new Vector2(0.5f, 1);
            optRect.anchorMax = new Vector2(0.5f, 1);
            optRect.pivot = new Vector2(0.5f, 1);
            optRect.anchoredPosition = new Vector2(0, -330);
            optRect.sizeDelta = new Vector2(800, 40);
            var optTmp = optGo.AddComponent<TextMeshProUGUI>();
            optTmp.text = "";
            optTmp.fontSize = 24;
            optTmp.alignment = TextAlignmentOptions.Center;
            optTmp.color = Color.white;
            optTmp.raycastTarget = false;
            if (font != null) optTmp.font = font;

            // Wire serialized fields
            var overlaySo = new SerializedObject(overlay);
            overlaySo.FindProperty("_tintOverlay").objectReferenceValue = tintImg;
            overlaySo.FindProperty("_titleText").objectReferenceValue = titleTmp;
            overlaySo.FindProperty("_countdownNumber").objectReferenceValue = numTmp;
            overlaySo.FindProperty("_optionsText").objectReferenceValue = optTmp;
            overlaySo.ApplyModifiedProperties();

            overlayGo.SetActive(false);
            EditorUtility.SetDirty(overlayGo);
        }

        private static void WireRoundManager()
        {
            var rmGo = GameObject.Find("RoundManager");
            if (rmGo == null) { Debug.LogWarning("[SetupVotingSystem] RoundManager not found."); return; }

            var rm = rmGo.GetComponent<RoundManager>();
            if (rm == null) { Debug.LogWarning("[SetupVotingSystem] RoundManager component not found."); return; }

            var so = new SerializedObject(rm);

            var chatPanel = GameObject.Find("ChatPanel");
            if (chatPanel != null)
            {
                var chatSim = chatPanel.GetComponent<ChatSimulator>();
                if (chatSim != null)
                    so.FindProperty("_chatSimulator").objectReferenceValue = chatSim;
            }

            var meterGo = GameObject.Find("HUD_VotingMeter");
            if (meterGo != null)
            {
                var meter = meterGo.GetComponent<VotingMeter>();
                if (meter != null)
                    so.FindProperty("_votingMeter").objectReferenceValue = meter;
            }

            var overlayGo = GameObject.Find("CrowdChaosOverlay");
            if (overlayGo != null)
            {
                var overlay = overlayGo.GetComponent<CrowdChaosOverlay>();
                if (overlay != null)
                    so.FindProperty("_chaosOverlay").objectReferenceValue = overlay;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rm);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rmGo.scene);
        }
    }
}
