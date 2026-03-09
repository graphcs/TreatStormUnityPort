# Snack Attack

> A 2D competitive local-multiplayer party game where dogs battle for snacks in chaotic, crowd-powered arenas.

---

## Features

- 6 playable characters, each with unique speed, size, and gameplay stats
- 3 game modes: Single Dog (Treat Attack), 1P vs AI, 2P local co-op/versus
- 7 snack types with power-up and penalty effects
- 3 arena levels with escalating spawn difficulty
- AI opponents at 3 configurable difficulty tiers
- Audience voting & Crowd Chaos system with live ChatSimulator
- AI-generated custom dog avatars via OpenRouter (multi-step image pipeline)
- Twitch chat integration — route live !votes into the VotingSystem
- Cinematic intros: storm sequence, round start, and menu flythrough
- Full audio system with music crossfade and pooled SFX
- Canvas-based rendering — no world-space physics, no Rigidbody2D

---

## Project Structure

```
Assets/
├── Art/
│   ├── Characters/
│   │   ├── Portraits/          # Character portrait sprites
│   │   └── SpriteSheets/       # Run/eat/walk sprite sheets
│   ├── Food/                   # Snack sprites
│   ├── UI/                     # Background, logo, button, panel assets
│   └── Wings/                  # Wing sprites for boost VFX
├── Audio/
│   ├── Music/
│   └── SFX/
├── Resources/                  # Runtime-loaded assets (Daydream SDF font, etc.)
├── ScriptableObjects/
│   └── Config/                 # 33 .asset config files
├── Scripts/
│   ├── Avatar/                 # OpenRouterClient, AvatarGenerator, AvatarPersistence
│   ├── Core/                   # SOs, enums, state machine, event bus
│   ├── Editor/                 # Setup tools (SOPopulator, SetupXxx scripts)
│   ├── Effects/                # VFX controllers, UI drawing primitives
│   ├── Entities/               # PlayerController, CharacterAnimator, FallingSnack, etc.
│   ├── Gameplay/               # RoundManager, Arena, SnackCollector
│   ├── Interaction/            # VotingSystem, CrowdChaosOverlay, ChatSimulator
│   ├── Screens/                # BaseScreen, ScreenManager, all 11 screen scripts
│   └── Twitch/                 # TwitchChatManager, TwitchConfigSO, TwitchSetupScreen
├── Scenes/
│   └── MainScene.unity
├── StreamingAssets/
│   └── AvatarRefs/             # Reference PNGs used as style guides for avatar gen
└── TextMesh Pro/
```

### Code Namespaces

| Namespace | Contents |
|---|---|
| `SnackAttack.Core` | Config ScriptableObjects, enums, structs, EventBus, GameStateMachine |
| `SnackAttack.Entities` | PlayerController, CharacterAnimator, AIController, LeashRenderer, FallingSnack, FallingTreat |
| `SnackAttack.Gameplay` | RoundManager, Arena, SnackCollector, TreatAttackManager, TreatAttackSpawner, TreatAttackCollector |
| `SnackAttack.Screens` | BaseScreen, ScreenManager, MainMenuScreen, CharacterSelectScreen, GameOverScreen, SettingsScreen, UploadAvatarScreen, AvatarShowcaseScreen |
| `SnackAttack.Effects` | PlayerVFXController, PickupFlashEffect, StatusIndicatorEffect, AuraEffect, WingsEffect, SpeedStreakEffect, SteamParticleEffect, SnackGlowEffect, UILineDrawer, UICircleDrawer, UIParticlePool |
| `SnackAttack.Audio` | AudioManager |
| `SnackAttack.Interaction` | VotingSystem, CrowdChaosOverlay, VotingMeter, ChatSimulator |
| `SnackAttack.Avatar` | AvatarGenerator, OpenRouterClient, AvatarPersistence |
| `SnackAttack.Twitch` | TwitchChatManager, TwitchConfigSO |
| `SnackAttack.Editor` | All Editor-only setup and population tools |

