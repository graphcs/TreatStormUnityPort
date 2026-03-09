using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Avatar;
using Utilities.Inputs;

namespace SnackAttack.Screens
{
    public class UploadAvatarScreen : BaseScreen
    {
        private enum AvatarScreenState { Input, Generating, Complete, Error, ApiKey }

        [Header("Input State")]
        [SerializeField] private GameObject _inputGroup;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private Image _photoPreview;
        [SerializeField] private GameObject _photoPlaceholder;
        [SerializeField] private TMP_Text _fileName;
        [SerializeField] private Image _browseBtnImage;
        [SerializeField] private TMP_Text _browseBtnText;
        [SerializeField] private Image _generateBtnImage;
        [SerializeField] private TMP_Text _generateBtnText;

        [Header("Generating State")]
        [SerializeField] private GameObject _generatingGroup;
        [SerializeField] private TMP_Text _genTitle;
        [SerializeField] private TMP_Text _genWaitText;
        [SerializeField] private TMP_Text _genStepDesc;
        [SerializeField] private Image _progressBarBG;
        [SerializeField] private Image _progressBarFill;
        [SerializeField] private TMP_Text _genStepCounter;
        [SerializeField] private Image _genSourcePreview;
        [SerializeField] private TMP_Text _genDogName;
        [SerializeField] private TMP_Text _genHint;

        [Header("Complete State")]
        [SerializeField] private GameObject _completeGroup;
        [SerializeField] private TMP_Text _compTitle;
        [SerializeField] private TMP_Text _compReadyText;
        [SerializeField] private Image _compOriginal;
        [SerializeField] private TMP_Text _compArrow;
        [SerializeField] private Image _compGenerated;
        [SerializeField] private Image _compDoneBtnImage;
        [SerializeField] private TMP_Text _compDoneBtnText;
        [SerializeField] private TMP_Text _compHint;

        [Header("Error State")]
        [SerializeField] private GameObject _errorGroup;
        [SerializeField] private TMP_Text _errTitle;
        [SerializeField] private TMP_Text _errMessage;
        [SerializeField] private Image _retryBtnImage;
        [SerializeField] private TMP_Text _retryBtnText;
        [SerializeField] private Image _errBackBtnImage;
        [SerializeField] private TMP_Text _errBackBtnText;

        [Header("Api Key State")]
        [SerializeField] private GameObject _apiKeyGroup;
        [SerializeField] private TMP_Text _apiTitle;
        [SerializeField] private TMP_InputField _apiInputField;
        [SerializeField] private TMP_Text _apiSubmitHint;
        [SerializeField] private TMP_Text _apiBackHint;

        [Header("Shared")]
        [SerializeField] private Image _backBtnImage;
        [SerializeField] private TMP_Text _backBtnText;

        // State
        private AvatarScreenState _currentState;
        private string _dogName = "";
        private string _photoPath = "";
        private Sprite _photoSprite;
        private Texture2D _photoTexture;
        private string _gameMode;
        private bool _vsAi;
        private bool _active;
        private bool _inputCooldown;
        private float _dotTimer;
        private int _dotCount;
        private Coroutine _generateCoroutine;
        private string _apiKey;
        private AvatarGenerationResult _generationResult;
        private AvatarGenerator _avatarGenerator;

        // Cached
        private UIColorsSO _colors;
        private UILayoutSO _layout;
        private ControlsSO _controls;

        // Hover state
        private bool _browseHovered;
        private bool _generateHovered;
        private bool _doneHovered;
        private bool _retryHovered;
        private bool _errBackHovered;
        private bool _backHovered;

        public override void OnEnter(Dictionary<string, object> data)
        {
            base.OnEnter(data);

            _colors = GM.UIColors;
            _layout = GM.UILayout;
            _controls = GM.Controls;

            _gameMode = data != null && data.ContainsKey("mode") ? (string)data["mode"] : "single_dog";
            _vsAi = data != null && data.ContainsKey("vs_ai") && (bool)data["vs_ai"];

            _dogName = "";
            _photoPath = "";
            _generationResult = null;
            CleanupPhoto();

            if (_nameInputField != null)
            {
                _nameInputField.text = "";
                _nameInputField.characterLimit = 20;
            }

            // Ensure AvatarGenerator exists
            if (_avatarGenerator == null)
            {
                _avatarGenerator = GameManager.Instance.GetComponent<AvatarGenerator>();
                if (_avatarGenerator == null)
                    _avatarGenerator = GameManager.Instance.gameObject.AddComponent<AvatarGenerator>();
            }

            // Load API key from PlayerPrefs
            _apiKey = PlayerPrefs.GetString("sa_openRouterKey", "");

            // Check for API key — if missing, start in ApiKey state
            if (string.IsNullOrEmpty(_apiKey))
                SetState(AvatarScreenState.ApiKey);
            else
                SetState(AvatarScreenState.Input);

            _inputCooldown = true;
            _active = true;
        }

