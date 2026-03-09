using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Interaction;
using Utilities.Inputs;

namespace SnackAttack.Screens
{
    public class TwitchSetupScreen : BaseScreen
    {
        [Header("Items")]
        [SerializeField] private TMP_Text _enabledLabel;
        [SerializeField] private TMP_Text _enabledValue;
        [SerializeField] private TMP_Text _channelLabel;
        [SerializeField] private TMP_Text _channelValue;
        [SerializeField] private TMP_Text _tokenLabel;
        [SerializeField] private TMP_Text _tokenValue;
        [SerializeField] private TMP_Text _usernameLabel;
        [SerializeField] private TMP_Text _usernameValue;
        [SerializeField] private TMP_Text _testLabel;
        [SerializeField] private TMP_Text _statusText;

        [Header("Input Overlay")]
        [SerializeField] private GameObject _inputOverlay;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TMP_Text _inputLabel;

        [Header("Navigation")]
        [SerializeField] private TMP_Text _backText;
        [SerializeField] private Image _selectIndicator;
        [SerializeField] private TMP_Text _footerText;

        private const int ITEM_COUNT = 6; // 5 items + Back
        private const string KEY_ENABLED = "sa_twitchEnabled";
        private const string KEY_CHANNEL = "sa_twitchChannel";
        private const string KEY_TOKEN = "sa_twitchToken";
        private const string KEY_USERNAME = "sa_twitchBotUsername";

        private int _selectedIndex;
        private bool _active;
        private bool _inputActive;
        private int _editingItem = -1;

        private UIColorsSO _colors;
        private ControlsSO _controls;
        private TwitchConfigSO _twitchConfig;

        private RectTransform[] _itemRects;
        private TMP_Text[] _labels;

        // Cached values
        private bool _enabled;
        private string _channel;
        private string _token;
        private string _username;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void OnEnter(Dictionary<string, object> data)
        {
            base.OnEnter(data);

            var gm = GM;
            _colors = gm.UIColors;
            _controls = gm.Controls;
            _twitchConfig = gm.TwitchConfig;

            LoadFromPlayerPrefs();
            CacheItemRects();

            _labels = new TMP_Text[]
            {
                _enabledLabel, _channelLabel, _tokenLabel, _usernameLabel, _testLabel, _backText
            };

            _selectedIndex = 0;
            _active = true;
            _inputActive = false;

            if (_inputOverlay != null)
                _inputOverlay.SetActive(false);

            UpdateConnectionStatus();
            RefreshAllVisuals();
        }

        public override void OnExit()
        {
            _active = false;
            if (_inputOverlay != null)
                _inputOverlay.SetActive(false);
            if (_selectIndicator != null)
                _selectIndicator.enabled = false;
            base.OnExit();
        }

        private void Update()
        {
            if (!_active) return;

            if (_inputActive)
            {
                HandleInputOverlay();
                return;
            }

            HandleKeyboardInput();
            HandleMouseInput();
        }

