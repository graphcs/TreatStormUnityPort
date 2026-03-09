using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;

namespace SnackAttack.Interaction
{
    public class ChatSimulator : MonoBehaviour
    {
        [SerializeField] private RectTransform _messageArea;
        [SerializeField] private RectTransform _buttonArea;
        [SerializeField] private Image _autoToggleBg;
        [SerializeField] private TMP_Text _autoToggleText;

        private VotingSystem _votingSystem;
        private VotingSettingsSO _settings;
        private TMP_FontAsset _font;
        private CanvasGroup _canvasGroup;

        private bool _autoVoteEnabled = false; // PyGame: auto_vote starts False
        private float _autoVoteTimer;
        private int _nextBotId = 1;

        // Message display
        private readonly List<TMP_Text> _messageTexts = new();
        private int _maxVisible;
        private float _rowHeight;

        // Vote buttons
        private readonly List<GameObject> _voteButtons = new();
        private string[] _currentOptions;
        private Color[] _currentColors;

        // Max visible messages (PyGame stores 15 max, renders last 12)
        private int _maxStored = 15;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Hide();
        }

        public void Initialize(VotingSystem system, VotingSettingsSO settings, TMP_FontAsset font)
        {
            _votingSystem = system;
            _settings = settings;
            _font = font;
            _maxVisible = settings != null ? settings.maxVisibleMessages : 12;
            _maxStored = settings?.maxStoredMessages ?? 15;
            _rowHeight = settings != null ? settings.messageRowHeight : 16f;
            _nextBotId = 1;

            SetupAutoToggle();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        public void SetVoteOptions(string[] options, Color[] colors)
        {
            _currentOptions = options;
            _currentColors = colors;
            ClearMessages();
            ClearButtons();
            CreateVoteButtons(options, colors);

            float interval = _settings != null ? _settings.autoVoteInterval : 2f;
            float variance = _settings != null ? _settings.autoVoteVariance : 0.5f;
            _autoVoteTimer = interval + Random.Range(-variance, variance);
        }

        public void ClearVoting()
        {
            ClearButtons();
            _currentOptions = null;
            _currentColors = null;
        }

        public void AddMessage(string user, string text, Color color)
        {
            if (_messageArea == null) return;

            // Shift existing messages up, remove oldest if over limit (PyGame stores 15, shows last 12)
            if (_messageTexts.Count >= _maxStored)
            {
                var oldest = _messageTexts[0];
                _messageTexts.RemoveAt(0);
                Destroy(oldest.gameObject);
            }

            // Reposition all existing
            int xOff = _settings?.messageXOffset ?? 4;
            for (int i = 0; i < _messageTexts.Count; i++)
            {
                var rt = _messageTexts[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(xOff, -(i * _rowHeight));
            }

            // Create new message at bottom
            var go = new GameObject("Msg");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(_messageArea, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(xOff, -(_messageTexts.Count * _rowHeight));
            rect.sizeDelta = new Vector2(-8, _rowHeight);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            tmp.text = $"<color=#{colorHex}>{user}</color>: {text}";
            tmp.fontSize = _settings?.messageFontSize ?? 9;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            if (_font != null) tmp.font = _font;

            _messageTexts.Add(tmp);
        }

        public void OnVoteButtonClicked(int idx)
        {
            if (_votingSystem == null || _currentOptions == null) return;
            if (idx < 0 || idx >= _currentOptions.Length) return;

            Color c = _currentColors != null && idx < _currentColors.Length ? _currentColors[idx] : Color.white;
            _votingSystem.AddVote("Player", idx);
            AddMessage("You", $"!{_currentOptions[idx]}", c);
        }

        private void Update()
        {
            if (_votingSystem == null || !_autoVoteEnabled) return;
            if (_votingSystem.Phase != VotePhase.Voting) return;
            if (_currentOptions == null || _currentOptions.Length == 0) return;

            _autoVoteTimer -= Time.deltaTime;
            if (_autoVoteTimer <= 0f)
            {
                GenerateAutoVote();
                float interval = _settings != null ? _settings.autoVoteInterval : 2f;
                float variance = _settings != null ? _settings.autoVoteVariance : 0.5f;
                _autoVoteTimer = interval + Random.Range(-variance, variance);
            }
        }

        private void GenerateAutoVote()
        {
            if (_votingSystem == null || _currentOptions == null) return;

            // Smart vote: pick option with fewest votes to prevent blowouts
            int minVotes = int.MaxValue;
            int minIdx = 0;
            var counts = _votingSystem.VoteCounts;
            if (counts != null)
            {
                for (int i = 0; i < counts.Length; i++)
                {
                    if (counts[i] < minVotes)
                    {
                        minVotes = counts[i];
                        minIdx = i;
                    }
                }
                // Add some randomness: smartVoteThreshold chance to pick fewest
                if (Random.value > (_settings?.smartVoteThreshold ?? 0.6f))
                    minIdx = Random.Range(0, _currentOptions.Length);
            }
            else
            {
                minIdx = Random.Range(0, _currentOptions.Length);
            }

            string botName = GetNextBotName();
            Color c = _currentColors != null && minIdx < _currentColors.Length ? _currentColors[minIdx] : Color.white;

            _votingSystem.AddVote(botName, minIdx);
            AddMessage(botName, $"!{_currentOptions[minIdx]}", c);
        }

        private string GetNextBotName()
        {
            // PyGame: bot_name = f"Bot{self.next_bot_id}", wraps modulo 99
            string name = $"Bot{_nextBotId}";
            _nextBotId = (_nextBotId % 99) + 1;
            return name;
        }

        private void CreateVoteButtons(string[] options, Color[] colors)
        {
            if (_buttonArea == null) return;

            float btnHeight = _settings?.buttonHeight ?? 28f;
            float btnGap = _settings?.buttonGap ?? 4f;
            int btnXOff = _settings?.buttonXOffset ?? 4;
            float btnAlpha = _settings?.buttonBackgroundAlpha ?? 0.3f;

            for (int i = 0; i < options.Length; i++)
            {
                float yPos = -(i * (btnHeight + btnGap));

                var btnGo = new GameObject($"VoteBtn_{i}");
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.SetParent(_buttonArea, false);
                btnRect.anchorMin = new Vector2(0, 1);
                btnRect.anchorMax = new Vector2(1, 1);
                btnRect.pivot = new Vector2(0, 1);
                btnRect.anchoredPosition = new Vector2(btnXOff, yPos);
                btnRect.sizeDelta = new Vector2(-8, btnHeight);

                var btnImg = btnGo.AddComponent<Image>();
                Color bgColor = colors != null && i < colors.Length ? colors[i] : Color.gray;
                bgColor.a = btnAlpha;
                btnImg.color = bgColor;

                var btn = btnGo.AddComponent<Button>();
                int capturedIdx = i;
                btn.onClick.AddListener(() => OnVoteButtonClicked(capturedIdx));

                var labelGo = new GameObject("Label");
                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.SetParent(btnRect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = $"!{options[i]}";
                label.fontSize = _settings?.buttonLabelFontSize ?? 10;
                label.alignment = TextAlignmentOptions.Center;
                label.color = colors != null && i < colors.Length ? colors[i] : Color.white;
                label.raycastTarget = false;
                if (_font != null) label.font = _font;

                _voteButtons.Add(btnGo);
            }
        }

        private void ClearButtons()
        {
            foreach (var btn in _voteButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _voteButtons.Clear();
        }

        private void ClearMessages()
        {
            foreach (var msg in _messageTexts)
            {
                if (msg != null) Destroy(msg.gameObject);
            }
            _messageTexts.Clear();
        }

        private void SetupAutoToggle()
        {
            if (_autoToggleBg != null)
            {
                _autoToggleBg.color = _autoVoteEnabled ? (_settings?.autoToggleOnColor ?? new Color(0.3f, 0.8f, 0.3f)) : Color.gray;

                var btn = _autoToggleBg.GetComponent<Button>();
                if (btn == null) btn = _autoToggleBg.gameObject.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ToggleAutoVote);
            }
        }

        private void ToggleAutoVote()
        {
            _autoVoteEnabled = !_autoVoteEnabled;
            if (_autoToggleBg != null)
                _autoToggleBg.color = _autoVoteEnabled ? (_settings?.autoToggleOnColor ?? new Color(0.3f, 0.8f, 0.3f)) : Color.gray;
            if (_autoToggleText != null)
                _autoToggleText.text = "AUTO";
            // PyGame adds system messages on toggle
            if (_autoVoteEnabled)
                AddMessage("System", "Auto-vote ON", _settings?.systemAutoOnColor ?? new Color(200f / 255f, 200f / 255f, 100f / 255f));
            else
                AddMessage("System", "Auto-vote OFF", _settings?.systemAutoOffColor ?? new Color(150f / 255f, 150f / 255f, 150f / 255f));
        }

        private void OnDestroy()
        {
            ClearButtons();
            ClearMessages();
        }
    }
}
