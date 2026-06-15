# KittyAdventures

A third-person 3D action-adventure game built in Unity. You play as **Kitty**, exploring a
hand-built world, solving environmental puzzles, fighting enemies, defeating a boss, and finding
your way back home.

> This README documents the gameplay scripts under `Assets/ScriptsNew/`, which contain all of the
> game's custom logic. Third-party packages (TextMesh Pro, EZSoftBone, Cartoon FX, etc.) live
> elsewhere under `Assets/` and are not covered here.

---

## Gameplay

Kitty moves through a connected world gated by **obstructions** (trees, mushrooms, potions) that
only open once you complete the matching objective. Progress is driven by four objective types:

- **Orb loop** — activate a sequence of glowing checkpoints in the correct order to open a path.
- **Spider cull** — defeat all the spiders in an area to slide a barrier of trees apart.
- **Number puzzle** — count the mushrooms that spawned and enter the right answer; the blocking trees turn into mushrooms.
- **The Spirit boss** — defeat the Spirit, which drops a potion that completes the run.

Reach your house at the end to trigger the Victory sequence.

### Player abilities

- **Movement** — walk / sprint, jump, dodge-roll, and climb (all gated by a stamina meter).
- **Stomp** — land on an enemy from above to deal damage.
- **Combat** — light attack (chains into a 3-hit combo), heavy attack (knockback + wide arc), and a **parry** with a timing window that stuns enemies.
- **Snacks** — collectible consumables that heal Kitty (eat with **F** by default).

### Controls

Input is handled through Unity's **Input System** (PlayerInput in *Invoke Unity Events* mode), so
exact key/controller bindings live in the Input Actions asset. The actions exposed to gameplay are:
Move, Look, Zoom, Jump, Dodge, Climb, Sprint, Light Attack, Heavy Attack, Parry, and Interact.

---

## Core gameplay loop

1. Explore with a **true orbit camera** that always frames Kitty.
2. Approach interactable objects (orb gates, prompts) — an on-screen prompt appears; press Interact.
3. Complete an objective → its event fires → the matching obstruction animates open.
4. Fight spiders and survive the Spirit boss; collect snacks to stay alive.
5. Collect the Spirit's potion and return home → **Victory → End**.

---

## Project structure

```
Assets/ScriptsNew/
├── Camera/            Orbit camera
│   └── CameraController.cs
├── Player/            Kitty's movement, combat, stats, input
│   ├── PlayerController.cs       walk/sprint/jump/dodge/climb/stomp
│   ├── PlayerCombat.cs           light/heavy/parry, hit-feel
│   ├── PlayerStats.cs            health, stamina, snacks (IDamageable)
│   ├── Inputreader.cs            reads Input System, exposes clean props
│   ├── PlayerInputRouter.cs      alternate input router (+ pause)
│   └── BossArenaRespawnCheckpoint.cs
├── Enemies/
│   ├── EnemyAI.cs                abstract base: Patrol→Chase→Attack→Dead
│   ├── SpiderAI.cs               melee charger/circler
│   ├── SpiritAI.cs               boss: orbit + dash, enrage phase
│   ├── EnemyStats.cs             shared enemy health (IDamageable)
│   ├── SpiritPotion.cs           drop that completes the run
│   ├── Combatfx.cs               hit-stop / screen flash / sparks (singleton)
│   └── IDamageable.cs            damage interface
├── InteractiveObjects/
│   ├── Interactor.cs + IInteractable + InteractPromptUI.cs   raycast interaction
│   ├── CheckpointMarker.cs       orb markers
│   ├── Looptracker.cs            ordered checkpoint sequence
│   ├── OrbGate.cs                opens path once enough orbs collected
│   ├── TreeObstruction.cs / SlidingTreeObstruction.cs
│   ├── MushroomObstruction.cs / MushroomSpawner.cs / MushroomPathOrbsDeletus.cs
│   ├── PotionObstruction.cs
│   ├── SnackPickUp.cs            healing pickup
│   └── SpiderCountPrompt.cs      shows kill progress
├── Managers/
│   ├── GameManager.cs            top-level singleton
│   ├── AudioManager.cs           SFX/music (singleton, with cooldowns)
│   ├── ScreenFader.cs            persistent scene-fade (DontDestroyOnLoad)
│   ├── DeathScreen.cs            respawn / exit overlay
│   └── MainMenu/Intro/Victory/EndSceneManager.cs   scene flow
└── UI/
    ├── PlayerHUD.cs              health / stamina / snacks
    ├── InventoryHUD.cs           orbs / spiders / announcements
    ├── GamesStats.cs             objective tracker (singleton)
    ├── EnemyHealthbar.cs         world-space enemy bars
    └── Dialogue.cs               typewriter dialogue
```

