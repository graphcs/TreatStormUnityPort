using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using Utilities.Inputs;

namespace SnackAttack.Screens
{
    public class CharacterSelectScreen : BaseScreen
    {
        [Header("Character Select References")]
        [SerializeField] private Image background;
        [SerializeField] private Image titleImage;
        [SerializeField] private TMP_Text playerIndicator;
        [SerializeField] private RectTransform cardsContainer;
        [SerializeField] private TMP_Text createDogText;
        [SerializeField] private TMP_Text backText;
        [SerializeField] private Image selectIndicator;
        [SerializeField] private TMP_Text footerText;
        [SerializeField] private Sprite borderSprite;

        private const int CardsPerRow = 3;
        private const float CardWidth = 225f;
        private const float CardHeight = 225f;
        private const float SpacingX = 32f;
        private const float SpacingY = 32f;

        // Colors
        private static readonly Color P1Color = new Color(0.392f, 0.706f, 1.0f);
        private static readonly Color P2Color = new Color(1.0f, 0.471f, 0.471f);
        private static readonly Color BothColor = new Color(0.784f, 0.588f, 1.0f);
        private static readonly Color UnselectedBorder = new Color(0.235f, 0.275f, 0.392f);
        private static readonly Color HighlightGold = new Color(1.0f, 0.863f, 0.314f);
        private static readonly Color BackDefault = new Color(0.302f, 0.169f, 0.122f);
        private static readonly Color BackHover = new Color(0.576f, 0.298f, 0.188f);
        private static readonly Color CreateDogDefault = new Color(0.784f, 0.667f, 0.235f);
        private static readonly Color CreateDogHover = new Color(1.0f, 0.863f, 0.314f);

        // State
        private string _gameMode;
        private bool _vsAi;
        private int _p1Selection;
        private int _p2Selection = -1;
        private int _activePlayer = 1;
        private bool _p1Confirmed;
        private bool _backSelected;
        private bool _active;
        private bool _inputCooldown;
        private List<CharacterSO> _characters;
        private List<RectTransform> _cardRects;
        private List<Outline> _cardOutlines;
        private ScrollRect _scrollRect;
        private GameObject _scrollViewGO;

        // PyGame scroll zone: Y 310→850 from top (height 540)
        private const float ScrollZoneTop = 310f;
        private const float ScrollZoneHeight = 540f;

        private static readonly string[] CharacterOrder = { "jazzy", "biggie", "dash", "snowy", "prissy", "rex" };

        public override void OnEnter(Dictionary<string, object> data)
        {
            base.OnEnter(data);

            _gameMode = data != null && data.ContainsKey("mode") ? (string)data["mode"] : "single_dog";
            _vsAi = data != null && data.ContainsKey("vs_ai") && (bool)data["vs_ai"];

            _p1Selection = 0;
            _p2Selection = -1;
            _activePlayer = 1;
            _p1Confirmed = false;
            _backSelected = false;

            BuildCharacterList();
            BuildCardGrid();
            UpdatePlayerIndicator();
            UpdateAllCardVisuals();
            UpdateBackVisual();
            UpdateCreateDogVisual();

            if (selectIndicator != null)
                selectIndicator.enabled = false;

            _inputCooldown = true;
            _active = true;
        }

        public override void OnExit()
        {
            _active = false;
            DestroyCards();
            base.OnExit();
        }

        private void BuildCharacterList()
        {
            _characters = new List<CharacterSO>();
            var db = GM.CharacterDatabase;

            // Add characters in PyGame order
            foreach (string id in CharacterOrder)
            {
                var c = db.GetById(id);
                if (c != null)
                    _characters.Add(c);
            }

            // Append any not in the predefined order
            for (int i = 0; i < db.Count; i++)
            {
                var c = db.characters[i];
                if (!_characters.Contains(c))
                    _characters.Add(c);
            }
        }

