# DREAD SKETCH — Phase 0 Task Breakdown
### First Playable Build · Weeks 1–8

---

## HOW TO USE THIS DOCUMENT

- Tasks are ordered by dependency — do not skip ahead.
- Each task is sized to fit **within a single day** of AI-assisted development.
- Mark tasks with `[x]` when complete.
- If a task is blocked or taking too long, note it and move to the next non-dependent task.
- Prefix any task spilling into backlog with `[DEFER→P1]`.

**Complexity key:**
- 🟢 Easy — mostly boilerplate, Copilot will handle most of it
- 🟡 Medium — requires design decisions or manual wiring
- 🔴 Hard — novel system, expect iteration; apply the 2-hour rule

---

## WEEK 1–2: Project Foundation
**Goal: 5 players connected online in a grey-box map**

### Status Snapshot (2026-07-24)

- Multiplayer baseline is working end-to-end for host + client:
  - Lobby create/join by code
  - Relay transport connection
  - Netcode scene sync from MainMenu to ArtSchool_Greybox
  - Network player spawn, ownership, and replicated movement
- Current player implementation is a single prototype `NetworkPlayer` prefab.
- Remaining work to fully satisfy the week goal is scale validation (3-5 clients) and role split (Monster vs Survivors).

### Unity Project Setup

- [x] 🟢 Create Unity 6 (LTS) project with **HDRP** template
- [x] 🟢 Install packages via Package Manager:
  - `com.unity.netcode.gameobjects`
  - `com.unity.services.relay`
  - `com.unity.services.lobby`
  - `com.unity.services.authentication`
  - `com.unity.sentis`
- [x] 🟢 Set up GitHub repository and make initial commit
- [x] 🟢 Configure `.gitignore` for Unity (use `github/gitignore` Unity template)
  - ✅ The template ignores `/Library/`, `/Temp/`, `/Logs/`, `/Build/` — correct
  - ✅ `.meta` files are **NOT ignored** — Unity auto-generates them and they must be committed; they store GUIDs that link assets to scripts and prefabs. Losing them breaks all references.
  - ❌ Never manually delete `.meta` files — always let Unity create and remove them
- [x] 🟡 Set up Unity Gaming Services project in the UGS dashboard and link project ID in Unity Editor

### Grey-Box Map (Abandoned Art School)

- [x] 🟢 Create a single scene: `ArtSchool_Greybox`
- [x] 🟢 Block out map using Unity ProBuilder or primitive cubes:
  - Main hall (large open area)
  - 3 side rooms (corridors connecting them)
  - 2 floor levels with stairs or ramps
  - 4 wall positions where Gallery Frames will be placed (mark with placeholder quads)
  - 2 Exit Gate positions (mark with placeholder quads)
  - 4 Holding Frame wall positions (mark with placeholder quads)
  - 3 Ink Station positions (mark with placeholder cubes)
- [x] 🟢 Add basic HDRP directional light and baked lighting (low quality — just enough to see)
- [x] 🟢 Add a NavMesh bake for the map (needed later for Monster AI prototyping)

### Networking Foundation

- [x] 🔴 Create `NetworkManager` GameObject with `UnityTransport` using Relay
- [x] 🟡 Create `LobbyManager.cs` — handles:
  - `CreateLobby()` — host creates session, gets join code
  - `JoinLobby(string code)` — client joins by code
  - Display join code on screen (UI Text)
- [x] 🟡 Create `RelayManager.cs` — handles:
  - `CreateRelay(int maxPlayers)` → returns join code
  - `JoinRelay(string joinCode)` → connects client
- [x] 🟢 Create simple `MainMenu` scene with two buttons: **Host** and **Join**
- [x] 🟢 Wire Host button → `CreateLobby()` → `CreateRelay(5)` → load `ArtSchool_Greybox`
- [x] 🟢 Wire Join button → text input for code → `JoinLobby()` → `JoinRelay()` → load scene

### Player Spawning

- [x] 🟡 Create `PlayerSpawner.cs` (NetworkBehaviour) — spawns correct prefab per role:
  - First player to connect = Monster
  - Players 2–5 = Survivors
