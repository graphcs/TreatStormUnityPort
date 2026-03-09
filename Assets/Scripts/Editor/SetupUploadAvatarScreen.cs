using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Screens;

namespace SnackAttack.Editor
{
    public static class SetupUploadAvatarScreen
    {
        [MenuItem("SnackAttack/Setup Upload Avatar Screen")]
        public static void Setup()
        {
            var uiCanvas = GameObject.Find("UICanvas");
            if (uiCanvas == null)
            {
                Debug.LogError("UICanvas not found in scene!");
                return;
            }

            var colorsSO = AssetDatabase.LoadAssetAtPath<UIColorsSO>("Assets/ScriptableObjects/Config/UIColors.asset");
            var layoutSO = AssetDatabase.LoadAssetAtPath<UILayoutSO>("Assets/ScriptableObjects/Config/UILayout.asset");
            var daydreamFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Daydream SDF.asset");
            var selectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Select.png");

            // Colors
            Color bgColor = colorsSO != null ? colorsSO.avatarBgColor : new Color32(20, 30, 60, 255);
            Color gold = colorsSO != null ? colorsSO.avatarAccentGold : new Color32(255, 200, 80, 255);
            Color inputBg = colorsSO != null ? colorsSO.avatarInputActiveBg : new Color32(60, 80, 130, 255);
            Color inactiveBg = colorsSO != null ? colorsSO.avatarInputInactiveBg : new Color32(40, 50, 80, 255);
            Color btnNormal = colorsSO != null ? colorsSO.avatarButtonNormal : new Color32(80, 140, 80, 255);
            Color backNormal = colorsSO != null ? colorsSO.avatarBackNormal : new Color32(100, 70, 60, 255);
            Color successColor = colorsSO != null ? colorsSO.avatarSuccessColor : new Color32(100, 255, 100, 255);
            Color errorColor = colorsSO != null ? colorsSO.avatarErrorColor : new Color32(255, 100, 100, 255);
            Color progressFill = colorsSO != null ? colorsSO.avatarProgressFill : new Color32(100, 180, 255, 255);
            Color hintColor = colorsSO != null ? colorsSO.avatarHintColor : new Color32(120, 120, 140, 255);
            Color disabledBtn = colorsSO != null ? colorsSO.avatarDisabledBtn : new Color32(60, 60, 80, 255);
            Color apiLink = colorsSO != null ? colorsSO.avatarApiLinkColor : new Color32(150, 200, 255, 255);

            // Layout
            float titleY = layoutSO != null ? layoutSO.avatarTitleY : -100f;
            float nameLabelY = layoutSO != null ? layoutSO.avatarNameLabelY : -288f;
            float nameInputY = layoutSO != null ? layoutSO.avatarNameInputY : -332f;
            Vector2 nameInputSize = layoutSO != null ? layoutSO.avatarNameInputSize : new Vector2(400f, 50f);
            float photoAreaY = layoutSO != null ? layoutSO.avatarPhotoAreaY : -440f;
            Vector2 placeholderSize = layoutSO != null ? layoutSO.avatarPlaceholderSize : new Vector2(300f, 150f);
            float browseBtnY = layoutSO != null ? layoutSO.avatarBrowseBtnY : -660f;
            Vector2 browseBtnSize = layoutSO != null ? layoutSO.avatarBrowseBtnSize : new Vector2(200f, 45f);
            float generateBtnY = layoutSO != null ? layoutSO.avatarGenerateBtnY : -740f;
            Vector2 generateBtnSize = layoutSO != null ? layoutSO.avatarGenerateBtnSize : new Vector2(300f, 55f);
            float backBtnY = layoutSO != null ? layoutSO.avatarBackBtnY : -920f;
            Vector2 backBtnSize = layoutSO != null ? layoutSO.avatarBackBtnSize : new Vector2(160f, 40f);
            Vector2 progressBarSize = layoutSO != null ? layoutSO.avatarProgressBarSize : new Vector2(500f, 30f);

            // Panel
            var panel = FindOrCreate(uiCanvas.transform, "UploadAvatarPanel");
            var panelRect = EnsureRectTransform(panel);
            SetStretchAll(panelRect);
            EnsureComponent<CanvasGroup>(panel);
            var screen = EnsureComponent<UploadAvatarScreen>(panel);

            // Background
            var bgGO = FindOrCreate(panel.transform, "UA_Background");
            var bgRect = EnsureRectTransform(bgGO);
            SetStretchAll(bgRect);
            var bgImg = EnsureComponent<Image>(bgGO);
            bgImg.color = bgColor;
            bgImg.raycastTarget = true;

            // ========== INPUT GROUP ==========
            var inputGroup = FindOrCreate(panel.transform, "UA_InputGroup");
            EnsureRectTransform(inputGroup);
            SetStretchAll(EnsureRectTransform(inputGroup));

            var titleGO = FindOrCreate(inputGroup.transform, "UA_Title");
            SetTopCenter(EnsureRectTransform(titleGO), 0, titleY, 800, 60);
            var titleTmp = SetupTMP(titleGO, daydreamFont, 28, gold, "Create Your Dog");

            // Subtitle lines
            string[] subtitles = { "Upload a photo and we'll", "turn it into a playable", "character!" };
            for (int i = 0; i < 3; i++)
            {
                var subGO = FindOrCreate(inputGroup.transform, $"UA_Subtitle{i + 1}");
                SetTopCenter(EnsureRectTransform(subGO), 0, titleY - 50 - i * 28, 600, 30);
                SetupTMP(subGO, daydreamFont, 14, Color.white, subtitles[i]);
            }

            // Name Label
            var nameLabelGO = FindOrCreate(inputGroup.transform, "UA_NameLabel");
            SetTopCenter(EnsureRectTransform(nameLabelGO), 0, nameLabelY, 400, 30);
            SetupTMP(nameLabelGO, daydreamFont, 18, Color.white, "Dog's Name:");

            // Name Input BG
            var nameInputBGGO = FindOrCreate(inputGroup.transform, "UA_NameInputBG");
            SetTopCenter(EnsureRectTransform(nameInputBGGO), 0, nameInputY, nameInputSize.x, nameInputSize.y);
            var nameInputBGImg = EnsureComponent<Image>(nameInputBGGO);
            nameInputBGImg.color = inputBg;

            // Name Input Field
            var nameInputGO = FindOrCreate(nameInputBGGO.transform, "UA_NameInputField");
            var nameInputRect = EnsureRectTransform(nameInputGO);
            SetStretchAll(nameInputRect);
            nameInputRect.offsetMin = new Vector2(10, 5);
            nameInputRect.offsetMax = new Vector2(-10, -5);

            // TMP_InputField needs Text Area child with text + placeholder
            var textAreaGO = FindOrCreate(nameInputGO.transform, "Text Area");
            var textAreaRect = EnsureRectTransform(textAreaGO);
            SetStretchAll(textAreaRect);

            var nameTextGO = FindOrCreate(textAreaGO.transform, "Text");
            var nameTextRect = EnsureRectTransform(nameTextGO);
            SetStretchAll(nameTextRect);
            var nameTextTmp = EnsureComponent<TextMeshProUGUI>(nameTextGO);
            nameTextTmp.font = daydreamFont;
            nameTextTmp.fontSize = 18;
            nameTextTmp.color = Color.white;
            nameTextTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var namePlaceholderGO = FindOrCreate(textAreaGO.transform, "Placeholder");
            var namePlaceholderRect = EnsureRectTransform(namePlaceholderGO);
            SetStretchAll(namePlaceholderRect);
            var namePlaceholderTmp = EnsureComponent<TextMeshProUGUI>(namePlaceholderGO);
            namePlaceholderTmp.font = daydreamFont;
            namePlaceholderTmp.fontSize = 18;
            namePlaceholderTmp.color = hintColor;
            namePlaceholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
            namePlaceholderTmp.text = "Enter name...";
            namePlaceholderTmp.fontStyle = FontStyles.Italic;

            var nameInput = EnsureComponent<TMP_InputField>(nameInputGO);
            nameInput.textComponent = nameTextTmp;
            nameInput.placeholder = namePlaceholderTmp;
            nameInput.textViewport = textAreaRect;
            nameInput.characterLimit = 20;
            nameInput.fontAsset = daydreamFont;

            // Photo Preview Area
            var photoAreaGO = FindOrCreate(inputGroup.transform, "UA_PhotoPreviewArea");
            SetTopCenter(EnsureRectTransform(photoAreaGO), 0, photoAreaY, 300, 200);

            var photoPreviewGO = FindOrCreate(photoAreaGO.transform, "UA_PhotoPreview");
            SetTopCenter(EnsureRectTransform(photoPreviewGO), 0, 0, 200, 200);
            var photoPreviewImg = EnsureComponent<Image>(photoPreviewGO);
            photoPreviewImg.preserveAspect = true;
            photoPreviewImg.raycastTarget = false;
            photoPreviewGO.SetActive(false);

            var placeholderGO = FindOrCreate(photoAreaGO.transform, "UA_PhotoPlaceholder");
            SetTopCenter(EnsureRectTransform(placeholderGO), 0, 0, placeholderSize.x, placeholderSize.y);
            var placeholderImg = EnsureComponent<Image>(placeholderGO);
            placeholderImg.color = inactiveBg;
            var placeholderOutline = EnsureComponent<Outline>(placeholderGO);
            placeholderOutline.effectColor = inputBg;
            placeholderOutline.effectDistance = new Vector2(2, -2);

            var phTextGO = FindOrCreate(placeholderGO.transform, "UA_PlaceholderText");
            SetTopCenter(EnsureRectTransform(phTextGO), 0, -40, 280, 30);
            SetupTMP(phTextGO, daydreamFont, 18, hintColor, "No photo selected");

            var phSubGO = FindOrCreate(placeholderGO.transform, "UA_PlaceholderSub");
            SetTopCenter(EnsureRectTransform(phSubGO), 0, -75, 280, 30);
            SetupTMP(phSubGO, daydreamFont, 14, hintColor, "Click Browse below");

            // Filename text
            var fileNameGO = FindOrCreate(inputGroup.transform, "UA_FileName");
            SetTopCenter(EnsureRectTransform(fileNameGO), 0, photoAreaY - 210, 400, 25);
            var fileNameTmp = SetupTMP(fileNameGO, daydreamFont, 14, successColor, "");
            fileNameGO.SetActive(false);

            // Browse button
            var browseBtnGO = FindOrCreate(inputGroup.transform, "UA_BrowseBtn");
            SetTopCenter(EnsureRectTransform(browseBtnGO), 0, browseBtnY, browseBtnSize.x, browseBtnSize.y);
            var browseBtnImg = EnsureComponent<Image>(browseBtnGO);
            browseBtnImg.color = btnNormal;

            var browseBtnTextGO = FindOrCreate(browseBtnGO.transform, "UA_BrowseBtnText");
            SetStretchAll(EnsureRectTransform(browseBtnTextGO));
            var browseBtnTmp = SetupTMP(browseBtnTextGO, daydreamFont, 18, Color.white, "Browse...");
            browseBtnTmp.raycastTarget = false;

            // Generate button
            var generateBtnGO = FindOrCreate(inputGroup.transform, "UA_GenerateBtn");
            SetTopCenter(EnsureRectTransform(generateBtnGO), 0, generateBtnY, generateBtnSize.x, generateBtnSize.y);
            var generateBtnImg = EnsureComponent<Image>(generateBtnGO);
            generateBtnImg.color = disabledBtn;

            var generateBtnTextGO = FindOrCreate(generateBtnGO.transform, "UA_GenerateBtnText");
            SetStretchAll(EnsureRectTransform(generateBtnTextGO));
            var generateBtnTmp = SetupTMP(generateBtnTextGO, daydreamFont, 18, new Color(1, 1, 1, 0.4f), "Generate Avatar!");
            generateBtnTmp.raycastTarget = false;

            // ========== GENERATING GROUP ==========
            var genGroup = FindOrCreate(panel.transform, "UA_GeneratingGroup");
            SetStretchAll(EnsureRectTransform(genGroup));
            genGroup.SetActive(false);

            var genTitleGO = FindOrCreate(genGroup.transform, "UA_GenTitle");
            SetTopCenter(EnsureRectTransform(genTitleGO), 0, -120, 600, 50);
            var genTitleTmp = SetupTMP(genTitleGO, daydreamFont, 28, gold, "Generating Avatar");

            var genWaitGO = FindOrCreate(genGroup.transform, "UA_GenWaitText");
            SetTopCenter(EnsureRectTransform(genWaitGO), 0, -180, 400, 30);
            var genWaitTmp = SetupTMP(genWaitGO, daydreamFont, 18, Color.white, "Please wait...");

            var genStepGO = FindOrCreate(genGroup.transform, "UA_GenStepDesc");
            SetTopCenter(EnsureRectTransform(genStepGO), 0, -230, 500, 30);
            var genStepTmp = SetupTMP(genStepGO, daydreamFont, 18, Color.white, "Analyzing dog features...");

            // Progress bar
            var progBGGO = FindOrCreate(genGroup.transform, "UA_ProgressBarBG");
            SetTopCenter(EnsureRectTransform(progBGGO), 0, -300, progressBarSize.x, progressBarSize.y);
            var progBGImg = EnsureComponent<Image>(progBGGO);
            progBGImg.color = inactiveBg;

            var progFillGO = FindOrCreate(progBGGO.transform, "UA_ProgressBarFill");
            SetStretchAll(EnsureRectTransform(progFillGO));
            var progFillImg = EnsureComponent<Image>(progFillGO);
            progFillImg.color = progressFill;
            progFillImg.type = Image.Type.Filled;
            progFillImg.fillMethod = Image.FillMethod.Horizontal;
            progFillImg.fillAmount = 0f;
            progFillImg.raycastTarget = false;

            var genCounterGO = FindOrCreate(genGroup.transform, "UA_GenStepCounter");
            SetTopCenter(EnsureRectTransform(genCounterGO), 0, -340, 200, 25);
            var genCounterTmp = SetupTMP(genCounterGO, daydreamFont, 14, Color.white, "Step 1/7");

            var genSourceGO = FindOrCreate(genGroup.transform, "UA_GenSourcePreview");
            SetTopCenter(EnsureRectTransform(genSourceGO), 0, -500, 200, 200);
            var genSourceImg = EnsureComponent<Image>(genSourceGO);
            genSourceImg.preserveAspect = true;
            genSourceImg.raycastTarget = false;

            var genNameGO = FindOrCreate(genGroup.transform, "UA_GenDogName");
            SetTopCenter(EnsureRectTransform(genNameGO), 0, -620, 300, 25);
            var genNameTmp = SetupTMP(genNameGO, daydreamFont, 14, gold, "");

            var genHintGO = FindOrCreate(genGroup.transform, "UA_GenHint");
            SetTopCenter(EnsureRectTransform(genHintGO), 0, -700, 400, 25);
            SetupTMP(genHintGO, daydreamFont, 14, hintColor, "This may take 1-2 minutes");

            // ========== COMPLETE GROUP ==========
            var compGroup = FindOrCreate(panel.transform, "UA_CompleteGroup");
            SetStretchAll(EnsureRectTransform(compGroup));
            compGroup.SetActive(false);

            var compTitleGO = FindOrCreate(compGroup.transform, "UA_CompTitle");
            SetTopCenter(EnsureRectTransform(compTitleGO), 0, -120, 600, 50);
            var compTitleTmp = SetupTMP(compTitleGO, daydreamFont, 28, successColor, "Avatar Created!");

            var compReadyGO = FindOrCreate(compGroup.transform, "UA_CompReadyText");
            SetTopCenter(EnsureRectTransform(compReadyGO), 0, -180, 600, 30);
            var compReadyTmp = SetupTMP(compReadyGO, daydreamFont, 18, Color.white, "is ready to play!");

            var compOrigGO = FindOrCreate(compGroup.transform, "UA_CompOriginal");
            SetTopCenter(EnsureRectTransform(compOrigGO), -150, -350, 100, 100);
            var compOrigImg = EnsureComponent<Image>(compOrigGO);
            compOrigImg.preserveAspect = true;
            compOrigImg.raycastTarget = false;

            var compOrigLabelGO = FindOrCreate(compGroup.transform, "UA_CompOrigLabel");
            SetTopCenter(EnsureRectTransform(compOrigLabelGO), -150, -420, 120, 20);
            SetupTMP(compOrigLabelGO, daydreamFont, 14, hintColor, "Original");

            var compArrowGO = FindOrCreate(compGroup.transform, "UA_CompArrow");
            SetTopCenter(EnsureRectTransform(compArrowGO), 0, -350, 80, 30);
            var compArrowTmp = SetupTMP(compArrowGO, daydreamFont, 18, gold, ">>>");

            var compGenGO = FindOrCreate(compGroup.transform, "UA_CompGenerated");
            SetTopCenter(EnsureRectTransform(compGenGO), 150, -350, 200, 200);
            var compGenImg = EnsureComponent<Image>(compGenGO);
            compGenImg.preserveAspect = true;
            compGenImg.raycastTarget = false;
            var compGenOutline = EnsureComponent<Outline>(compGenGO);
            compGenOutline.effectColor = gold;
            compGenOutline.effectDistance = new Vector2(4, -4);

            var compDoneBtnGO = FindOrCreate(compGroup.transform, "UA_CompDoneBtn");
            SetTopCenter(EnsureRectTransform(compDoneBtnGO), 0, -600, 300, 55);
            var compDoneBtnImg = EnsureComponent<Image>(compDoneBtnGO);
            compDoneBtnImg.color = btnNormal;

            var compDoneTextGO = FindOrCreate(compDoneBtnGO.transform, "UA_CompDoneBtnText");
            SetStretchAll(EnsureRectTransform(compDoneTextGO));
            var compDoneTmp = SetupTMP(compDoneTextGO, daydreamFont, 18, Color.white, "Choose Character!");
            compDoneTmp.raycastTarget = false;

            var compHintGO = FindOrCreate(compGroup.transform, "UA_CompHint");
            SetTopCenter(EnsureRectTransform(compHintGO), 0, -670, 400, 25);
            SetupTMP(compHintGO, daydreamFont, 14, hintColor, "Press Enter to continue");

            // ========== ERROR GROUP ==========
            var errGroup = FindOrCreate(panel.transform, "UA_ErrorGroup");
            SetStretchAll(EnsureRectTransform(errGroup));
            errGroup.SetActive(false);

            var errTitleGO = FindOrCreate(errGroup.transform, "UA_ErrTitle");
            SetTopCenter(EnsureRectTransform(errTitleGO), 0, -200, 600, 50);
            var errTitleTmp = SetupTMP(errTitleGO, daydreamFont, 28, errorColor, "Generation Failed");

            var errMsgGO = FindOrCreate(errGroup.transform, "UA_ErrMessage");
            SetTopCenter(EnsureRectTransform(errMsgGO), 0, -300, 500, 100);
            var errMsgTmp = SetupTMP(errMsgGO, daydreamFont, 14, Color.white, "An error occurred.");
            errMsgTmp.enableWordWrapping = true;

            var retryBtnGO = FindOrCreate(errGroup.transform, "UA_RetryBtn");
            SetTopCenter(EnsureRectTransform(retryBtnGO), 0, -500, 200, 50);
            var retryBtnImg = EnsureComponent<Image>(retryBtnGO);
            retryBtnImg.color = btnNormal;

            var retryTextGO = FindOrCreate(retryBtnGO.transform, "UA_RetryBtnText");
            SetStretchAll(EnsureRectTransform(retryTextGO));
            var retryTmp = SetupTMP(retryTextGO, daydreamFont, 18, Color.white, "Try Again");
            retryTmp.raycastTarget = false;

            var errBackBtnGO = FindOrCreate(errGroup.transform, "UA_ErrBackBtn");
            SetTopCenter(EnsureRectTransform(errBackBtnGO), 0, -570, 160, 40);
            var errBackBtnImg = EnsureComponent<Image>(errBackBtnGO);
            errBackBtnImg.color = backNormal;

            var errBackTextGO = FindOrCreate(errBackBtnGO.transform, "UA_ErrBackBtnText");
            SetStretchAll(EnsureRectTransform(errBackTextGO));
            var errBackTmp = SetupTMP(errBackTextGO, daydreamFont, 18, Color.white, "Back");
            errBackTmp.raycastTarget = false;

            // ========== API KEY GROUP ==========
            var apiGroup = FindOrCreate(panel.transform, "UA_ApiKeyGroup");
            SetStretchAll(EnsureRectTransform(apiGroup));
            apiGroup.SetActive(false);

            var apiTitleGO = FindOrCreate(apiGroup.transform, "UA_ApiTitle");
            SetTopCenter(EnsureRectTransform(apiTitleGO), 0, -200, 600, 50);
            SetupTMP(apiTitleGO, daydreamFont, 28, gold, "OpenRouter API Key");

            string[] apiInsts = { "An API key is needed to", "generate your dog avatar.", "Get one at openrouter.ai" };
            Color[] apiInstColors = { Color.white, Color.white, apiLink };
            for (int i = 0; i < 3; i++)
            {
                var instGO = FindOrCreate(apiGroup.transform, $"UA_ApiInst{i + 1}");
                SetTopCenter(EnsureRectTransform(instGO), 0, -280 - i * 28, 500, 25);
                SetupTMP(instGO, daydreamFont, 14, apiInstColors[i], apiInsts[i]);
            }

            var apiInputBGGO = FindOrCreate(apiGroup.transform, "UA_ApiInputBG");
            SetTopCenter(EnsureRectTransform(apiInputBGGO), 0, -420, 600, 50);
            var apiInputBGImg = EnsureComponent<Image>(apiInputBGGO);
            apiInputBGImg.color = inputBg;
            var apiInputOutline = EnsureComponent<Outline>(apiInputBGGO);
            apiInputOutline.effectColor = gold;
            apiInputOutline.effectDistance = new Vector2(2, -2);

            var apiInputGO = FindOrCreate(apiInputBGGO.transform, "UA_ApiInputField");
            var apiInputRect = EnsureRectTransform(apiInputGO);
            SetStretchAll(apiInputRect);
            apiInputRect.offsetMin = new Vector2(10, 5);
            apiInputRect.offsetMax = new Vector2(-10, -5);

            var apiTextAreaGO = FindOrCreate(apiInputGO.transform, "Text Area");
            var apiTextAreaRect = EnsureRectTransform(apiTextAreaGO);
            SetStretchAll(apiTextAreaRect);

            var apiTextGO = FindOrCreate(apiTextAreaGO.transform, "Text");
            SetStretchAll(EnsureRectTransform(apiTextGO));
            var apiTextTmp = EnsureComponent<TextMeshProUGUI>(apiTextGO);
            apiTextTmp.font = daydreamFont;
            apiTextTmp.fontSize = 14;
            apiTextTmp.color = Color.white;
            apiTextTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var apiPlaceholderGO = FindOrCreate(apiTextAreaGO.transform, "Placeholder");
            SetStretchAll(EnsureRectTransform(apiPlaceholderGO));
            var apiPlaceholderTmp = EnsureComponent<TextMeshProUGUI>(apiPlaceholderGO);
            apiPlaceholderTmp.font = daydreamFont;
            apiPlaceholderTmp.fontSize = 14;
            apiPlaceholderTmp.color = hintColor;
            apiPlaceholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
            apiPlaceholderTmp.text = "Paste API key here...";
            apiPlaceholderTmp.fontStyle = FontStyles.Italic;

            var apiInput = EnsureComponent<TMP_InputField>(apiInputGO);
            apiInput.textComponent = apiTextTmp;
            apiInput.placeholder = apiPlaceholderTmp;
            apiInput.textViewport = apiTextAreaRect;
            apiInput.contentType = TMP_InputField.ContentType.Password;
            apiInput.fontAsset = daydreamFont;

            var apiSubmitGO = FindOrCreate(apiGroup.transform, "UA_ApiSubmitHint");
            SetTopCenter(EnsureRectTransform(apiSubmitGO), 0, -500, 400, 25);
            var apiSubmitTmp = SetupTMP(apiSubmitGO, daydreamFont, 14, hintColor, "Press Enter to continue");

            var apiBackGO = FindOrCreate(apiGroup.transform, "UA_ApiBackHint");
            SetTopCenter(EnsureRectTransform(apiBackGO), 0, -535, 400, 25);
            var apiBackTmp = SetupTMP(apiBackGO, daydreamFont, 14, hintColor, "Press Escape to go back");

            // ========== SHARED BACK BUTTON ==========
            var backBtnGO = FindOrCreate(panel.transform, "UA_BackBtn");
            SetTopCenter(EnsureRectTransform(backBtnGO), 0, backBtnY, backBtnSize.x, backBtnSize.y);
            var backBtnImg = EnsureComponent<Image>(backBtnGO);
            backBtnImg.color = backNormal;

            var backBtnTextGO = FindOrCreate(backBtnGO.transform, "UA_BackBtnText");
            SetStretchAll(EnsureRectTransform(backBtnTextGO));
            var backBtnTmp = SetupTMP(backBtnTextGO, daydreamFont, 18, Color.white, "Back");
            backBtnTmp.raycastTarget = false;

            // ========== WIRE SERIALIZED FIELDS ==========
            var so = new SerializedObject(screen);

            var screenStateProp = so.FindProperty("screenState");
            if (screenStateProp != null)
                screenStateProp.enumValueIndex = (int)GameState.UploadAvatar;

            // Input state
            SetRef(so, "_inputGroup", inputGroup);
            SetRef(so, "_title", titleTmp);
            SetRef(so, "_nameInputField", nameInput);
            SetRef(so, "_photoPreview", photoPreviewImg);
            SetRef(so, "_photoPlaceholder", placeholderGO);
            SetRef(so, "_fileName", fileNameTmp);
            SetRef(so, "_browseBtnImage", browseBtnImg);
            SetRef(so, "_browseBtnText", browseBtnTmp);
            SetRef(so, "_generateBtnImage", generateBtnImg);
            SetRef(so, "_generateBtnText", generateBtnTmp);

            // Generating state
            SetRef(so, "_generatingGroup", genGroup);
            SetRef(so, "_genTitle", genTitleTmp);
            SetRef(so, "_genWaitText", genWaitTmp);
            SetRef(so, "_genStepDesc", genStepTmp);
            SetRef(so, "_progressBarBG", progBGImg);
            SetRef(so, "_progressBarFill", progFillImg);
            SetRef(so, "_genStepCounter", genCounterTmp);
            SetRef(so, "_genSourcePreview", genSourceImg);
            SetRef(so, "_genDogName", genNameTmp);
            SetRef(so, "_genHint", genHintGO.GetComponent<TMP_Text>());

            // Complete state
            SetRef(so, "_completeGroup", compGroup);
            SetRef(so, "_compTitle", compTitleTmp);
            SetRef(so, "_compReadyText", compReadyTmp);
            SetRef(so, "_compOriginal", compOrigImg);
            SetRef(so, "_compArrow", compArrowTmp);
            SetRef(so, "_compGenerated", compGenImg);
            SetRef(so, "_compDoneBtnImage", compDoneBtnImg);
            SetRef(so, "_compDoneBtnText", compDoneTmp);
            SetRef(so, "_compHint", compHintGO.GetComponent<TMP_Text>());

            // Error state
            SetRef(so, "_errorGroup", errGroup);
            SetRef(so, "_errTitle", errTitleTmp);
            SetRef(so, "_errMessage", errMsgTmp);
            SetRef(so, "_retryBtnImage", retryBtnImg);
            SetRef(so, "_retryBtnText", retryTmp);
            SetRef(so, "_errBackBtnImage", errBackBtnImg);
            SetRef(so, "_errBackBtnText", errBackTmp);

            // Api key state
            SetRef(so, "_apiKeyGroup", apiGroup);
            SetRef(so, "_apiTitle", apiTitleGO.GetComponent<TMP_Text>());
            SetRef(so, "_apiInputField", apiInput);
            SetRef(so, "_apiSubmitHint", apiSubmitTmp);
            SetRef(so, "_apiBackHint", apiBackTmp);

            // Shared
            SetRef(so, "_backBtnImage", backBtnImg);
            SetRef(so, "_backBtnText", backBtnTmp);

            so.ApplyModifiedProperties();

            // Wire OnValueChanged for name input
            var inputSO = new SerializedObject(nameInput);
            inputSO.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("UploadAvatarScreen setup complete!");
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
    }
}