---

## How it works (architecture)

The codebase is **event-driven and decoupled**: systems broadcast C# events, and other systems
subscribe rather than calling each other directly.

### Damage — the `IDamageable` interface
Both `PlayerStats` (Kitty) and `EnemyStats` (enemies) implement `IDamageable.TakeDamage(...)`.
`PlayerCombat` and enemy attacks simply call `TakeDamage` on whatever they hit, without needing to
know what type it is.

### Stats & objectives — events
- `PlayerStats` raises `HealthChanged`, `StaminaChanged`, `SnacksChanged`, and `Died`. `PlayerHUD` and `DeathScreen` listen.
- `EnemyStats` raises `OnHealthChanged`, `OnDamaged`, and `OnDied`. `EnemyHealthBar` and `GameStats` listen.
- `GameStats` (persistent singleton) tracks orbs collected and spiders killed, then raises
  `OnAllOrbsCollected`, `OnAllSpidersKilled`, and `OnPotionCollected`. Obstructions and the
  `InventoryHUD` subscribe to these to open paths and advance the game.

Because obstructions react to events, completing an objective anywhere automatically triggers the
right barrier to open — no manual wiring between the objective and the door.

### Enemies — shared state machine
`EnemyAI` is an abstract base implementing the `Patrol → Chase → Attack → Dead` loop on a
`NavMeshAgent`. `SpiderAI` and `SpiritAI` inherit from it and override behaviour hooks. The Spirit
fully overrides the update loop to run its own orbit/dash logic and an **enrage phase** below 50% HP,
then drops a `SpiritPotion` on death.

### Interaction
`Interactor` raycasts from the camera; anything implementing `IInteractable` (e.g. `OrbGate`,
`SpiderCountPrompt`) supplies a `Prompt` string and an `Interact()` method. `InteractPromptUI`
shows/hides the on-screen prompt automatically.

### Singletons & persistence
`GameManager`, `GameStats`, `AudioManager`, `SceneFader` (screen fades), and `CombatFX` are
singletons; the persistent ones use `DontDestroyOnLoad` and re-register scene objects on each
`sceneLoaded` so counts stay correct across scene transitions.

### Scene flow
`MainMenu → Intro → MainScene → Victory → End`, with `SceneFader` fading between them and
`DeathScreen` offering respawn (from the last `RespawnCheckpoint`) or a return to the menu.

---

## Requirements & setup

- **Unity** with the **Input System** package, **AI Navigation / NavMesh**, and **TextMesh Pro**.
- Open the project in Unity, make sure all scenes (`MainMenu`, `Intro`, `MainScene`, `Victory`,
  `End`) are added to **Build Settings → Scenes In Build** in that order, and press Play from the
  MainMenu scene.
- A baked **NavMesh** is required for enemies to move.

---

## Code conventions

The scripts follow a consistent Scripting style: serialized data uses `[SerializeField] private`
fields (kept private but still Inspector-assignable), cached components are fetched with
`TryGetComponent`, events are raised with `?.Invoke()`, and no namespaces are used. Plain data
containers (e.g. `TreeObstruction.TreeEntry`) and fields shared across scripts remain `public`.