- [x] 🟢 Create `SurvivorPrefab` — capsule mesh, `NetworkObject`, `NetworkTransform`
- [x] 🟢 Create `MonsterPrefab` — capsule mesh (slightly taller), `NetworkObject`, `NetworkTransform`
- [x] 🟢 Define 5 spawn points in the map scene (tagged `SpawnPoint`)
- [x] 🟡 Test: host + 4 clients can all connect and see each other's capsules moving

Implementation note:
- Current prototype uses `NetworkPlayerController`, `NetworkPlayerSpawn`, and `PlayerSpawnManager` with one shared `NetworkPlayer` prefab.
- This is acceptable for Phase 0 networking validation, but role-specific prefabs and role assignment are still pending.

---

## WEEK 3–4: Drawing Systems
**Goal: DQS pipeline live, Key tool works end-to-end**

### Drawing Canvas UI

- [ ] 🟡 Create `DrawingCanvas` UI panel (Unity UI Toolkit or uGUI):
  - White background panel, 512×512 px draw area
  - Pencil cursor when hovering draw area
  - **Submit** button and **Clear** button
  - Opens/closes with TAB key
- [ ] 🟡 Create `StrokeCapture.cs`:
  - On pointer down → start stroke (List of Vector2 points)
  - On pointer move → add point to stroke, draw `LineRenderer` or `Texture2D` pixels
  - On pointer up → end stroke
  - Store all strokes as `List<List<Vector2>>`
- [ ] 🟡 Create `CanvasToTexture.cs`:
  - Render all strokes onto a `RenderTexture` (512×512)
  - Export as `Texture2D` on Submit press
- [ ] 🟢 Create `InkReserve.cs` (NetworkBehaviour per survivor):
  - `NetworkVariable<float> CurrentInk` starts at 100
  - `DeductInk(float amount)` — server-side
  - HUD display: ink bar UI element

### Unity Sentis + DQS Engine

- [ ] 🔴 Download or convert Quick, Draw! model to ONNX format:
  - Source: `github.com/googlecreativelab/quickdraw-dataset` (pre-trained TensorFlow → export ONNX)
  - Alternative: use a community-converted ONNX (search `quickdraw onnx huggingface`)
  - Place `.onnx` file in `Assets/Models/`
- [ ] 🔴 Create `DrawingRecogniser.cs` (uses Unity Sentis):
  - Load ONNX model with `ModelLoader.Load()`
  - `Recognise(Texture2D drawing)` → returns `(string category, float confidence)`
  - Preprocess: resize Texture2D to model input size (28×28 grayscale for Quick,Draw!)
  - Run inference with `worker.Execute()`
  - Return top-1 category label + softmax confidence score
- [ ] 🟡 Create `DQSCalculator.cs`:
  - Input: `Texture2D drawing`, `string expectedCategory`
  - **Recognizability (50%):** confidence score from `DrawingRecogniser`
  - **Completeness (30%):** ratio of non-white pixels that form closed shapes (edge detection heuristic)
  - **Detail (20%):** total stroke count / max expected strokes (clamped 0–1)
  - Output: `int dqsScore` (0–100)
- [ ] 🟢 Create `DQSDisplay.cs`:
  - After submission, show score overlay on canvas UI: e.g. "DQS: 74 — Competent"
  - Color code: red (0–19), orange (20–49), yellow (50–79), green (80–100)
- [ ] 🟡 Test: draw a key shape → submit → see DQS score and category label returned

### Key Tool — End-to-End

- [ ] 🟡 Create `ToolDefinition` ScriptableObject with fields:
  - `string toolName`, `int inkCost`, `float decayDuration`, `bool isOneUse`
  - Create asset: `Key.asset` (inkCost: 15, isOneUse: true)
- [ ] 🟡 Create `ToolSpawner.cs` (NetworkBehaviour):
  - `SpawnTool(string toolName, int dqsScore, Vector3 position)` [ServerRpc]
  - Instantiates the correct tool prefab as a `NetworkObject`
  - Stores `dqsScore` as a `NetworkVariable` on the prefab
- [ ] 🟢 Create `KeyPrefab`:
  - Simple key mesh (primitive or placeholder cube)
  - `NetworkObject` component
  - `KeyBehaviour.cs` — holds DQS score, exposes `Use()` method