        private void HandleKeyboardInput()
        {
            if (!InputsManager.Started) return;

            string vAxis = _controls != null ? _controls.verticalAxis : "Vertical";
            string submitAction = _controls != null ? _controls.submitAction : "Submit";
            string cancelAction = _controls != null ? _controls.cancelAction : "Cancel";

            if (InputsManager.InputNegativeHold(vAxis) || InputsManager.InputNegativeDown(vAxis))
                ChangeSelection(1);
            else if (InputsManager.InputPositiveHold(vAxis) || InputsManager.InputPositiveDown(vAxis))
                ChangeSelection(-1);
            else if (InputsManager.InputDown(submitAction))
                ActivateSelected();
            else if (InputsManager.InputDown(cancelAction))
                GoBackToSettings();
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
                        PlaySound(_controls != null ? _controls.selectSound : "select");
                    }
                    if (InputsManager.InputMouseButtonUp(0))
                        ActivateSelected();
                    break;
                }
            }

            if (_backText != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_backText.rectTransform, mousePos, null))
            {
                if (_selectedIndex != 5)
                {
                    _selectedIndex = 5;
                    RefreshAllVisuals();
                    PlaySound(_controls != null ? _controls.selectSound : "select");
                }
                if (InputsManager.InputMouseButtonUp(0))
                    GoBackToSettings();
            }
        }

        private void HandleInputOverlay()
        {
            if (InputsManager.InputDown("Submit"))
            {
                ConfirmInput();
            }
            else if (InputsManager.InputDown("Cancel"))
            {
                CancelInput();
            }
        }

        private void CacheItemRects()
        {
            _itemRects = new RectTransform[5];
            if (_enabledLabel != null) _itemRects[0] = _enabledLabel.transform.parent as RectTransform;
            if (_channelLabel != null) _itemRects[1] = _channelLabel.transform.parent as RectTransform;
            if (_tokenLabel != null) _itemRects[2] = _tokenLabel.transform.parent as RectTransform;
            if (_usernameLabel != null) _itemRects[3] = _usernameLabel.transform.parent as RectTransform;
            if (_testLabel != null) _itemRects[4] = _testLabel.transform.parent as RectTransform;
        }

        private void ChangeSelection(int direction)
        {
            _selectedIndex = ((_selectedIndex + direction) % ITEM_COUNT + ITEM_COUNT) % ITEM_COUNT;
            RefreshAllVisuals();
            PlaySound(_controls != null ? _controls.selectSound : "select");
        }

        private void ActivateSelected()
        {
            PlaySound(_controls != null ? _controls.selectSound : "select");

            switch (_selectedIndex)
            {
                case 0: // Enabled toggle
                    _enabled = !_enabled;
                    PlayerPrefs.SetInt(KEY_ENABLED, _enabled ? 1 : 0);
                    PlayerPrefs.Save();

                    // Connect/disconnect immediately
                    var twitch = GM.TwitchChat;
                    if (twitch != null)
                    {
                        if (_enabled && !string.IsNullOrEmpty(_channel) && !string.IsNullOrEmpty(_token))
                        {
                            string user = string.IsNullOrEmpty(_username) ?
                                (_twitchConfig != null ? _twitchConfig.defaultBotUsername : "SnackAttackBot") :
                                _username;
                            twitch.Connect(_channel, _token, user);
                        }
                        else if (!_enabled)
                        {
                            twitch.Disconnect();
                        }
                    }
                    RefreshAllVisuals();
                    break;
                case 1: // Channel
                    ShowInputOverlay("Enter Channel Name:", _channel, false);
                    _editingItem = 1;
                    break;
                case 2: // OAuth Token
                    ShowInputOverlay("Enter OAuth Token:", _token, true);
                    _editingItem = 2;
                    break;
                case 3: // Bot Username
                    string defaultUser = _twitchConfig != null ? _twitchConfig.defaultBotUsername : "SnackAttackBot";
                    ShowInputOverlay("Enter Bot Username:", string.IsNullOrEmpty(_username) ? defaultUser : _username, false);
                    _editingItem = 3;
                    break;
                case 4: // Test Connection
                    TestConnection();
                    break;
                case 5: // Back
                    GoBackToSettings();
                    break;
            }
        }

        private void ShowInputOverlay(string label, string currentValue, bool isPassword)
        {
            if (_inputOverlay == null || _inputField == null) return;

            _inputActive = true;
            _inputOverlay.SetActive(true);

            if (_inputLabel != null)
                _inputLabel.text = label;

            _inputField.text = currentValue ?? "";
            _inputField.contentType = isPassword ?
                TMP_InputField.ContentType.Password :
                TMP_InputField.ContentType.Standard;
            _inputField.ForceLabelUpdate();
            _inputField.ActivateInputField();
            _inputField.Select();
        }

        private void ConfirmInput()
        {
            string value = _inputField.text.Trim();

            switch (_editingItem)
            {
                case 1:
                    _channel = value;
                    PlayerPrefs.SetString(KEY_CHANNEL, value);
                    break;
                case 2:
                    _token = value;
                    PlayerPrefs.SetString(KEY_TOKEN, value);
                    break;
                case 3:
                    _username = value;
                    PlayerPrefs.SetString(KEY_USERNAME, value);
                    break;
            }
            PlayerPrefs.Save();

            _inputActive = false;
            _editingItem = -1;
            if (_inputOverlay != null)
                _inputOverlay.SetActive(false);

            RefreshAllVisuals();
        }

        private void CancelInput()
        {
            _inputActive = false;
            _editingItem = -1;
            if (_inputOverlay != null)
                _inputOverlay.SetActive(false);
        }

        private void TestConnection()
        {
            if (string.IsNullOrEmpty(_channel) || string.IsNullOrEmpty(_token))
            {
                SetStatusText("Channel and token required", _twitchConfig?.disconnectedColor ?? Color.red);
                return;
            }

            string user = string.IsNullOrEmpty(_username) ?
                (_twitchConfig != null ? _twitchConfig.defaultBotUsername : "SnackAttackBot") :
                _username;

            SetStatusText("Testing...", _twitchConfig?.connectingColor ?? Color.yellow);

            var twitch = GM.TwitchChat;
            if (twitch != null)
            {
                twitch.TestConnectionAsync(_channel, _token, user, (success, message) =>
                {
                    if (success)
                        SetStatusText(message, _twitchConfig?.connectedColor ?? Color.green);
                    else
                        SetStatusText(message, _twitchConfig?.disconnectedColor ?? Color.red);
                });
            }
            else
            {
                SetStatusText("TwitchChatManager not found", _twitchConfig?.disconnectedColor ?? Color.red);
            }
        }

        private void GoBackToSettings()
        {
            PlaySound(_controls != null ? _controls.selectSound : "select");
            ChangeState(GameState.Settings);
        }

        private void LoadFromPlayerPrefs()
        {
            _enabled = PlayerPrefs.GetInt(KEY_ENABLED, 0) == 1;
            _channel = PlayerPrefs.GetString(KEY_CHANNEL, "");
            _token = PlayerPrefs.GetString(KEY_TOKEN, "");
            _username = PlayerPrefs.GetString(KEY_USERNAME, "");
        }

        private void UpdateConnectionStatus()
        {
            var twitch = GM.TwitchChat;
            if (twitch == null)
            {
                SetStatusText("Not available", _twitchConfig?.disconnectedColor ?? Color.red);
                return;
            }

            switch (twitch.State)
            {
                case TwitchConnectionState.Connected:
                    SetStatusText("Connected", _twitchConfig?.connectedColor ?? Color.green);
                    break;
                case TwitchConnectionState.Connecting:
                    SetStatusText("Connecting...", _twitchConfig?.connectingColor ?? Color.yellow);
                    break;
                case TwitchConnectionState.Error:
                    SetStatusText(twitch.ErrorMessage ?? "Error", _twitchConfig?.disconnectedColor ?? Color.red);
                    break;
                default:
                    SetStatusText("Disconnected", _twitchConfig?.disconnectedColor ?? Color.red);
                    break;
            }
        }

        private void SetStatusText(string text, Color color)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
                _statusText.color = color;
            }
        }

        private void RefreshAllVisuals()
        {
            Color menuNormal = _colors != null ? _colors.menuNormal : new Color32(77, 43, 31, 255);
            Color menuSelected = _colors != null ? _colors.menuSelected : new Color32(147, 76, 48, 255);
            Color onColor = _colors != null ? _colors.popupPositiveColor : new Color32(81, 180, 71, 255);
            Color offColor = _colors != null ? _colors.popupNegativeColor : new Color32(222, 97, 91, 255);

            if (_labels != null)
            {
                for (int i = 0; i < _labels.Length; i++)
                {
                    if (_labels[i] != null)
                        _labels[i].color = i == _selectedIndex ? menuSelected : menuNormal;
                }
            }

            // Enabled toggle
            if (_enabledValue != null)
            {
                _enabledValue.text = _enabled ? "ON" : "OFF";
                _enabledValue.color = _enabled ? onColor : offColor;
            }

            // Channel
            if (_channelValue != null)
                _channelValue.text = string.IsNullOrEmpty(_channel) ? "<not set>" : _channel;

            // Token (masked)
            if (_tokenValue != null)
                _tokenValue.text = string.IsNullOrEmpty(_token) ? "<not set>" : "****";

            // Username
            if (_usernameValue != null)
            {
                string defaultUser = _twitchConfig != null ? _twitchConfig.defaultBotUsername : "SnackAttackBot";
                _usernameValue.text = string.IsNullOrEmpty(_username) ? defaultUser : _username;
            }

            // Test label
            if (_testLabel != null)
                _testLabel.text = "Test Connection";

            UpdateSelectIndicator();
        }

        private readonly Vector3[] _corners = new Vector3[4];

        private void UpdateSelectIndicator()
        {
            if (_selectIndicator == null) return;

            _selectIndicator.enabled = true;
            var indicatorRect = _selectIndicator.rectTransform;
            var parentRect = indicatorRect.parent as RectTransform;
            if (parentRect == null) return;

            TMP_Text targetText;
            if (_selectedIndex < 5)
                targetText = _labels != null && _selectedIndex < _labels.Length ? _labels[_selectedIndex] : null;
            else
                targetText = _backText;

            if (targetText == null) return;

            targetText.rectTransform.GetWorldCorners(_corners);
            float textWorldLeft = _corners[0].x;
            float textWorldCenterY = (_corners[0].y + _corners[1].y) / 2f;

            if (_selectedIndex == 5)
            {
                float textWorldCenterX = (_corners[0].x + _corners[3].x) / 2f;
                textWorldLeft = textWorldCenterX - targetText.preferredWidth / 2f;
            }

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, new Vector2(textWorldLeft, textWorldCenterY), null, out localPoint);

            float iconX = localPoint.x - indicatorRect.rect.width - 6f;
            indicatorRect.localPosition = new Vector3(iconX, localPoint.y, 0);
        }
    }
}