---

## Setup / How to Run

### Requirements

- **Unity 6000.3.10f1** (Unity 6.3)
- Packages (installed via Package Manager):
  - **Universal Render Pipeline (URP)**
  - **TextMesh Pro**
  - **BxB Inputs Manager** (wraps the New Input System; required for all player input)

### Steps

1. Clone or download the repository.
2. Open the project folder in Unity Hub → **Open Project**.
3. Once the Editor loads, open `Assets/Scenes/MainScene.unity`.
4. Press **Play**.

### Runtime PlayerPrefs Keys

| Key | Purpose |
|---|---|
| `sa_openRouterKey` | OpenRouter API key for avatar generation |
| `sa_musicVolume` | Music volume (0–1) |
| `sa_sfxVolume` | SFX volume (0–1) |
| `sa_masterVolume` | Master volume (0–1) |
| `sa_musicEnabled` | Music on/off toggle |
| `sa_sfxEnabled` | SFX on/off toggle |

---

## Architecture Overview

**Single-scene architecture** — `MainScene.unity` is the only scene. All game states, screens, and modes live within it and are activated/deactivated at runtime.

**Canvas layers** separate rendering concerns:

| Canvas | Sort Order | Contents |
|---|---|---|
| BackgroundCanvas | 0 | Gameplay background, clouds, battlefield images |
| GameplayCanvas | 50 | All entities (players, snacks, leashes, VFX) |
| UICanvas | 100 | All HUD overlays and menu screens |

**Event-driven communication** — `EventBus` (static singleton) carries 30+ typed `GameEvent` values. Systems subscribe/unsubscribe in `OnEnable`/`OnDisable`. No direct component references needed between unrelated systems.

**State machine** — `GameStateMachine` drives 9 `GameState` values (`MainMenu`, `CharacterSelect`, `Gameplay`, `GameOver`, `Settings`, `TreatAttack`, `UploadAvatar`, `AvatarShowcase`, `TwitchSetup`). Each `BaseScreen` registers itself for its state and auto-shows/hides via `CanvasGroup`.

**ScriptableObject-driven configuration** — 33 config assets under `Assets/ScriptableObjects/Config/`. No values are hardcoded in scripts. Tuning is done entirely in the Inspector.

**Manual collision detection** — `SnackCollector` and `TreatAttackCollector` use `Rect.Overlaps()` per frame. There are no `Rigidbody2D`, `BoxCollider2D`, or `CircleCollider2D` components anywhere.

**Code-driven animation** — `CharacterAnimator` swaps sprite frames on timers (10 fps run, ~8 fps eat). Unity's Animator component is not used.

---

## Systems Summary

### Core
Config ScriptableObjects cover every tunable value: character stats (`CharacterSO`), snack definitions (`SnackSO`), level configs (`LevelSO`), gameplay rules (`GameSettingsSO`), visual palettes (`UIColorsSO`), layout constants (`UILayoutSO`), audio (`AudioSettingsSO`), intros (`IntroSettingsSO`), voting (`VotingSettingsSO`), and power-up visuals (`PowerUpVisualsSO`). The `EventBus` provides a zero-dependency publish/subscribe layer. `GameStateMachine` transitions between the 9 game states and notifies registered `IScreen` implementors.

### Entities
`PlayerController` handles velocity-based movement, a leash constraint system, free-flight during Boost, and a stacking power-up effect list. `CharacterAnimator` drives sprite-swap animation and applies tint colours for active effects. `AIController` sits alongside `PlayerController` and issues `SetMoveInput()` calls based on timer-driven decisions — snack scoring (points, distance, penalty avoidance) plus noise-perturbed pathfinding. `LeashRenderer` draws the leash rope as a parabolic arc using `UILineDrawer`. `FallingSnack` and `FallingTreat` are lightweight falling-item entities that move downward each frame and report collisions to their respective collectors.