        public override void OnExit()
        {
            _active = false;
            if (_generateCoroutine != null)
            {
                StopCoroutine(_generateCoroutine);
                _generateCoroutine = null;
            }
            CleanupPhoto();
            base.OnExit();
        }

        private void Update()
        {
            if (!_active || !InputsManager.Started) return;

            if (_inputCooldown)
            {
                _inputCooldown = false;
                return;
            }

            // Animated dots in generating state
            if (_currentState == AvatarScreenState.Generating)
            {
                _dotTimer += Time.unscaledDeltaTime;
                if (_dotTimer >= 0.5f)
                {
                    _dotTimer = 0f;
                    _dotCount = (_dotCount % 4) + 1;
                    if (_genWaitText != null)
                        _genWaitText.text = "Please wait" + new string('.', _dotCount);
                }
            }

            HandleKeyboardInput();
            HandleMouseInput();
        }

        private void HandleKeyboardInput()
        {
            string cancelAction = _controls != null ? _controls.cancelAction : "Cancel";
            string submitAction = _controls != null ? _controls.submitAction : "Submit";

            switch (_currentState)
            {
                case AvatarScreenState.Input:
                    if (InputsManager.InputDown(cancelAction))
                        GoBackToCharSelect();
                    else if (InputsManager.InputDown(submitAction) && CanGenerate())
                        StartGeneration();
                    break;

                case AvatarScreenState.Generating:
                    // No keyboard actions during generation
                    break;

                case AvatarScreenState.Complete:
                    if (InputsManager.InputDown(submitAction) || InputsManager.InputDown(cancelAction))
                        OnCompleteConfirm();
                    break;

                case AvatarScreenState.Error:
                    if (InputsManager.InputDown(cancelAction))
                        GoBackToCharSelect();
                    else if (InputsManager.InputDown(submitAction))
                        SetState(AvatarScreenState.Input);
                    break;

                case AvatarScreenState.ApiKey:
                    if (InputsManager.InputDown(cancelAction))
                        GoBackToCharSelect();
                    else if (InputsManager.InputDown(submitAction))
                        SubmitApiKey();
                    break;
            }
        }

        private void HandleMouseInput()
        {
            Vector2 mousePos = InputsManager.InputMousePosition();
            bool click = InputsManager.InputMouseButtonUp(0);

            // Reset hover states
            bool prevBrowse = _browseHovered, prevGen = _generateHovered, prevDone = _doneHovered;
            bool prevRetry = _retryHovered, prevErrBack = _errBackHovered, prevBack = _backHovered;
            _browseHovered = _generateHovered = _doneHovered = false;
            _retryHovered = _errBackHovered = _backHovered = false;

            switch (_currentState)
            {
                case AvatarScreenState.Input:
                    if (_browseBtnImage != null && IsHovered(_browseBtnImage.rectTransform, mousePos))
                    {
                        _browseHovered = true;
                        if (click) OnBrowseClicked();
                    }
                    if (_generateBtnImage != null && IsHovered(_generateBtnImage.rectTransform, mousePos))
                    {
                        _generateHovered = true;
                        if (click && CanGenerate()) StartGeneration();
                    }
                    break;

                case AvatarScreenState.Complete:
                    if (_compDoneBtnImage != null && IsHovered(_compDoneBtnImage.rectTransform, mousePos))
                    {
                        _doneHovered = true;
                        if (click) OnCompleteConfirm();
                    }
                    break;

                case AvatarScreenState.Error:
                    if (_retryBtnImage != null && IsHovered(_retryBtnImage.rectTransform, mousePos))
                    {
                        _retryHovered = true;
                        if (click) SetState(AvatarScreenState.Input);
                    }
                    if (_errBackBtnImage != null && IsHovered(_errBackBtnImage.rectTransform, mousePos))
                    {
                        _errBackHovered = true;
                        if (click) GoBackToCharSelect();
                    }
                    break;
            }

            // Shared back button (visible in Input and ApiKey states)
            if (_currentState != AvatarScreenState.Generating &&
                _backBtnImage != null && IsHovered(_backBtnImage.rectTransform, mousePos))
            {
                _backHovered = true;
                if (click) GoBackToCharSelect();
            }

            // Update button visuals on hover change
            if (_browseHovered != prevBrowse || _generateHovered != prevGen ||
                _doneHovered != prevDone || _retryHovered != prevRetry ||
                _errBackHovered != prevErrBack || _backHovered != prevBack)
            {
                UpdateButtonColors();
            }
        }

