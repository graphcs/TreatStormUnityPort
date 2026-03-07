using System.Collections.Generic;
using UnityEngine;

namespace SnackAttack.Core
{
    /// <summary>
    /// Manages game state transitions and screen lifecycle.
    /// Mirrors PyGame StateMachine: register_state, change_state, go_back.
    /// </summary>
    public class GameStateMachine : MonoBehaviour
    {
        private readonly Dictionary<GameState, IScreen> _screens = new();
        private GameState? _currentState;
        private GameState? _previousState;
        private Dictionary<string, object> _transitionData = new();

        public GameState? CurrentState => _currentState;
        public GameState? PreviousState => _previousState;

        public void RegisterScreen(GameState state, IScreen screen)
        {
            _screens[state] = screen;
        }

        public void ChangeState(GameState newState, Dictionary<string, object> data = null)
        {
            // Exit current state
            if (_currentState.HasValue && _screens.TryGetValue(_currentState.Value, out var currentScreen))
            {
                currentScreen.OnExit();
            }

            // Store transition data
            _transitionData = data ?? new Dictionary<string, object>();

            // Update state tracking
            _previousState = _currentState;
            _currentState = newState;

            // Emit ScreenTransition event
            EventBus.Emit(GameEvent.ScreenTransition, new Dictionary<string, object>
            {
                { "from", _previousState },
                { "to", newState },
                { "data", _transitionData }
            }, "StateMachine");

            // Enter new state
            if (_screens.TryGetValue(newState, out var newScreen))
            {
                newScreen.OnEnter(_transitionData);
            }
        }

        public IScreen GetCurrentScreen()
        {
            if (_currentState.HasValue && _screens.TryGetValue(_currentState.Value, out var screen))
                return screen;
            return null;
        }

        public void GoBack()
        {
            if (_previousState.HasValue)
                ChangeState(_previousState.Value);
        }

        public Dictionary<string, object> GetTransitionData()
        {
            return _transitionData;
        }
    }

    /// <summary>
    /// Interface for screens managed by the state machine.
    /// Mirrors PyGame BaseScreen: on_enter, on_exit, update, handle_event.
    /// </summary>
    public interface IScreen
    {
        void OnEnter(Dictionary<string, object> data);
        void OnExit();
    }
}