- [ ] 🟢 Create `DoorPrefab`:
  - Plane blocking a doorway
  - `DoorBehaviour.cs`:
    - `TryUnlock(int dqsScore)` — unlock speed = `Mathf.Lerp(8f, 1f, dqsScore / 100f)` seconds
    - Plays open animation after unlock duration
- [ ] 🟡 Create `DrawingSubmitHandler.cs`:
  - On Submit: check active tool slot count (max 2)
  - Deduct ink via `InkReserve.DeductInk()`
  - Call `DrawingRecogniser.Recognise()` → `DQSCalculator.Calculate()`
  - Call `ToolSpawner.SpawnTool()`
  - Place spawned tool at player's feet
- [ ] 🟡 Create `ToolPickup.cs`:
  - Survivor walks over tool → press E → adds to active tool slot (max 2)
  - If 2 slots full, oldest tool is removed from world
- [ ] 🟡 Create `ToolUse.cs`:
  - Press F near a door while holding Key → calls `DoorBehaviour.TryUnlock(keyDQS)`
- [ ] 🟢 Test full loop: open canvas → draw key → submit → key spawns → pick up → use on door → door opens

### Ink Stations

- [ ] 🟢 Create `InkStationPrefab`:
  - Cylinder mesh at station positions in map
  - `InkStationBehaviour.cs`:
    - `Refill(InkReserve reserve)` — 4-second hold interaction, restores 60 ink
    - Plays fill animation / sound placeholder
- [ ] 🟢 Create `InteractionSystem.cs` (reusable):
  - Hold E near any `IInteractable` → progress bar fills → trigger on complete
  - `InkStationBehaviour` implements `IInteractable`

---

## WEEK 5–6: Monster Painting + Core Loop
**Goal: Monster paints pre-match, Holding Frame catch/free loop works, Monster can win**

### Monster Paint Canvas (Pre-Match Lobby)

- [ ] 🟡 Create `PainterLobby` scene — empty white room, single canvas on a wall
- [ ] 🟡 Create `MonsterPaintCanvas.cs`:
  - 6 paintable regions displayed as labelled quads on canvas: Head, Torso, Left Arm, Right Arm, Left Leg, Right Leg
  - Click a region → opens region paint panel (same stroke capture as survivor drawing canvas)
  - Each region stores its `Texture2D` independently
  - 90-second countdown timer
  - **Lock In** button submits all regions
- [ ] 🟡 Create `MonsterFeatureRecogniser.cs`:
  - For each region Texture2D: run `DrawingRecogniser.Recognise()`
  - Build `List<string> recognisedFeatures` from all regions
  - Map features to abilities (see table below)
- [ ] 🟢 Create `MonsterAbilitySet.cs` (ScriptableObject-style runtime data):
  - `bool hasWings`, `bool hasLargeEyes`, `bool hasBlankZone`, etc.
  - Populated by `MonsterFeatureRecogniser` at match start
- [ ] 🟡 Implement **3 feature abilities** for Phase 0 (others deferred to Phase 1):

  | Feature | Ability | Implementation |
  |---|---|---|
  | Wings (recognized on back region) | +15% move speed | Multiply `CharacterController.speed` |
  | Large eyes (recognized on head region) | Detection radius +4m | Increase aura sphere radius on Monster |
  | Blank region (no strokes on any region) | Vulnerability zone | Tag that region as `IsVulnerable = true` |

- [ ] 🟡 Sync Monster appearance to all clients:
  - Serialize all 6 region `Texture2D`s as byte arrays
  - Send via `ServerRpc` → apply as decal textures on Monster mesh on all clients

### Monster Character Controller

- [ ] 🟢 Create `MonsterController.cs`:
  - WASD/Arrow movement, `CharacterController` component
  - `LungeAttack()` on left-click: forward dash 3 units, detect survivor collision
  - `PickUpSurvivor(SurvivorController survivor)` — attaches survivor to carry position
  - `PlaceSurvivorInFrame(HoldingFrame frame)` — deposits carried survivor