### Gameplay
`RoundManager` orchestrates the full match lifecycle: intro → rounds → game-over. It creates player GameObjects dynamically, assigns characters and inputs, manages the `RoundPhase` state machine, runs voting between rounds, and tallies wins. `Arena` manages snack spawning (weighted random pool, lightning flash pre-spawn, configurable max-on-screen). `SnackCollector` detects overlaps, applies effects via `PlayerController.ApplyEffect()`, awards points (with a 1.5× stolen-snack bonus), fires `PointPopupRequested` events, and triggers eat animation.

### Screens
All menu screens extend `BaseScreen` (abstract `MonoBehaviour`). `ScreenManager` discovers all `BaseScreen` children on `UICanvas` at startup and triggers the initial state. The 11 screens are: `MainMenuScreen`, `CharacterSelectScreen`, `GameplayHUD` (plain MonoBehaviour overlay), `GameOverScreen`, `SettingsScreen`, `UploadAvatarScreen`, `AvatarShowcaseScreen`, and stubs for `TreatAttackScreen`, `TwitchSetupScreen`. Navigation is keyboard-first (arrow keys + Enter) with full mouse support.

### Effects
`PlayerVFXController` attaches `VFX_Behind` and `VFX_Front` containers to each player and coordinates eight sub-effects: `PickupFlashEffect` (expanding ring on collection), `StatusIndicatorEffect` (timer bars + icons above the dog), `AuraEffect` (pulsing rings + orbiting sparkles), `WingsEffect` (wing flap + trail during Boost), `SpeedStreakEffect` (afterimages + streaks during SpeedBoost), `SteamParticleEffect` (steam during Chaos), and `SnackGlowEffect` (glow on power-up snacks). `UIParticlePool` provides a pooled, reusable canvas-based particle system. `EasingUtils` supplies easing curves for cinematic animation.

### Audio
`AudioManager` on the `GameManager` GameObject holds 12 `AudioClip` references, one looping music `AudioSource`, and a pool of 6 SFX sources. It subscribes to `PlaySound`, `PlayMusic`, `StopMusic`, `SettingsChanged`, `SnackCollected`, `GamePaused`, and `GameResumed` events. Music transitions use a crossfade coroutine. Volume and enabled state are read from `AudioSettingsSO` and overridden by PlayerPrefs at runtime.

