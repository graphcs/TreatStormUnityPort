namespace SnackAttack.Core
{
    public enum GameEvent
    {
        // Gameplay
        SnackSpawned,
        SnackCollected,
        SnackDespawned,

        // Effects
        PowerUpActivated,
        PowerUpExpired,
        PenaltyApplied,
        ChaosTriggered,
        ChaosEnded,

        // Score
        ScoreChanged,
        PointPopupRequested,

        // Flow
        GameStart,
        RoundStart,
        RoundEnd,
        LevelComplete,
        GameOver,
        GamePaused,
        GameResumed,

        // Player
        PlayerCollision,
        PlayerMoved,

        // UI
        ScreenTransition,
        SettingsChanged,

        // Audio
        PlaySound,
        PlayMusic,
        StopMusic
    }
}
