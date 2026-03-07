using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Screens
{
    /// <summary>
    /// Abstract base class for all UI screens. Each screen is a Canvas panel
    /// toggled by the ScreenManager via the state machine.
    /// Mirrors PyGame BaseScreen: on_enter, on_exit, update, handle_event.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseScreen : MonoBehaviour, IScreen
    {
        [Header("Screen Config")]
        [SerializeField] private GameState screenState;

        private CanvasGroup _canvasGroup;

        /// <summary>The GameState this screen is registered for.</summary>
        public GameState ScreenState => screenState;

        protected GameManager GM => GameManager.Instance;
        protected GameStateMachine StateMachine => GM.StateMachine;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            // Hide visually but keep GO active so Start() can register with StateMachine
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        protected virtual void Start()
        {
            StateMachine.RegisterScreen(screenState, this);
        }

        // --- IScreen ---

        public virtual void OnEnter(Dictionary<string, object> data)
        {
            Show();
        }

        public virtual void OnExit()
        {
            Hide();
        }

        // --- Show / Hide ---

        public void Show()
        {
            gameObject.SetActive(true);
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        // --- Helpers ---

        /// <summary>
        /// Transition to another state, forwarding data.
        /// </summary>
        protected void ChangeState(GameState state, Dictionary<string, object> data = null)
        {
            StateMachine.ChangeState(state, data);
        }

        /// <summary>
        /// Go back to the previous state.
        /// </summary>
        protected void GoBack()
        {
            StateMachine.GoBack();
        }

        /// <summary>
        /// Emit a sound event.
        /// </summary>
        protected void PlaySound(string soundName)
        {
            EventBus.Emit(GameEvent.PlaySound, new Dictionary<string, object>
            {
                { "sound", soundName }
            });
        }

        /// <summary>
        /// Emit a music event.
        /// </summary>
        protected void PlayMusic(string trackName)
        {
            EventBus.Emit(GameEvent.PlayMusic, new Dictionary<string, object>
            {
                { "track", trackName }
            });
        }
    }
}