- [ ] 🟡 Create `DownedState.cs` on Survivor:
  - When downed: disable normal movement, enable slow crawl only
  - Show "Downed" UI indicator
  - Monster can pick up downed survivor with E press

### Holding Frame System

- [ ] 🟡 Create `HoldingFramePrefab` (4 placed on walls in map):
  - Frame mesh (ornate border placeholder)
  - `HoldingFrameBehaviour.cs` (NetworkBehaviour):
    - `NetworkVariable<bool> IsOccupied`
    - `NetworkVariable<ulong> CapturedPlayerId`
    - `Capture(ulong survivorId)` [ServerRpc]
    - `Release()` [ServerRpc]
- [ ] 🟡 Create `HoldingFrameManager.cs`:
  - Tracks all 4 frames
  - `AllFramesOccupied()` → returns bool
  - Calls `VictoryManager.MonsterWins()` when all 4 occupied simultaneously
- [ ] 🟡 Add Survivor rescue interaction to `HoldingFrameBehaviour`:
  - Survivor approaches occupied frame with a Key → press E → calls `Release()` [ServerRpc]
  - Consumes the Key on use
  - Freed survivor returns with 50% Ink

### Survivor Character Controller

- [ ] 🟢 Create `SurvivorController.cs`:
  - WASD movement, sprint (hold Shift), crouch (hold C)
  - Cannot open Drawing Canvas while sprinting (add `IsRunning` guard)
  - First-person or third-person camera (choose one — third-person easier for multiplayer)
- [ ] 🟢 Add `ActiveToolSlots.cs` to Survivor:
  - `List<ToolInstance> slots` (max 2)
  - HUD shows both slot icons

### Victory Manager

- [ ] 🟡 Create `VictoryManager.cs` (NetworkBehaviour, server-authoritative):
  - `MonsterWins()` [ServerRpc] — called by `HoldingFrameManager`
  - `SurvivorWins(ulong survivorId)` [ServerRpc] — called by gate/hatch escape
  - Both methods: stop match, broadcast result to all clients [ClientRpc]
- [ ] 🟢 Create `EndMatchScreen.cs`:
  - Shows "Monster Wins" or "Survivors Win"
  - **Return to Lobby** button → unload match scene, reload `MainMenu`

---

## WEEK 7–8: Gallery Frames + Full Match Loop
**Goal: Survivor objective complete, both win conditions reachable, full loop playable**

### Gallery Frames

- [ ] 🟡 Create `GalleryFramePrefab` (2 placed in map for Phase 0):
  - Ornate frame mesh with canvas inside
  - Shows a silhouette hint image (use placeholder black silhouettes: bird, house, hand, clock)
  - `GalleryFrameBehaviour.cs` (NetworkBehaviour):
    - `NetworkVariable<bool> IsActivated`
    - `TryActivate(Texture2D drawing)` [ServerRpc]:
      - Run `DQSCalculator.Calculate()` with hint category as expected
      - If DQS ≥ 20 → set `IsActivated = true` → play light-up animation
      - If DQS < 20 → play flicker/reject animation
- [ ] 🟡 Create `GalleryFrameManager.cs`:
  - Tracks all 2 frames (Phase 0) / 4 frames (Phase 1)
  - `AllFramesActivated()` → returns bool
  - When true: powers Exit Gate(s)
- [ ] 🟡 Create `MonsterInkBloom.cs`:
  - While a survivor is drawing at a Gallery Frame, send position to Monster client as a world-space glow particle [ClientRpc targeted at Monster]
  - Disappears when survivor stops drawing

### Ink Station (drawing interaction)

- [ ] 🟡 Extend `DrawingSubmitHandler.cs` for Gallery Frame activation:
  - If player is standing near a Gallery Frame and presses TAB → open canvas in "Frame Mode"
  - On submit: call `GalleryFrameBehaviour.TryActivate(texture)` instead of spawning a tool
  - Frame Mode does not cost Ink

### Exit Gate

