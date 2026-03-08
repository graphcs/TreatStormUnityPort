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

        // Intro
        IntroComplete,

        // Audio
        PlaySound,
        PlayMusic,
        StopMusic,

        // Voting / Crowd Chaos
        CrowdChaosStarted,
        VotingStarted,
        VotingEnded,

        // Twitch
        TwitchConnected,
        TwitchDisconnected,
        TwitchError
    }
}