        private bool IsHovered(RectTransform rect, Vector2 mousePos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null);
        }

        private void SetState(AvatarScreenState state)
        {
            _currentState = state;

            if (_inputGroup != null) _inputGroup.SetActive(state == AvatarScreenState.Input);
            if (_generatingGroup != null) _generatingGroup.SetActive(state == AvatarScreenState.Generating);
            if (_completeGroup != null) _completeGroup.SetActive(state == AvatarScreenState.Complete);
            if (_errorGroup != null) _errorGroup.SetActive(state == AvatarScreenState.Error);
            if (_apiKeyGroup != null) _apiKeyGroup.SetActive(state == AvatarScreenState.ApiKey);

            // Back button visible except during generating
            if (_backBtnImage != null)
                _backBtnImage.gameObject.SetActive(state != AvatarScreenState.Generating &&
                                                    state != AvatarScreenState.Complete &&
                                                    state != AvatarScreenState.Error);

            UpdateButtonColors();

            // Focus input fields
            if (state == AvatarScreenState.Input && _nameInputField != null)
                _nameInputField.ActivateInputField();
            if (state == AvatarScreenState.ApiKey && _apiInputField != null)
                _apiInputField.ActivateInputField();
        }

        private bool CanGenerate()
        {
            return !string.IsNullOrWhiteSpace(_dogName) && _photoSprite != null;
        }

        private void UpdateButtonColors()
        {
            Color btnNormal = _colors != null ? _colors.avatarButtonNormal : new Color32(80, 140, 80, 255);
            Color btnHover = _colors != null ? _colors.avatarButtonHover : new Color32(100, 180, 100, 255);
            Color backNormal = _colors != null ? _colors.avatarBackNormal : new Color32(100, 70, 60, 255);
            Color backHover = _colors != null ? _colors.avatarBackHover : new Color32(130, 100, 80, 255);
            Color disabled = _colors != null ? _colors.avatarDisabledBtn : new Color32(60, 60, 80, 255);

            if (_browseBtnImage != null)
                _browseBtnImage.color = _browseHovered ? btnHover : btnNormal;

            if (_generateBtnImage != null)
            {
                if (!CanGenerate())
                    _generateBtnImage.color = disabled;
                else
                    _generateBtnImage.color = _generateHovered ? btnHover : btnNormal;
            }
            if (_generateBtnText != null)
                _generateBtnText.color = CanGenerate() ? Color.white : new Color(1f, 1f, 1f, 0.4f);

            if (_compDoneBtnImage != null)
                _compDoneBtnImage.color = _doneHovered ? btnHover : btnNormal;

            if (_retryBtnImage != null)
                _retryBtnImage.color = _retryHovered ? btnHover : btnNormal;

            if (_errBackBtnImage != null)
                _errBackBtnImage.color = _errBackHovered ? backHover : backNormal;

            if (_backBtnImage != null)
                _backBtnImage.color = _backHovered ? backHover : backNormal;
        }

        private void OnBrowseClicked()
        {
            string path = NativeFilePicker.OpenImageFile();
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);
                if (!tex.LoadImage(bytes))
                {
                    Object.Destroy(tex);
                    return;
                }

                CleanupPhoto();
                _photoTexture = tex;
                _photoSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
                _photoPath = path;

                if (_photoPreview != null)
                {
                    _photoPreview.sprite = _photoSprite;
                    _photoPreview.gameObject.SetActive(true);
                    _photoPreview.preserveAspect = true;
                }