- [ ] 🟡 Create `ExitGatePrefab` (1 placed in map for Phase 0):
  - Gate mesh blocking an exit corridor
  - `ExitGateBehaviour.cs` (NetworkBehaviour):
    - `NetworkVariable<bool> IsPowered` — set true by `GalleryFrameManager`
    - `NetworkVariable<bool> IsOpen`
    - While powered: show green light
    - Survivor presses E at gate while holding Key → `TryOpen(int keyDQS)` [ServerRpc]
    - Open time = `Mathf.Lerp(12f, 4f, keyDQS / 100f)` seconds
    - On open: trigger `VictoryManager.SurvivorWins(survivorId)`
    - Without key: hold E for 12 seconds flat to open (fallback)

### End-of-Match Screen Polish

- [ ] 🟢 Show each player's name and role on end screen
- [ ] 🟢 Show "Draw again?" / "Return to lobby" buttons
- [ ] 🟢 Ensure all `NetworkObject`s are properly despawned on match end (prevent memory leaks)

### Full Loop Integration & Bug Sprint

- [ ] 🟡 Playtest Week 1–7 systems together as a 5-player session (use Unity Multiplex or 5 Editor instances via ParrelSync)
- [ ] 🟡 Fix top-priority bugs found during integration playtest
- [ ] 🟢 Add placeholder UI for:
  - Ink Reserve bar (HUD, bottom left)
  - Active tool slots ×2 (HUD, bottom centre)
  - Frame activation count (HUD, top centre): "Frames: 0 / 2"
  - Holding Frames filled count visible to Monster (HUD, top right): "Captured: 0 / 4"
- [ ] 🟡 Add basic audio placeholders (no real audio yet — Unity built-in AudioSource with primitive clips):
  - Footstep: play `AudioClip` on every other step
  - Drawing submit: short click sound
  - Frame activated: chime sound
  - Match end: victory / defeat stinger

### Phase 0 Final Acceptance Test

Run through this checklist with 5 real players (or ParrelSync):

- [ ] Host creates game, 4 clients join by code within 30 seconds
- [ ] Monster is assigned to first-connected player, Survivors to the rest
- [ ] Monster opens paint canvas, paints, locks in — abilities confirmed in console log
- [ ] Match loads — Monster is visually distinct, moves around map
- [ ] Survivor opens drawing canvas with TAB (standing still only)
- [ ] Survivor draws a key shape — DQS score appears
- [ ] Key spawns at survivor's feet — survivor picks it up
- [ ] Survivor uses key on door — door opens at DQS-scaled speed
- [ ] Survivor approaches Gallery Frame — Monster sees ink bloom
- [ ] Survivor draws silhouette hint at frame — frame activates (DQS ≥ 20)
- [ ] Both frames activated → Exit Gate powers (green light)
- [ ] Survivor uses key on exit gate → gate opens → Survivor Win screen
- [ ] Monster lunges → survivor enters Downed state
- [ ] Monster picks up downed survivor → carries → places in Holding Frame
- [ ] Another survivor draws key → frees captive survivor
- [ ] Monster fills all 4 Holding Frames → Monster Win screen
- [ ] Return to lobby works cleanly with no errors

---

## DEFERRED TO PHASE 1 — DO NOT IMPLEMENT IN PHASE 0

These are confirmed features that must not enter the Phase 0 sprint:

| Feature | Reason for Deferral |
|---|---|
| All 9 survivor tools (Torch, Rope, Bandage, etc.) | Key proves the pattern — others follow in Month 3 |
| All 10 Monster painted feature abilities | Only 3 needed to validate the system |
| The Reveal sequence (canvas tear, cutscene) | Core loop works without it |
| Fear Meter / Nervous Laughter | Depends on Reveal |
| Critique System | Depends on Reveal |
| Gallery Frames 3 and 4 | 2 is sufficient to test the mechanic |
| Second Exit Gate | 1 is sufficient |
| The Hatch | Last-survivor edge case — Phase 1 |
| Anti-camp / anti-tunnel rules | Balance pass — Phase 1 |
| Badge system | Post-match feature — Phase 1 |
| Map art pass | Greybox only in Phase 0 |
| Audio pass | Placeholders only |
| Ranked / Casual split | Phase 2 |

---

*Phase 0 target: Week 8 end. If it runs long, cut from Week 7–8 art/audio tasks first — never cut networking or core loop tasks.*
