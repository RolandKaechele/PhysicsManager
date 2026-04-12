# PhysicsManager

A data-driven Unity physics settings and collision management framework.  
Manages named physics profiles (gravity, timestep, simulation mode), collision layer rules, global impact events, and physics pause/resume — all configurable from JSON.  
Optionally integrates with [StateManager](https://github.com/RolandKaechele/StateManager), [CameraManager](https://github.com/RolandKaechele/CameraManager), [EventManager](https://github.com/RolandKaechele/EventManager), and [MapLoaderFramework](https://github.com/RolandKaechele/MapLoaderFramework).


## Features

- **Physics profiles** — named snapshots of gravity, fixed timestep, simulation mode and sleep threshold; switch at runtime in one call
- **Collision layer rules** — define enable/disable rules between named layers; apply from Inspector or JSON
- **Pause / Resume** — suspend physics simulation (e.g. during cutscenes or loading screens) and restore on resume
- **Impact events** — `ImpactReporter` component aggregates collision impulses; `OnImpact` fires above your configured threshold
- **Time-scale helpers** — `SetTimeScale()` / `ResetTimeScale()` maintain a stable fixed timestep ratio
- **JSON authoring** — define profiles in `StreamingAssets/physics_profiles/` and collision rules in `StreamingAssets/collision_rules/`; no recompile required
- **Modding support** — JSON entries are merged over Inspector entries by id at runtime
- **StateManager integration** — `StateManagerBridge` auto-pauses/resumes physics on Cutscene or Loading states (activated via `PHYSICSMANAGER_STM`)
- **EventManager integration** — `EventManagerBridge` fires `physics.impact` and `physics.profile.changed` events (activated via `PHYSICSMANAGER_EM`)
- **MapLoaderFramework integration** — `MapLoaderBridge` activates the map's configured `physicsProfileId` on chapter load (activated via `PHYSICSMANAGER_MLF`)
- **CameraManager integration** — `CameraManagerBridge` triggers camera shake on significant impacts (activated via `PHYSICSMANAGER_CAM`)
- **Custom Inspector** — runtime profile activator, pause/resume buttons, and profile list in the Unity Inspector
- **DOTween Pro integration** — `DotweenPhysicsBridge` smoothly tweens gravity between profiles and provides `SlowMotion()` / `ResetTimeScale()` with easing (activated via `PHYSICSMANAGER_DOTWEEN`)
- **Odin Inspector integration** — `SerializedMonoBehaviour` base (activated via `ODIN_INSPECTOR`)


## Installation

### Option A — Unity Package Manager (Git URL)

```
https://github.com/RolandKaechele/PhysicsManager.git
```

### Option B — Clone into Assets

```bash
git clone https://github.com/RolandKaechele/PhysicsManager.git Assets/PhysicsManager
```


## Quick Start

### 1. Add PhysicsManager to your scene

Add **PhysicsManager** to a persistent manager `GameObject`.

### 2. Configure profiles in the Inspector

Add `PhysicsProfile` entries (gravity, timestep, simulation mode) to the `Profiles` list.

### 3. Use from code

```csharp
var pm = FindFirstObjectByType<PhysicsManager.Runtime.PhysicsManager>();

// Switch physics profile
pm.ActivateProfile("underwater");

// Pause / resume physics
pm.PausePhysics();
pm.ResumePhysics();

// Bullet-time via time scale
pm.SetTimeScale(0.3f);
pm.ResetTimeScale();

// Register a new profile at runtime
pm.RegisterProfile(new PhysicsProfile { id = "custom", gravity = new Vector3(0, -5f, 0) });
```

### 4. Add ImpactReporter to any physics object

```csharp
// On any Rigidbody GameObject — no extra code needed
gameObject.AddComponent<PhysicsManager.Runtime.ImpactReporter>();

// Subscribe to impact events
pm.OnImpact += data => Debug.Log($"Impact {data.impulse:F1} at {data.point}");
```


## Physics JSON Format

**`StreamingAssets/physics_profiles/`** — physics profiles (example: `physics_profiles/main.json`):

```json
{
  "profiles": [
    {
      "id": "default",
      "label": "Default",
      "gravity": { "x": 0, "y": -9.81, "z": 0 },
      "fixedTimestep": 0.02,
      "maxAllowedTimestep": 0.3333,
      "autoSimulation": true,
      "simulationMode": 0,
      "sleepThreshold": 0.005
    }
  ]
}
```

**`StreamingAssets/collision_rules/`** — collision layer rules (example: `collision_rules/main.json`):

```json
{
  "collisionRules": [
    { "layerA": "Player", "layerB": "Enemy", "enabled": true }
  ]
}
```

| `simulationMode` | Value | Description |
| ---------------- | ----- | ----------- |
| FixedUpdate | 0 | Default Unity physics update |
| Update | 1 | Physics updated in Update |
| Script | 2 | Manual simulation drive |


## Optional Integration Defines

| Define | Manager | Effect |
| ------ | ------- | ------ |
| `PHYSICSMANAGER_STM` | StateManager | Pause physics during Cutscene/Loading states |
| `PHYSICSMANAGER_EM` | EventManager | Fire physics.impact / physics.profile.changed |
| `PHYSICSMANAGER_MLF` | MapLoaderFramework | Switch profile from map's physicsProfileId |
| `PHYSICSMANAGER_CAM` | CameraManager | Camera shake on significant impact |
| `PHYSICSMANAGER_DOTWEEN` | DOTween Pro | Tween gravity transitions and slow-motion |


## Runtime API

### `PhysicsManager`

| Member | Description |
| ------ | ----------- |
| `ActivateProfile(id)` | Apply a named physics profile instantly |
| `RegisterProfile(profile)` | Register or replace a profile at runtime |
| `GetProfile(id)` | Return `PhysicsProfile` by id, or null |
| `GetAllProfileIds()` | All registered profile ids |
| `PausePhysics()` | Halt physics simulation |
| `ResumePhysics()` | Restore physics simulation |
| `ApplyCollisionRules(rules)` | Apply layer collision rules |
| `ReportImpact(data)` | Report an impact from an ImpactReporter |
| `SetTimeScale(scale)` | Set time-scale with proportional fixedDeltaTime |
| `ResetTimeScale()` | Restore time-scale to 1 |
| `CurrentProfileId` | Currently active profile id |
| `IsPaused` | True while physics is paused |
| `OnImpact` | `event Action<ImpactData>` — fires on threshold impact |
| `OnProfileChanged` | `event Action<string, string>` — fires on profile switch |
| `OnPhysicsPaused` | `event Action` — fires when physics pauses |
| `OnPhysicsResumed` | `event Action` — fires when physics resumes |
| `ProfileActivatedOverride` | `Action<PhysicsProfile, Action>` delegate |

### `DotweenPhysicsBridge` *(requires `PHYSICSMANAGER_DOTWEEN`)*

| Member | Description |
| ------ | ----------- |
| `SlowMotion()` | Ramp time scale to configured slow-motion value |
| `ResetTimeScale()` | Ramp time scale back to 1 |


## Editor Tools

Open via **JSON Editors → Physics Manager** in the Unity menu bar, or via the **Open JSON Editor** button in the PhysicsManager Inspector.

Edits profiles and collision rules stored in two separate folders: `StreamingAssets/physics_profiles/` (`PhysicsProfile`) and `StreamingAssets/collision_rules/` (`CollisionLayerRule`).

| Action | Result |
| ------ | ------ |
| **Load** | Reads all `*.json` from `StreamingAssets/physics_profiles/` and `StreamingAssets/collision_rules/`; creates missing folders automatically |
| **Edit** | Add / remove / reorder profiles and collision rules using the Inspector list |
| **Save** | Writes each profile as `<id>.json` to `StreamingAssets/physics_profiles/` and each collision rule as `<id>.json` to `StreamingAssets/collision_rules/`; entries without an `id` are skipped. Calls `AssetDatabase.Refresh()` |

With **ODIN_INSPECTOR** active, lists use Odin's enhanced drawer (drag-to-sort, collapsible entries).


## Dependencies

| Dependency | Required | Notes |
| ---------- | -------- | ----- |
| Unity 2022.3+ | ✓ | |
| StateManager | optional | Required when `PHYSICSMANAGER_STM` is defined |
| EventManager | optional | Required when `PHYSICSMANAGER_EM` is defined |
| MapLoaderFramework | optional | Required when `PHYSICSMANAGER_MLF` is defined |
| CameraManager | optional | Required when `PHYSICSMANAGER_CAM` is defined |
| DOTween Pro | optional | Required when `PHYSICSMANAGER_DOTWEEN` is defined |
| Odin Inspector | optional | Required when `ODIN_INSPECTOR` is defined |


## License

MIT — see [LICENSE](LICENSE).