                if (_photoPlaceholder != null)
                    _photoPlaceholder.SetActive(false);

                if (_fileName != null)
                {
                    _fileName.gameObject.SetActive(true);
                    _fileName.text = Path.GetFileName(path);
                }

                // Auto-focus name if empty
                if (string.IsNullOrEmpty(_dogName) && _nameInputField != null)
                    _nameInputField.ActivateInputField();

                UpdateButtonColors();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UploadAvatarScreen] Failed to load image: {e.Message}");
            }
        }

        private void StartGeneration()
        {
            _dogName = _nameInputField != null ? _nameInputField.text.Trim() : _dogName;
            if (!CanGenerate()) return;

            PlaySound("select");
            SetState(AvatarScreenState.Generating);

            if (_genSourcePreview != null)
            {
                _genSourcePreview.sprite = _photoSprite;
                _genSourcePreview.preserveAspect = true;
            }
            if (_genDogName != null)
                _genDogName.text = _dogName;

            _dotTimer = 0f;
            _dotCount = 0;

            _generateCoroutine = _avatarGenerator.StartGeneration(
                _photoPath, _dogName, _apiKey,
                OnGenerationProgress, OnGenerationComplete);
        }

        private void OnGenerationProgress(AvatarGenerationProgress progress)
        {
            if (_genStepDesc != null)
                _genStepDesc.text = progress.stepDescription;
            if (_genStepCounter != null)
                _genStepCounter.text = $"Step {progress.currentStep}/{progress.totalSteps}";
            if (_progressBarFill != null)
                _progressBarFill.fillAmount = (float)progress.currentStep / progress.totalSteps;
        }

        private void OnGenerationComplete(AvatarGenerationResult result)
        {
            _generateCoroutine = null;

            if (result.success)
            {
                _generationResult = result;
                ShowComplete(result);
            }
            else
            {
                if (_errMessage != null)
                    _errMessage.text = result.errorMessage ?? "Generation failed. Please try again.";
                SetState(AvatarScreenState.Error);
            }
        }

        private void ShowComplete(AvatarGenerationResult result)
        {
            SetState(AvatarScreenState.Complete);

            if (_compReadyText != null)
                _compReadyText.text = $"{result.characterName} is ready to play!";

            if (_compOriginal != null)
            {
                _compOriginal.sprite = _photoSprite;
                _compOriginal.preserveAspect = true;
            }

            // Show generated profile portrait
            if (_compGenerated != null && result.character != null && result.character.portrait != null)
            {
                _compGenerated.sprite = result.character.portrait;
                _compGenerated.preserveAspect = true;
            }
            else if (_compGenerated != null)
            {
                _compGenerated.sprite = _photoSprite;
                _compGenerated.preserveAspect = true;
            }
        }

        private void OnCompleteConfirm()
        {
            PlaySound("select");
            // Return to character select with the new character available
            GoBackToCharSelect();
        }

        private void SubmitApiKey()
        {
            if (_apiInputField == null) return;
            string key = _apiInputField.text.Trim();
            if (string.IsNullOrEmpty(key)) return;

            _apiKey = key;
            PlayerPrefs.SetString("sa_openRouterKey", key);
            PlayerPrefs.Save();
            PlaySound("select");
            SetState(AvatarScreenState.Input);
        }

        private void GoBackToCharSelect()
        {
            PlaySound("select");
            var data = new Dictionary<string, object>
            {
                { "mode", _gameMode },
                { "vs_ai", _vsAi }
            };
            ChangeState(GameState.CharacterSelect, data);
        }

        private void CleanupPhoto()
        {
            if (_photoTexture != null)
            {
                Destroy(_photoTexture);
                _photoTexture = null;
            }
            if (_photoSprite != null)
            {
                Destroy(_photoSprite);
                _photoSprite = null;
            }

            if (_photoPreview != null)
                _photoPreview.gameObject.SetActive(false);
            if (_photoPlaceholder != null)
                _photoPlaceholder.SetActive(true);
            if (_fileName != null)
                _fileName.gameObject.SetActive(false);
        }

        // Called by TMP_InputField OnValueChanged
        public void OnNameChanged(string newName)
        {
            _dogName = newName.Trim();
            UpdateButtonColors();
        }
    }
}
