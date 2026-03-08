using System.Collections.Generic;
using UnityEngine;
using Utilities.Inputs;

namespace SnackAttack.Core
{
    /// <summary>
    /// Singleton root orchestrator. Mirrors PyGame Game class:
    /// owns config references, state machine, processes event queue each frame.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Config References")]
        [SerializeField] private GameSettingsSO gameSettings;
        [SerializeField] private CharacterDatabaseSO characterDatabase;
        [SerializeField] private SnackDatabaseSO snackDatabase;
        [SerializeField] private LevelDatabaseSO levelDatabase;
        [SerializeField] private AIDifficultySO aiDifficulty;
        [SerializeField] private AudioSettingsSO audioSettings;
        [SerializeField] private UIColorsSO uiColors;
        [SerializeField] private CelebrationSettingsSO celebrationSettings;
        [SerializeField] private UILayoutSO uiLayout;
        [SerializeField] private ControlsSO controls;
        [SerializeField] private PowerUpVisualsSO powerUpVisuals;

        public GameSettingsSO GameSettings => gameSettings;
        public CharacterDatabaseSO CharacterDatabase => characterDatabase;
        public SnackDatabaseSO SnackDatabase => snackDatabase;
        public LevelDatabaseSO LevelDatabase => levelDatabase;
        public AIDifficultySO AIDifficulty => aiDifficulty;
        public AudioSettingsSO AudioSettings => audioSettings;
        public UIColorsSO UIColors => uiColors;
        public CelebrationSettingsSO CelebrationSettings => celebrationSettings;
        public UILayoutSO UILayout => uiLayout;
        public ControlsSO Controls => controls;
        public PowerUpVisualsSO PowerUpVisuals => powerUpVisuals;

        public GameStateMachine StateMachine { get; private set; }

        // Session data shared across screens (mirrors PyGame Game's shared state)
        public Dictionary<string, object> SessionData { get; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            StateMachine = GetComponent<GameStateMachine>();
            if (StateMachine == null)
                StateMachine = gameObject.AddComponent<GameStateMachine>();

            Application.targetFrameRate = gameSettings != null ? gameSettings.targetFPS : 60;
        }

        protected void Start()
        {
            if (!InputsManager.Started)
                InputsManager.Start();
        }

        private void Update()
        {
            EventBus.ProcessQueue();

            if (InputsManager.Started)
                InputsManager.Update();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                EventBus.Clear();
                Instance = null;
            }
        }
    }
}
