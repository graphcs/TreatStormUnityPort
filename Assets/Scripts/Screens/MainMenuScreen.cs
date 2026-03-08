using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SnackAttack.Core;
using SnackAttack.Effects;
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

        // Intro
        private MainMenuIntro _intro;
        private bool _introRunning;
        private static bool _introPlayed;

        // Cached settings
        private UILayoutSO _layout;
        private ControlsSO _controls;

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

            _layout = GM.UILayout;
            _controls = GM.Controls;

            _selectedIndex = 0;
            UpdateIndicator();

            // TODO: Re-enable menu intro when ready
            // var introSettings = GM.IntroSettings;
            // if (!_introPlayed && introSettings != null)
            // {
            //     // Hide menu items during intro
            //     SetMenuItemsVisible(false);
            //     _introRunning = true;
            //     _active = false;
            //
            //     // Create intro
            //     var panelRect = GetComponent<RectTransform>();
            //     var introGo = new GameObject("MainMenuIntro");
            //     var introRect = introGo.AddComponent<RectTransform>();
            //     introRect.SetParent(panelRect, false);
            //     introRect.anchorMin = Vector2.zero;
            //     introRect.anchorMax = Vector2.one;
            //     introRect.sizeDelta = Vector2.zero;
            //     introRect.anchoredPosition = Vector2.zero;
            //
            //     // Get logo rect for positioning
            //     Rect logoR = new Rect(180f, 80f, 840f, 350f);
            //     if (logo != null)
            //     {
            //         var lr = logo.rectTransform;
            //         logoR = new Rect(
            //             lr.anchoredPosition.x - lr.sizeDelta.x * lr.pivot.x + 600f,
            //             -(lr.anchoredPosition.y + lr.sizeDelta.y * (1f - lr.pivot.y)),
            //             lr.sizeDelta.x,
            //             lr.sizeDelta.y
            //         );
            //     }
            //
            //     _intro = introGo.AddComponent<MainMenuIntro>();
            //     _intro.Initialize(introRect, introSettings, logoR);
            //     _intro.StartIntro();
            // }
            // else
            // {
            //     _active = true;
            //     _introRunning = false;
            // }
            _active = true;
            _introRunning = false;

            string bgMusic = _controls != null ? _controls.backgroundMusic : "background";
            PlayMusic(bgMusic);
        }

        public override void OnExit()
        {
            _active = false;
            _introRunning = false;
            if (_intro != null)
            {
                Destroy(_intro.gameObject);
                _intro = null;
            }
            if (selectIndicator != null)
                selectIndicator.enabled = false;
            base.OnExit();
        }

        private void Update()
        {
            if (_introRunning && _intro != null && _intro.IsComplete)
            {
                _introRunning = false;
                _introPlayed = true;
                _active = true;
                SetMenuItemsVisible(true);

                if (_intro != null)
                {
                    Destroy(_intro.gameObject);
                    _intro = null;
                }
            }

            if (!_active) return;

            HandleKeyboardInput();
            HandleMouseInput();
        }

        private void HandleKeyboardInput()
        {
            if (!InputsManager.Started) return;

            string vAxis = _controls != null ? _controls.verticalAxis : "Vertical";
            string submitAction = _controls != null ? _controls.submitAction : "Submit";
            string cancelAction = _controls != null ? _controls.cancelAction : "Cancel";

            if (InputsManager.InputPositiveHold(vAxis) || InputsManager.InputPositiveDown(vAxis))
            {
                ChangeSelection(-1);
            }
            else if (InputsManager.InputNegativeHold(vAxis) || InputsManager.InputNegativeDown(vAxis))
            {
                ChangeSelection(1);
            }
            else if (InputsManager.InputDown(submitAction))
            {
                ActivateSelected();
            }
            else if (InputsManager.InputDown(cancelAction))
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
        }

        private void ChangeSelection(int direction)
        {
            int count = _menuItems.Length;
            _selectedIndex = ((_selectedIndex + direction) % count + count) % count;
            UpdateIndicator();
            string selectSnd = _controls != null ? _controls.selectSound : "select";
            PlaySound(selectSnd);
        }

        private void ActivateSelected()
        {
            string selectSnd = _controls != null ? _controls.selectSound : "select";
            PlaySound(selectSnd);

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

            float offset = _layout != null ? _layout.selectorOffset : 6f;
            float indicatorX = buttonX - buttonHalfWidth - indicatorHalfWidth - offset;
            float indicatorY = buttonRect.anchoredPosition.y;

            _indicatorRect.anchoredPosition = new Vector2(indicatorX, indicatorY);
        }

        private void SetMenuItemsVisible(bool visible)
        {
            if (menuContainer != null)
                menuContainer.enabled = visible;
            if (buttonImages != null)
            {
                foreach (var btn in buttonImages)
                {
                    if (btn != null)
                        btn.enabled = visible;
                }
            }
            if (selectIndicator != null)
                selectIndicator.enabled = visible;
            if (footerText != null)
                footerText.enabled = visible;
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
