using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;

namespace SnackAttack.Interaction
{
    public class VotingMeter : MonoBehaviour
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private RectTransform _barsContainer;

        private VotingSystem _votingSystem;
        private VotingSettingsSO _settings;
        private TMP_FontAsset _font;
        private CanvasGroup _canvasGroup;

        // Dynamic bar elements (horizontal side-by-side, matching PyGame)
        private Image[] _barFills;
        private Image[] _barBgs;
        private TMP_Text[] _barLabels;
        private RectTransform[] _barBgRects;
        private int _optionCount;

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
        }

        public void SetOptions(string[] options, Color[] colors)
        {
            ClearBars();
            _optionCount = options.Length;

            if (_barsContainer == null || _optionCount == 0) return;

            _barFills = new Image[_optionCount];
            _barBgs = new Image[_optionCount];
            _barLabels = new TMP_Text[_optionCount];
            _barBgRects = new RectTransform[_optionCount];

            // PyGame layout: bars start at y=45 from meter top, margin=20 on each side
            // Bars are HORIZONTAL side-by-side with gaps between them
            float barMargin = _settings?.barMargin ?? 20f;
            float barH = _settings != null ? _settings.barHeight : 20f;
            float gap = _settings != null ? _settings.barGap : 10f;
            float containerW = _barsContainer.rect.width > 0 ? _barsContainer.rect.width : (_settings?.meterSize.x ?? 300f);
            float availableW = containerW - (barMargin * 2f);
            float totalGaps = (_optionCount - 1) * gap;
            float barWidth = Mathf.Max(1f, (availableW - totalGaps) / _optionCount);

            // Bars positioned at y=-45 from top of container (PyGame: bar_y = rect.y + 45)
            float barY = _settings?.meterBarY ?? -45f;

            for (int i = 0; i < _optionCount; i++)
            {
                float xPos = barMargin + i * (barWidth + gap);
                int colorIdx = i % (_settings != null ? _settings.barBgColors.Length : 1);

                // Bar background
                var bgGo = new GameObject($"BarBg_{i}");
                var bgRect = bgGo.AddComponent<RectTransform>();
                bgRect.SetParent(_barsContainer, false);
                bgRect.anchorMin = new Vector2(0, 1);
                bgRect.anchorMax = new Vector2(0, 1);
                bgRect.pivot = new Vector2(0, 1);
                bgRect.anchoredPosition = new Vector2(xPos, barY);
                bgRect.sizeDelta = new Vector2(barWidth, barH);
                var bgImg = bgGo.AddComponent<Image>();
                bgImg.color = _settings != null && colorIdx < _settings.barBgColors.Length
                    ? _settings.barBgColors[colorIdx] : new Color(0.7f, 0.7f, 0.7f);
                bgImg.raycastTarget = false;
                _barBgs[i] = bgImg;
                _barBgRects[i] = bgRect;

                // Bar fill (anchored left, grows right via width)
                var fillGo = new GameObject($"BarFill_{i}");
                var fillRect = fillGo.AddComponent<RectTransform>();
                fillRect.SetParent(bgRect, false);
                fillRect.anchorMin = new Vector2(0, 0);
                fillRect.anchorMax = new Vector2(0, 1);
                fillRect.pivot = new Vector2(0, 0.5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = new Vector2(0, 0);
                var fillImg = fillGo.AddComponent<Image>();
                Color fillColor = colors != null && i < colors.Length ? colors[i]
                    : (_settings != null && colorIdx < _settings.barColors.Length
                        ? _settings.barColors[colorIdx] : Color.green);
                fillImg.color = fillColor;
                fillImg.raycastTarget = false;
                _barFills[i] = fillImg;

                // Label below bar (centered)
                var labelGo = new GameObject($"BarLabel_{i}");
                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.SetParent(_barsContainer, false);
                labelRect.anchorMin = new Vector2(0, 1);
                labelRect.anchorMax = new Vector2(0, 1);
                labelRect.pivot = new Vector2(0.5f, 1);
                // Center label below bar: x = xPos + barWidth/2, y = barY - barH - labelYOffset
                float labelYOffset = _settings?.barLabelYOffset ?? 5f;
                labelRect.anchoredPosition = new Vector2(xPos + barWidth * 0.5f, barY - barH - labelYOffset);
                float labelHeight = _settings?.barLabelHeight ?? 14f;
                labelRect.sizeDelta = new Vector2(barWidth + 10f, labelHeight);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                // Truncate long labels
                int truncLen = _settings?.barLabelTruncateLength ?? 8;
                string labelText = options[i];
                if (labelText.Length > truncLen)
                    labelText = labelText.Substring(0, truncLen - 2) + "..";
                label.text = labelText;
                label.fontSize = _settings?.barLabelFontSize ?? 8;
                label.alignment = TextAlignmentOptions.Center;
                label.color = fillColor;
                label.raycastTarget = false;
                if (_font != null) label.font = _font;
                _barLabels[i] = label;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
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

        private void Update()
        {
            if (_votingSystem == null || _barFills == null) return;

            // Update status text (right-aligned in PyGame)
            if (_statusText != null)
            {
                var phase = _votingSystem.Phase;
                if (phase == VotePhase.Voting)
                {
                    // PyGame uses int() truncation, not CeilToInt
                    int secs = (int)_votingSystem.TimeRemaining;
                    // PyGame: "TRIVIA: Xs" for trivia mode, "Voting: Xs" otherwise
                    if (_votingSystem.Mode == VoteMode.Trivia)
                        _statusText.text = $"TRIVIA: {secs}s";
                    else
                        _statusText.text = $"Voting: {secs}s";
                }
                else if (phase == VotePhase.Cooldown && _votingSystem.WinnerIndex >= 0)
                {
                    string winner = _votingSystem.Options[_votingSystem.WinnerIndex];
                    int statusTrunc = _settings?.statusTruncateLength ?? 10;
                    if (winner.Length >= statusTrunc)
                        winner = winner.Substring(0, statusTrunc - 2) + "..";
                    int secs = (int)_votingSystem.TimeRemaining;
                    _statusText.text = $"{winner.ToUpper()}! ({secs}s)";
                }
                else
                {
                    _statusText.text = "Crowd Chaos soon";
                }

                // Right-align status text (PyGame: right-aligned)
                _statusText.alignment = TextAlignmentOptions.MidlineRight;
            }

            // Update bar fills
            var counts = _votingSystem.VoteCounts;
            if (counts == null) return;

            int total = _votingSystem.TotalVotes;

            for (int i = 0; i < _optionCount && i < counts.Length; i++)
            {
                float pct = total > 0 ? (float)counts[i] / total : 0f;
                float barWidth = _barBgRects[i].sizeDelta.x;
                float fillW = barWidth * pct;

                var fillRect = _barFills[i].GetComponent<RectTransform>();
                fillRect.sizeDelta = new Vector2(fillW, 0);
            }
        }

        private void ClearBars()
        {
            if (_barsContainer == null) return;

            for (int i = _barsContainer.childCount - 1; i >= 0; i--)
                Destroy(_barsContainer.GetChild(i).gameObject);

            _barFills = null;
            _barBgs = null;
            _barLabels = null;
            _barBgRects = null;
            _optionCount = 0;
        }
    }
}
