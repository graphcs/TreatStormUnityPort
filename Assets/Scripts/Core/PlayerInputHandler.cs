using UnityEngine;
using Utilities.Inputs;
using SnackAttack.Entities;

namespace SnackAttack.Core
{
    /// <summary>
    /// Reads input via BxB Inputs Manager and drives PlayerController.SetMoveInput().
    /// Mirrors PyGame gameplay.py key handling (WASD for P1, Arrows for P2).
    /// In single-player mode, uses the shared Horizontal/Vertical inputs
    /// so both WASD and Arrows control P1.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private int playerIndex; // 0 = P1, 1 = P2

        [Header("Mode")]
        [SerializeField] private bool singlePlayerMode;

        // BxB input names (configured via Tools > Utilities > Inputs Manager)
        private string _horizontalInput;
        private string _verticalInput;

        private void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            SetupInputNames();
        }

        /// <summary>
        /// Configure which player this handler controls and whether it's single-player mode.
        /// </summary>
        public void Configure(int index, bool isSinglePlayer)
        {
            playerIndex = index;
            singlePlayerMode = isSinglePlayer;
            SetupInputNames();
        }

        private void SetupInputNames()
        {
            if (singlePlayerMode)
            {
                // Single-player: use shared Horizontal/Vertical (reads both WASD and Arrows)
                _horizontalInput = "Horizontal";
                _verticalInput = "Vertical";
            }
            else if (playerIndex == 0)
            {
                // P1 in multiplayer: WASD only
                _horizontalInput = "Player1_Horizontal";
                _verticalInput = "Player1_Vertical";
            }
            else
            {
                // P2: Arrow keys only
                _horizontalInput = "Player2_Horizontal";
                _verticalInput = "Player2_Vertical";
            }
        }

        private void Update()
        {
            if (playerController == null) return;

            float dx = InputsManager.InputValue(_horizontalInput);
            float dy = InputsManager.InputValue(_verticalInput);

            playerController.SetMoveInput(new Vector2(dx, dy));
        }
    }
}
