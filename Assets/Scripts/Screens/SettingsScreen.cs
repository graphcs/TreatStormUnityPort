using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using Utilities.Inputs;

namespace SnackAttack.Screens
{
    public class SettingsScreen : BaseScreen
    {
        [Header("Background & Container")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _container;
        [SerializeField] private Image _titleImage;

        [Header("Music Toggle (Item 0)")]
        [SerializeField] private TMP_Text _musicLabel;
        [SerializeField] private TMP_Text _musicValue;

        [Header("SFX Toggle (Item 1)")]
        [SerializeField] private TMP_Text _sfxLabel;
        [SerializeField] private TMP_Text _sfxValue;

        [Header("Music Volume Slider (Item 2)")]
        [SerializeField] private TMP_Text _musicVolLabel;
        [SerializeField] private Image _musicVolSliderBG;
        [SerializeField] private Image _musicVolSliderFill;
        [SerializeField] private Outline _musicVolSliderOutline;

        [Header("SFX Volume Slider (Item 3)")]
        [SerializeField] private TMP_Text _sfxVolLabel;
        [SerializeField] private Image _sfxVolSliderBG;
        [SerializeField] private Image _sfxVolSliderFill;
        [SerializeField] private Outline _sfxVolSliderOutline;

        [Header("Master Volume Slider (Item 4)")]
        [SerializeField] private TMP_Text _masterVolLabel;
        [SerializeField] private Image _masterVolSliderBG;
        [SerializeField] private Image _masterVolSliderFill;
        [SerializeField] private Outline _masterVolSliderOutline;

        [Header("Navigation")]
        [SerializeField] private TMP_Text _backText;
        [SerializeField] private Image _selectIndicator;
        [SerializeField] private TMP_Text _footerText;

        private const int ITEM_COUNT = 6; // 5 items + Back
        private const float VOLUME_STEP = 0.1f;

        private const string KEY_MUSIC_ENABLED = "sa_musicEnabled";
        private const string KEY_SFX_ENABLED = "sa_sfxEnabled";
        private const string KEY_MASTER_VOLUME = "sa_masterVolume";
        private const string KEY_MUSIC_VOLUME = "sa_musicVolume";
        private const string KEY_SFX_VOLUME = "sa_sfxVolume";

        private int _selectedIndex;
        private bool _active;

        // Cached SO refs
        private AudioSettingsSO _audio;
        private UIColorsSO _colors;
        private UILayoutSO _layout;
        private ControlsSO _controls;

        // Item row RectTransforms for mouse hit testing
        private RectTransform[] _itemRects;

        // All label texts for color updates
        private TMP_Text[] _labels;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            _audio = GM.AudioSettings;
            LoadFromPlayerPrefs(_audio);
        }

        public override void OnEnter(Dictionary<string, object> data)
        {
            base.OnEnter(data);

            var gm = GM;
            _audio = gm.AudioSettings;
            _colors = gm.UIColors;
            _layout = gm.UILayout;
            _controls = gm.Controls;

            LoadFromPlayerPrefs(_audio);

            CacheItemRects();
            _labels = new TMP_Text[]
            {
                _musicLabel, _sfxLabel, _musicVolLabel, _sfxVolLabel, _masterVolLabel, _backText
            };

            _selectedIndex = 0;
            _active = true;

            RefreshAllVisuals();
        }

        public override void OnExit()
        {
            _active = false;
            SaveToPlayerPrefs(_audio);
            if (_selectIndicator != null)
                _selectIndicator.enabled = false;
            base.OnExit();
        }

        private void Update()
        {
            if (!_active) return;

            HandleKeyboardInput();
            HandleMouseInput();
        }

        // --- Input ---

        private void HandleKeyboardInput()
        {
            if (!InputsManager.Started) return;

            string vAxis = _controls != null ? _controls.verticalAxis : "Vertical";
            string hAxis = _controls != null ? _controls.horizontalAxis : "Horizontal";
            string submitAction = _controls != null ? _controls.submitAction : "Submit";
            string cancelAction = _controls != null ? _controls.cancelAction : "Cancel";

            if (InputsManager.InputNegativeHold(vAxis) || InputsManager.InputNegativeDown(vAxis))
            {
                ChangeSelection(1);
            }
            else if (InputsManager.InputPositiveHold(vAxis) || InputsManager.InputPositiveDown(vAxis))
            {
                ChangeSelection(-1);
            }
            else if (InputsManager.InputDown(submitAction))
            {
                ActivateSelected();
            }
            else if (InputsManager.InputDown(cancelAction))
            {
                GoBackToMenu();
            }
            else if (InputsManager.InputNegativeHold(hAxis) || InputsManager.InputNegativeDown(hAxis))
            {
                AdjustValue(-1);
            }
            else if (InputsManager.InputPositiveHold(hAxis) || InputsManager.InputPositiveDown(hAxis))
            {
                AdjustValue(1);
            }
        }

