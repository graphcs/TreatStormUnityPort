using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using Utilities.Inputs;

namespace SnackAttack.Screens
{
    public class MainMenuScreen : BaseScreen
    {
        [Header("Main Menu References")]
        [SerializeField] private Image background;
        [SerializeField] private Image logo;
        [SerializeField] private Image menuContainer;
        [SerializeField] private Image selectIndicator;
        [SerializeField] private Image[] buttonImages;
        [SerializeField] private TMP_Text footerText;

        private int _selectedIndex;
        private bool _active;
        private RectTransform _indicatorRect;
        private RectTransform[] _buttonRects;

        private struct MenuItem
        {
            public GameState? targetState;
            public Dictionary<string, object> data;
            public bool isQuit;
        }

        private readonly MenuItem[] _menuItems = new MenuItem[]
        {
            new MenuItem
            {
                targetState = GameState.CharacterSelect,
                data = new Dictionary<string, object> { { "mode", "single_dog" }, { "vs_ai", false } }
            },
            new MenuItem
            {
                targetState = GameState.CharacterSelect,
                data = new Dictionary<string, object> { { "mode", "1p" }, { "vs_ai", true } }
            },
            new MenuItem
            {
                targetState = GameState.CharacterSelect,
                data = new Dictionary<string, object> { { "mode", "2p" }, { "vs_ai", false } }
            },
            new MenuItem
            {
                targetState = GameState.Settings,
                data = null
            },
            new MenuItem
            {
                targetState = null,
                data = null,
                isQuit = true
            }
        };

        protected override void Awake()
        {
            base.Awake();
            if (selectIndicator != null)
                _indicatorRect = selectIndicator.GetComponent<RectTransform>();
        }

        protected override void Start()
        {
            base.Start();
            CacheButtonRects();
        }

        private void CacheButtonRects()
        {
            if (buttonImages == null) return;
            _buttonRects = new RectTransform[buttonImages.Length];
            for (int i = 0; i < buttonImages.Length; i++)
            {
                if (buttonImages[i] != null)
                    _buttonRects[i] = buttonImages[i].GetComponent<RectTransform>();
            }
        }

        public override void OnEnter(Dictionary<string, object> data)
        {
            base.OnEnter(data);
            _selectedIndex = 0;
            _active = true;
            UpdateIndicator();
            PlayMusic("background");
        }

        public override void OnExit()
        {
            _active = false;
            if (selectIndicator != null)
                selectIndicator.enabled = false;
            base.OnExit();
        }

        private void Update()
        {
            if (!_active) return;

            HandleKeyboardInput();
            HandleMouseInput();
        }

        private void HandleKeyboardInput()
        {
            if (!InputsManager.Started) return;

            if (InputsManager.InputPositiveDown("Vertical"))
            {
                ChangeSelection(-1);
            }
            else if (InputsManager.InputNegativeDown("Vertical"))
            {
                ChangeSelection(1);
            }
            else if (InputsManager.InputDown("Submit"))
            {
                ActivateSelected();
            }
            else if (InputsManager.InputDown("Cancel"))
            {
                QuitGame();
            }
        }

        private void HandleMouseInput()
        {
            if (_buttonRects == null || !InputsManager.Started) return;

            for (int i = 0; i < _buttonRects.Length; i++)
            {
                if (_buttonRects[i] == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(
                    _buttonRects[i], InputsManager.InputMousePosition(), null))
                {
                    if (i != _selectedIndex)
                    {
                        _selectedIndex = i;
                        UpdateIndicator();
                        PlaySound("select");
                    }

                    if (InputsManager.InputMouseButtonUp(0))
                    {
                        ActivateSelected();
                    }
                    break;
                }
            }
        }

        private void ChangeSelection(int direction)
        {
            int count = _menuItems.Length;
            _selectedIndex = ((_selectedIndex + direction) % count + count) % count;
            UpdateIndicator();
            PlaySound("select");
        }

        private void ActivateSelected()
        {
            PlaySound("select");

            var item = _menuItems[_selectedIndex];
            if (item.isQuit)
            {
                QuitGame();
                return;
            }

            if (item.targetState.HasValue)
            {
                ChangeState(item.targetState.Value, item.data);
            }
        }

        private void UpdateIndicator()
        {
            if (_indicatorRect == null || _buttonRects == null) return;
            if (_selectedIndex < 0 || _selectedIndex >= _buttonRects.Length) return;

            selectIndicator.enabled = true;

            var buttonRect = _buttonRects[_selectedIndex];
            if (buttonRect == null) return;

            float buttonX = buttonRect.anchoredPosition.x;
            float buttonHalfWidth = buttonRect.sizeDelta.x * 0.5f;
            float indicatorHalfWidth = _indicatorRect.sizeDelta.x * 0.5f;

            float indicatorX = buttonX - buttonHalfWidth - indicatorHalfWidth - 6f;
            float indicatorY = buttonRect.anchoredPosition.y;

            _indicatorRect.anchoredPosition = new Vector2(indicatorX, indicatorY);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
