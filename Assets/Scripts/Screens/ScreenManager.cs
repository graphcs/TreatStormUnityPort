using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Screens
{
    /// <summary>
    /// Manages screen visibility based on GameState transitions.
    /// Lives on the root UI Canvas. Discovers all BaseScreen children on Awake
    /// and auto-registers them with the state machine.
    ///
    /// Mirrors PyGame's screen management in Game._create_screens() and the
    /// StateMachine transition flow.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        [Header("Auto-start")]
        [SerializeField] private GameState initialState = GameState.MainMenu;
        [SerializeField] private float startDelay = 0.1f;

        private readonly List<BaseScreen> _screens = new();

        private void Awake()
        {
            // Discover all BaseScreen components in children
            GetComponentsInChildren(true, _screens);
        }

        private void Start()
        {
            // Screens self-register in their Start() via BaseScreen.Start()
            // Transition to the initial state after a brief delay to ensure
            // all registrations complete
            Invoke(nameof(GoToInitialState), startDelay);
        }

        private void GoToInitialState()
        {
            GameManager.Instance.StateMachine.ChangeState(initialState);
        }
    }
}