        private void HandleMouseInput()
        {
            if (_itemRects == null || !InputsManager.Started) return;

            Vector2 mousePos = InputsManager.InputMousePosition();

            for (int i = 0; i < _itemRects.Length; i++)
            {
                if (_itemRects[i] == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(_itemRects[i], mousePos, null))
                {
                    if (i != _selectedIndex)
                    {
                        _selectedIndex = i;
                        RefreshAllVisuals();
                        string selectSnd = _controls != null ? _controls.selectSound : "select";
                        PlaySound(selectSnd);
                    }

                    if (InputsManager.InputMouseButtonUp(0))
                    {
                        ActivateSelected();
                    }
                    break;
                }
            }

            // Check Back text
            if (_backText != null)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_backText.rectTransform, mousePos, null))
                {
                    if (_selectedIndex != 5)
                    {
                        _selectedIndex = 5;
                        RefreshAllVisuals();
                        string selectSnd = _controls != null ? _controls.selectSound : "select";
                        PlaySound(selectSnd);
                    }

                    if (InputsManager.InputMouseButtonUp(0))
                    {
                        GoBackToMenu();
                    }
                }
            }
        }

        private void CacheItemRects()
        {
            _itemRects = new RectTransform[5];

            // Items 0-4 are the parent RectTransforms of the labels
            if (_musicLabel != null) _itemRects[0] = _musicLabel.transform.parent as RectTransform;
            if (_sfxLabel != null) _itemRects[1] = _sfxLabel.transform.parent as RectTransform;
            if (_musicVolLabel != null) _itemRects[2] = _musicVolLabel.transform.parent as RectTransform;
            if (_sfxVolLabel != null) _itemRects[3] = _sfxVolLabel.transform.parent as RectTransform;
            if (_masterVolLabel != null) _itemRects[4] = _masterVolLabel.transform.parent as RectTransform;
        }

        private void ChangeSelection(int direction)
        {
            _selectedIndex = ((_selectedIndex + direction) % ITEM_COUNT + ITEM_COUNT) % ITEM_COUNT;
            RefreshAllVisuals();
            string selectSnd = _controls != null ? _controls.selectSound : "select";
            PlaySound(selectSnd);
        }

        private void ActivateSelected()
        {
            string selectSnd = _controls != null ? _controls.selectSound : "select";
            PlaySound(selectSnd);

            switch (_selectedIndex)
            {
                case 0: // Music toggle
                    _audio.musicEnabled = !_audio.musicEnabled;
                    EmitSettingsChanged();
                    RefreshAllVisuals();
                    break;
                case 1: // SFX toggle
                    _audio.sfxEnabled = !_audio.sfxEnabled;
                    EmitSettingsChanged();
                    RefreshAllVisuals();
                    break;
                case 5: // Back
                    GoBackToMenu();
                    break;
                // Sliders don't respond to Enter
            }
        }

        private void AdjustValue(int direction)
        {
            switch (_selectedIndex)
            {
                case 0: // Music toggle
                    _audio.musicEnabled = !_audio.musicEnabled;
                    EmitSettingsChanged();
                    RefreshAllVisuals();
                    break;
                case 1: // SFX toggle
                    _audio.sfxEnabled = !_audio.sfxEnabled;
                    EmitSettingsChanged();
                    RefreshAllVisuals();
                    break;
                case 2: // Music volume
                    _audio.musicVolume = Mathf.Clamp01(_audio.musicVolume + direction * VOLUME_STEP);
                    EmitSettingsChanged();
                    RefreshAllVisuals();
                    break;
                case 3: // SFX volume
                    _audio.sfxVolume = Mathf.Clamp01(_audio.sfxVolume + direction * VOLUME_STEP);
                    EmitSettingsChanged();
                    RefreshAllVisuals();
                    break;
                case 4: // Master volume
                    _audio.masterVolume = Mathf.Clamp01(_audio.masterVolume + direction * VOLUME_STEP);
                    EmitSettingsChanged();
                    RefreshAllVisuals();
                    break;
            }
        }

        private void GoBackToMenu()
        {
            string selectSnd = _controls != null ? _controls.selectSound : "select";
            PlaySound(selectSnd);
            SaveToPlayerPrefs(_audio);
            ChangeState(GameState.MainMenu);
        }

        // --- Visuals ---

        private void RefreshAllVisuals()
        {
            Color menuNormal = _colors != null ? _colors.menuNormal : new Color32(77, 43, 31, 255);
            Color menuSelected = _colors != null ? _colors.menuSelected : new Color32(147, 76, 48, 255);
            Color onColor = _colors != null ? _colors.popupPositiveColor : new Color32(81, 180, 71, 255);
            Color offColor = _colors != null ? _colors.popupNegativeColor : new Color32(222, 97, 91, 255);
            Color sliderBg = _colors != null ? _colors.sliderBackground : Color.white;

            // Update label colors
            if (_labels != null)
            {
                for (int i = 0; i < _labels.Length; i++)
                {
                    if (_labels[i] != null)
                        _labels[i].color = i == _selectedIndex ? menuSelected : menuNormal;
                }
            }

            // Toggle values
            if (_musicValue != null)
            {
                _musicValue.text = _audio.musicEnabled ? "ON" : "OFF";
                _musicValue.color = _audio.musicEnabled ? onColor : offColor;
            }

            if (_sfxValue != null)
            {
                _sfxValue.text = _audio.sfxEnabled ? "ON" : "OFF";
                _sfxValue.color = _audio.sfxEnabled ? onColor : offColor;
            }

            // Sliders
            UpdateSlider(_musicVolSliderBG, _musicVolSliderFill, _musicVolSliderOutline,
                         _audio.musicVolume, sliderBg, menuNormal, menuSelected, 2);
            UpdateSlider(_sfxVolSliderBG, _sfxVolSliderFill, _sfxVolSliderOutline,
                         _audio.sfxVolume, sliderBg, menuNormal, menuSelected, 3);
            UpdateSlider(_masterVolSliderBG, _masterVolSliderFill, _masterVolSliderOutline,
                         _audio.masterVolume, sliderBg, menuNormal, menuSelected, 4);

            // Select indicator
            UpdateSelectIndicator();
        }

        private void UpdateSlider(Image bg, Image fill, Outline outline,
                                  float value, Color bgColor, Color normalColor, Color selectedColor, int itemIndex)
        {
            if (bg != null)
                bg.color = bgColor;

            Color selectionColor = _selectedIndex == itemIndex ? selectedColor : normalColor;

            if (fill != null)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillAmount = value;
                fill.color = selectionColor;
            }

            if (outline != null)
                outline.effectColor = selectionColor;
        }

        private readonly Vector3[] _corners = new Vector3[4];

        private void UpdateSelectIndicator()
        {
            if (_selectIndicator == null) return;

            _selectIndicator.enabled = true;
            var indicatorRect = _selectIndicator.rectTransform;
            var parentRect = indicatorRect.parent as RectTransform;
            if (parentRect == null) return;

            float offsetX = _layout != null ? _layout.settingsSelectOffsetX : 6f;

            TMP_Text targetText;
            if (_selectedIndex < 5)
                targetText = _labels != null && _selectedIndex < _labels.Length ? _labels[_selectedIndex] : null;
            else
                targetText = _backText;

            if (targetText == null) return;

            // Use world corners to find the text's left edge and vertical center,
            // independent of any anchor/pivot configuration
            targetText.rectTransform.GetWorldCorners(_corners);
            // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
            float textWorldLeft = _corners[0].x;
            float textWorldCenterY = (_corners[0].y + _corners[1].y) / 2f;

            if (_selectedIndex == 5)
            {
                // Back is center-aligned — compute actual text left edge from preferred width
                float textWorldCenterX = (_corners[0].x + _corners[3].x) / 2f;
                textWorldLeft = textWorldCenterX - targetText.preferredWidth / 2f;
            }
            else
            {
                _selectIndicator.enabled = false; // For non-back items, rely on label color change instead of showing the indicator
            }

            // Convert the world-space left edge to the indicator parent's local space
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, new Vector2(textWorldLeft, textWorldCenterY), null, out localPoint);

            float iconX = localPoint.x - indicatorRect.rect.width - offsetX;
            indicatorRect.localPosition = new Vector3(iconX, localPoint.y, 0);
        }

        // --- Persistence ---

        private void EmitSettingsChanged()
        {
            EventBus.Emit(GameEvent.SettingsChanged);
        }

        public static void LoadFromPlayerPrefs(AudioSettingsSO audio)
        {
            if (audio == null) return;

            if (PlayerPrefs.HasKey(KEY_MUSIC_ENABLED))
                audio.musicEnabled = PlayerPrefs.GetInt(KEY_MUSIC_ENABLED) == 1;
            if (PlayerPrefs.HasKey(KEY_SFX_ENABLED))
                audio.sfxEnabled = PlayerPrefs.GetInt(KEY_SFX_ENABLED) == 1;
            if (PlayerPrefs.HasKey(KEY_MASTER_VOLUME))
                audio.masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME);
            if (PlayerPrefs.HasKey(KEY_MUSIC_VOLUME))
                audio.musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME);
            if (PlayerPrefs.HasKey(KEY_SFX_VOLUME))
                audio.sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME);
        }

        public static void SaveToPlayerPrefs(AudioSettingsSO audio)
        {
            if (audio == null) return;

            PlayerPrefs.SetInt(KEY_MUSIC_ENABLED, audio.musicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SFX_ENABLED, audio.sfxEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, audio.masterVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, audio.musicVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, audio.sfxVolume);
            PlayerPrefs.Save();
        }
    }
}