        private void BuildCardGrid()
        {
            DestroyCards();
            SetupScrollView();

            // Configure GridLayoutGroup on container
            var grid = cardsContainer.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = cardsContainer.gameObject.AddComponent<GridLayoutGroup>();

            grid.cellSize = new Vector2(CardWidth, CardHeight);
            grid.spacing = new Vector2(SpacingX, SpacingY);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CardsPerRow;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;

            // Add ContentSizeFitter so container grows to fit all cards
            var fitter = cardsContainer.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = cardsContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _cardRects = new List<RectTransform>();
            _cardOutlines = new List<Outline>();

            for (int i = 0; i < _characters.Count; i++)
            {
                // Card root — GridLayoutGroup handles size and position
                var cardGO = new GameObject($"Card_{_characters[i].id}");
                var cardRect = cardGO.AddComponent<RectTransform>();
                cardRect.SetParent(cardsContainer, false);

                var borderImg = cardGO.AddComponent<Image>();
                borderImg.sprite = borderSprite;
                borderImg.preserveAspect = true;
                borderImg.color = Color.white;

                var outline = cardGO.AddComponent<Outline>();
                outline.effectColor = P1Color;
                outline.effectDistance = new Vector2(4, -4);
                outline.enabled = false;

                // Portrait child fills the card on top
                var portraitGO = new GameObject("Portrait");
                var portraitRect = portraitGO.AddComponent<RectTransform>();
                portraitRect.SetParent(cardRect, false);
                portraitRect.pivot = new Vector2(0.5f, 0.5f);
                portraitRect.anchorMin = Vector2.zero;
                portraitRect.anchorMax = Vector2.one;
                portraitRect.offsetMin = Vector2.zero;
                portraitRect.offsetMax = Vector2.zero;

                var portraitImg = portraitGO.AddComponent<Image>();
                portraitImg.sprite = _characters[i].portrait;
                portraitImg.preserveAspect = true;
                portraitImg.color = Color.white;

                _cardRects.Add(cardRect);
                _cardOutlines.Add(outline);
            }
        }

        private void SetupScrollView()
        {
            if (_scrollViewGO != null) return;

            var panel = cardsContainer.parent;

            // Create scroll view GO
            _scrollViewGO = new GameObject("CS_ScrollView");
            var svRect = _scrollViewGO.AddComponent<RectTransform>();
            svRect.SetParent(panel, false);
            svRect.anchorMin = new Vector2(0, 1);
            svRect.anchorMax = new Vector2(1, 1);
            svRect.pivot = new Vector2(0.5f, 1);
            svRect.anchoredPosition = new Vector2(0, -ScrollZoneTop);
            svRect.sizeDelta = new Vector2(0, ScrollZoneHeight);

            // Create viewport child (RectTransform only, no mask)
            var viewportGO = new GameObject("Viewport");
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.SetParent(svRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            // Add ScrollRect
            _scrollRect = _scrollViewGO.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 40f;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.viewport = viewportRect;

            // Reparent cardsContainer into viewport
            cardsContainer.SetParent(viewportRect, false);
            cardsContainer.anchorMin = new Vector2(0, 1);
            cardsContainer.anchorMax = new Vector2(1, 1);
            cardsContainer.pivot = new Vector2(0.5f, 1);
            cardsContainer.anchoredPosition = Vector2.zero;

            // Transparent image on cardsContainer (required for ScrollRect content raycasting)
            var containerImg = cardsContainer.GetComponent<Image>();
            if (containerImg == null)
                containerImg = cardsContainer.gameObject.AddComponent<Image>();
            containerImg.color = new Color(0, 0, 0, 0);

            _scrollRect.content = cardsContainer;
        }

        private void DestroyCards()
        {
            if (_cardRects != null)
            {
                for (int i = _cardRects.Count - 1; i >= 0; i--)
                {
                    if (_cardRects[i] != null)
                        Destroy(_cardRects[i].gameObject);
                }
            }
            _cardRects = null;
            _cardOutlines = null;

            // Reparent cardsContainer back and destroy scroll view
            if (_scrollViewGO != null)
            {
                var panel = _scrollViewGO.transform.parent;
                cardsContainer.SetParent(panel, false);
                Destroy(_scrollViewGO);
                _scrollViewGO = null;
                _scrollRect = null;
            }
        }

        private void Update()
        {
            if (!_active || !InputsManager.Started) return;

            if (_inputCooldown)
            {
                _inputCooldown = false;
                return;
            }

            HandleKeyboardInput();
            HandleMouseInput();
        }

        private void HandleKeyboardInput()
        {
            string hInput, vInput;
            GetInputNames(out hInput, out vInput);

            int currentSel = GetCurrentSelection();
            bool wasBack = _backSelected;

            if (InputsManager.InputPositiveDown(vInput))
            {
                // Up
                if (_backSelected)
                {
                    _backSelected = false;
                    int lastRow = (_characters.Count - 1) / CardsPerRow;
                    int centerCol = Mathf.Min(1, _characters.Count - 1 - lastRow * CardsPerRow);
                    SetCurrentSelection(lastRow * CardsPerRow + centerCol);
                    PlaySound("select");
                }
                else if (currentSel >= CardsPerRow)
                {
                    SetCurrentSelection(currentSel - CardsPerRow);
                    PlaySound("select");
                }
            }
            else if (InputsManager.InputNegativeDown(vInput))
            {
                // Down
                if (!_backSelected)
                {
                    int row = currentSel / CardsPerRow;
                    int lastRow = (_characters.Count - 1) / CardsPerRow;
                    if (row >= lastRow)
                    {
                        _backSelected = true;
                        PlaySound("select");
                    }
                    else
                    {
                        SetCurrentSelection(Mathf.Min(currentSel + CardsPerRow, _characters.Count - 1));
                        PlaySound("select");
                    }
                }
            }
            else if (InputsManager.InputNegativeDown(hInput))
            {
                // Left
                if (!_backSelected && currentSel > 0)
                {
                    SetCurrentSelection(currentSel - 1);
                    PlaySound("select");
                }
            }
            else if (InputsManager.InputPositiveDown(hInput))
            {
                // Right
                if (!_backSelected && currentSel < _characters.Count - 1)
                {
                    SetCurrentSelection(currentSel + 1);
                    PlaySound("select");
                }
            }
            else if (InputsManager.InputDown("Submit"))
            {
                if (_backSelected)
                {
                    GoBackAction();
                }
                else
                {
                    ConfirmSelection();
                }
            }
            else if (InputsManager.InputDown("Cancel"))
            {
                GoBackAction();
            }

            if (wasBack != _backSelected || GetCurrentSelection() != currentSel)
            {
                UpdateAllCardVisuals();
                UpdateBackVisual();
                ScrollToSelection();
            }
        }

        private void HandleMouseInput()
        {
            Vector2 mousePos = InputsManager.InputMousePosition();

            // Check cards
            for (int i = 0; i < _cardRects.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_cardRects[i], mousePos, null))
                {
                    int curSel = GetCurrentSelection();
                    if (_backSelected || curSel != i)
                    {
                        _backSelected = false;
                        SetCurrentSelection(i);
                        UpdateAllCardVisuals();
                        UpdateBackVisual();
                        PlaySound("select");
                    }

                    if (InputsManager.InputMouseButtonUp(0))
                    {
                        ConfirmSelection();
                    }
                    return;
                }
            }