### Interaction
`VotingSystem` (pure C# class) manages a single vote-per-voter round with three modes: `TreatDrop`, `Action` (extend/yank leash), and `Trivia` (speed boost reward). `CrowdChaosOverlay` displays a 5-second countdown with a red tint pulse. `VotingMeter` renders live vote tallies as dynamic bars. `ChatSimulator` drives a scrollable mock chat window with bot auto-voters; it can be replaced at runtime by `TwitchChatManager` as the vote source.

### Avatar
`AvatarGenerator` runs a seven-step coroutine pipeline via `OpenRouterClient` (a `UnityWebRequest`-based HTTP client with exponential back-off on 429/5xx responses): describe the dog's appearance → generate a profile portrait → run sprite sheet → eat sprite sheet → walk sprite sheet → boost sprite → register with `AvatarPersistence`. All images are produced through the OpenRouter vision-capable model. `AvatarPersistence` saves PNGs and a `manifest.json` to `Application.persistentDataPath/custom_avatars/{id}/` and reloads them into `CharacterDatabaseSO` on startup.

### UI Primitives
`UILineDrawer` and `UICircleDrawer` are custom `MaskableGraphic` subclasses that write geometry directly into Unity's UI mesh builder. `UILineDrawer` renders polylines with configurable width, caps, and per-segment colours — used for leash ropes, lightning bolts, and rain. `UICircleDrawer` renders filled circles, rings, and radial beam bursts — used for auras, pickup flashes, and cinematic blooms. Both integrate fully with the Canvas render pipeline and respect masking.

---

## Game Modes

| Mode | Players | Arena | Rounds | Voting |
|---|---|---|---|---|
| **Single Dog** (Treat Attack) | 1 (keyboard or AI) | 720×720 single-player arena | Infinite / timed | Action mode, 10 s window, cycling extend/yank leash |
| **1P vs AI** | 1 human + 1 AI | Full 1200×860 dual arena | Best of `roundsPerGame` | Treat / Action / Trivia rotating per round |
| **2 Players** | 2 humans | Full 1200×860 dual arena | Best of `roundsPerGame` | Treat / Action / Trivia rotating per round |

---

## Characters & Snacks

### Characters

| Name | Speed Multiplier | Gameplay Size |
|---|---|---|
| Biggie | 0.90× | default |
| Prissy | 1.10× | 173 px |
| Dash | 1.30× | default |
| Snowy | 1.00× | default |
| Rex | 1.05× | default |
| Jazzy | 1.15× | default |

### Snacks

| Snack | Points | Effect | Duration |
|---|---|---|---|
| Pizza | 100 | — | — |
| Bone | 25 | SpeedBoost 1.5× | 5 s |
| Broccoli | −50 | Slow 0.5× | 3 s |
| Spicy Pepper | 200 | Chaos (controls flipped) | 4 s |
| Bacon | 150 | — | — |
| Steak | 250 | Invincibility | 2 s |
| Red Bull | 50 | Boost (free flight) 2× | 6 s |

---

## Avatar Generation

Custom playable dogs are generated entirely through the **OpenRouter** API.

### Requirements
- An OpenRouter API key must be set in `Settings → Avatar API Key` (stored in PlayerPrefs key `sa_openRouterKey`).

### Pipeline (7 steps)
1. **Describe** — send dog name + breed to the LLM; receive a detailed visual description.
2. **Profile portrait** — generate a square portrait PNG from the description.
3. **Run sprite sheet** — generate a 4-frame horizontal run sheet.
4. **Eat sprite sheet** — generate a 3-frame eat/bite sheet.
5. **Walk sprite sheet** — generate a 4-frame walk sheet.
6. **Boost sprite** — generate a single "wings out" boost pose.
7. **Register** — slice sheets via `Sprite.Create`, build a `CharacterSO`, add to `CharacterDatabaseSO`.

All prompts use reference images from `Assets/StreamingAssets/AvatarRefs/` as style guides.

### Persistence
Generated assets are saved to:
```
Application.persistentDataPath/
  custom_avatars/
    {id}/
      profile.png
      run.png
      eat.png
      walk.png
      boost.png
      manifest.json
```

`AvatarPersistence.LoadAll()` is called in `GameManager.Start()` to restore custom characters across sessions.

### Entry Point
`CharacterSelectScreen` → right-click on a card slot → `UploadAvatarScreen` → `AvatarGenerator.GenerateAvatar()` coroutine.

---

## Twitch Integration

**Status: partially stubbed.**

`TwitchChatManager.cs` and `TwitchConfigSO` are present and wired to the screen flow, but the live IRC connection is not yet fully implemented.

### Intended behaviour
When a Twitch channel is connected, `TwitchChatManager` replaces `ChatSimulator` as the vote source. Incoming `!vote1` / `!vote2` (or configurable command strings) are parsed and forwarded to `VotingSystem.Vote(viewerId, option)`.

### Configuration
- Enter channel name, bot username, and OAuth token via **Settings → Twitch Setup** (`TwitchSetupScreen`).
- Values are stored in `TwitchConfigSO` and/or PlayerPrefs.

---

## Input System

Input is handled via the **BxB Inputs Manager** package, which wraps Unity's New Input System under a string-keyed API (`InputsManager.InputValue("name")`).

| Action | Player 1 | Player 2 |
|---|---|---|
| Move Horizontal | A / D | ← / → |
| Move Vertical *(Boost only)* | W / S | ↑ / ↓ |
| UI Confirm | Enter | — |
| UI Back / Pause | Escape | — |
| Quit to Menu *(while paused)* | Q | — |

Vertical movement is only active while the **Boost** (Red Bull) effect is in effect. All other movement is horizontal-only.

---

## License

© graphcs. All rights reserved.