            // Check back text
            if (backText != null)
            {
                var backRect = backText.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(backRect, mousePos, null))
                {
                    if (!_backSelected)
                    {
                        _backSelected = true;
                        UpdateAllCardVisuals();
                        UpdateBackVisual();
                        PlaySound("select");
                    }

                    if (InputsManager.InputMouseButtonUp(0))
                    {
                        GoBackAction();
                    }
                    return;
                }
            }

            // Check create dog text
            if (createDogText != null)
            {
                var createRect = createDogText.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(createRect, mousePos, null))
                {
                    UpdateCreateDogVisual(true);
                    if (InputsManager.InputMouseButtonUp(0))
                    {
                        ChangeState(GameState.UploadAvatar);
                    }
                    return;
                }
            }

            UpdateCreateDogVisual(false);
        }

        private void GetInputNames(out string horizontal, out string vertical)
        {
            if (_gameMode == "2p" && _activePlayer == 1)
            {
                horizontal = "Player1_Horizontal";
                vertical = "Player1_Vertical";
            }
            else if (_gameMode == "2p" && _activePlayer == 2)
            {
                horizontal = "Player2_Horizontal";
                vertical = "Player2_Vertical";
            }
            else
            {
                horizontal = "Horizontal";
                vertical = "Vertical";
            }
        }

        private int GetCurrentSelection()
        {
            return _activePlayer == 2 ? _p2Selection : _p1Selection;
        }

        private void SetCurrentSelection(int index)
        {
            if (_activePlayer == 2)
                _p2Selection = index;
            else
                _p1Selection = index;
        }

        private void ConfirmSelection()
        {
            PlaySound("select");

            if (_gameMode == "2p" && !_p1Confirmed)
            {
                // P1 confirmed, switch to P2
                _p1Confirmed = true;
                _activePlayer = 2;
                _p2Selection = 0;
                _backSelected = false;
                UpdatePlayerIndicator();
                UpdateAllCardVisuals();
                UpdateBackVisual();
            }
            else
            {
                StartGame();
            }
        }

        private void GoBackAction()
        {
            PlaySound("select");

            if (_gameMode == "2p" && _p1Confirmed)
            {
                // Undo P1 confirm, go back to P1 selecting
                _p1Confirmed = false;
                _activePlayer = 1;
                _p2Selection = -1;
                _backSelected = false;
                UpdatePlayerIndicator();
                UpdateAllCardVisuals();
                UpdateBackVisual();
            }
            else
            {
                ChangeState(GameState.MainMenu);
            }
        }

        private void StartGame()
        {
            CharacterSO p2Char = null;

            if (_vsAi)
            {
                // AI gets random different character
                var available = new List<CharacterSO>(_characters);
                available.RemoveAt(_p1Selection);
                p2Char = available[Random.Range(0, available.Count)];
            }
            else if (_gameMode == "2p")
            {
                p2Char = _characters[_p2Selection];
            }

            var data = new Dictionary<string, object>
            {
                { "mode", _gameMode },
                { "vs_ai", _vsAi },
                { "p1_character", _characters[_p1Selection] },
                { "p2_character", p2Char },
                { "difficulty", "medium" }
            };
            ChangeState(GameState.Gameplay, data);
        }

        private void UpdatePlayerIndicator()
        {
            if (playerIndicator == null) return;

            if (_gameMode == "2p")
            {
                playerIndicator.gameObject.SetActive(true);
                if (_activePlayer == 1)
                {
                    playerIndicator.text = "Player 1 Select";
                    playerIndicator.color = P1Color;
                }
                else
                {
                    playerIndicator.text = "Player 2 Select";
                    playerIndicator.color = P2Color;
                }
            }
            else
            {
                playerIndicator.gameObject.SetActive(false);
            }
        }

        private void UpdateAllCardVisuals()
        {
            if (_cardOutlines == null) return;

            for (int i = 0; i < _cardOutlines.Count; i++)
            {
                Color borderColor;
                bool isP1 = i == _p1Selection;
                bool isP2 = _p2Selection >= 0 && i == _p2Selection;
                bool isCurrentHover = !_backSelected && i == GetCurrentSelection();
                bool selected = false;

                if (isP1 && _p1Confirmed && isP2)
                {
                    borderColor = BothColor;
                    selected = true;
                }
                else if (isP1 && _p1Confirmed)
                {
                    borderColor = P1Color;
                    selected = true;
                }
                else if (isCurrentHover)
                {
                    borderColor = _activePlayer == 2 ? P2Color : P1Color;
                    selected = true;
                }
                else
                {
                    borderColor = UnselectedBorder;
                }

                _cardOutlines[i].enabled = selected;
                if (selected)
                {
                    _cardOutlines[i].effectColor = borderColor;
                    _cardOutlines[i].effectDistance = new Vector2(4, -4);
                }
            }
        }

        private void UpdateBackVisual()
        {
            if (backText == null) return;
            backText.color = _backSelected ? BackHover : BackDefault;

            if (selectIndicator != null)
            {
                selectIndicator.enabled = _backSelected;
                if (_backSelected)
                {
                    var indicatorRect = selectIndicator.GetComponent<RectTransform>();
                    var backRect = backText.GetComponent<RectTransform>();
                    float halfWidth = backText.preferredWidth * 0.5f;
                    float indicatorHalf = indicatorRect.sizeDelta.x * 0.5f;
                    indicatorRect.anchoredPosition = new Vector2(
                        backRect.anchoredPosition.x - halfWidth - indicatorHalf - 6f,
                        backRect.anchoredPosition.y);
                }
            }
        }

        private void UpdateCreateDogVisual(bool hovered = false)
        {
            if (createDogText == null) return;
            createDogText.color = hovered ? CreateDogHover : CreateDogDefault;
        }

        private void ScrollToSelection()
        {
            if (_scrollRect == null || _backSelected) return;

            int sel = GetCurrentSelection();
            if (sel < 0 || sel >= _cardRects.Count) return;

            // Get the selected card's position relative to the content
            var cardRect = _cardRects[sel];
            float cardTop = -cardRect.anchoredPosition.y;
            float cardBottom = cardTop + CardHeight;

            float contentHeight = cardsContainer.rect.height;
            float viewHeight = ScrollZoneHeight;

            if (contentHeight <= viewHeight) return;

            float scrollableRange = contentHeight - viewHeight;

            // Current scroll position in content units (0 = top, scrollableRange = bottom)
            float currentScroll = (1f - _scrollRect.verticalNormalizedPosition) * scrollableRange;

            // Scroll so the card is fully visible
            if (cardTop < currentScroll)
            {
                float norm = 1f - (cardTop / scrollableRange);
                _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(norm);
            }
            else if (cardBottom > currentScroll + viewHeight)
            {
                float norm = 1f - ((cardBottom - viewHeight) / scrollableRange);
                _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(norm);
            }
        }
    }
}
