# DREAD SKETCH — Development Plan
### Unity · Asymmetric Online Horror · Steam PC
### Solo Developer · AI-Assisted · Revised Plan

---

## 1. EXECUTIVE SUMMARY

| Item | Detail |
|---|---|
| **Engine** | Unity 6 (LTS) |
| **Platform target** | Steam (PC / Mac) |
| **Genre** | Asymmetric multiplayer horror |
| **Target player count** | 5 per session (1 Monster, 4 Survivors) |
| **Developer** | Solo (1 person + AI toolchain) |
| **First playable build** | End of Month 2 |
| **Early Access target** | End of Month 10 |
| **Full release target** | End of Month 18 |
| **Estimated total budget** | $8,000 – $22,000 USD (tools, services, assets) |

---

## 2. THE AI-ASSISTED SOLO WORKFLOW

You are the sole human developer. AI handles code generation, asset creation, debugging, and documentation. Your job is direction, testing, taste, and final decisions.

### AI Tools by Discipline

| Discipline | Primary Tool | Use |
|---|---|---|
| **Code** | VS Code + GitHub Copilot Pro | Unity C# generation, boilerplate, debugging, refactoring |
| **Code (complex)** | Claude / GPT-4o (in chat) | Architecture decisions, networking logic, algorithm design |
| **3D Models** | Meshy AI / CSM AI | Low-poly props, environment assets, Survivor models |
| **Textures / Art** | Stable Diffusion (ComfyUI) / Midjourney | Concept art, texture maps, lore illustration |
| **Monster Painting Canvas** | Unity built-in + Shader Graph | Custom paint layer system — hand-coded with Copilot assist |
| **Drawing Recognition (DQS)** | Unity Sentis + Quick,Draw! ONNX | On-device inference, no API cost |
| **Audio / SFX** | ElevenLabs SFX / Freesound + AI upscale | Footsteps, ambient, UI sounds |
| **Music** | Udio / Suno | Atmospheric horror score per map mood |
| **UI** | Figma (AI plugins) + Unity UI Toolkit | Menus, HUD, badge display |
| **QA / Bug finding** | Claude + manual playtesting | Describe bugs in plain English, get fix suggestions |
| **Documentation** | GitHub Copilot Chat | Inline docs, changelogs, README generation |

### Daily Workflow

```
Morning  → Define today's task in plain English
         → Prompt Copilot/Cursor to generate or scaffold
         → Review, adjust, wire into existing systems

Afternoon → Playtest what was built
          → Note bugs and design issues
          → Use AI chat to diagnose and fix

Evening  → Commit to GitHub
          → Update task tracker (Linear / Notion)
          → Draft tomorrow's task list
```

**Rule:** Never spend more than 2 hours stuck on a single problem. Describe it to an AI assistant, get an alternative approach, or reduce scope. Forward momentum beats perfect solutions.

---

## 3. TECHNOLOGY STACK

### Engine & Runtime

| Tool | Purpose |
|---|---|
| **Unity 6 (LTS)** | Core engine |
| **Unity Netcode for GameObjects** | Multiplayer session networking |
| **Unity Relay + Lobby** | Peer connection without a dedicated server |
| **Unity GameServices (UGS)** | Authentication, cloud save, leaderboards |
| **Steamworks SDK (Facepunch wrapper)** | Steam achievements, leaderboards, DRM |

> **Solo note:** Use Unity Relay (not dedicated servers) until post-launch. It's free for small sessions and requires zero server management.

### Drawing Recognition (DQS)

| Tool | Role |
|---|---|
| **Unity Sentis** | Run ONNX models inside Unity — no API calls, no internet required mid-match |
| **Quick, Draw! ONNX model** | Pre-trained on 50M drawings, 345 shape categories — covers all 9 survivor tools |
| **Custom fine-tune (Month 6+)** | Train on real player drawing data collected during alpha using Google Teachable Machine or a lightweight PyTorch model |

> The DQS system runs fully local. No latency, no per-call cost, works offline.

### Art Pipeline (AI-Assisted Solo)

| Tool | Purpose |
|---|---|
| **Blender (+ AI Blender plugins)** | Model editing, rigging, scene layout |
| **Meshy AI** | Generate low-poly 3D assets from text prompts |
| **Stable Diffusion (ComfyUI)** | Texture generation, concept art, lore illustrations |
| **Unity HDRP** | Lighting and atmosphere |
| **Shader Graph** | Paint drip, wet canvas, ink flood shaders |

### Backend (Minimal)

| Tool | Purpose | Cost |
|---|---|---|
| **Unity Relay** | Session networking | Free up to 50 CCU, then $9/mo |
| **Unity GameServices** | Auth + cloud save | Free tier covers early players |
| **Supabase (free tier)** | Badge storage, Living Archive votes | Free |
| **GitHub** | Version control | Free |

### Productivity

| Tool | Purpose |
|---|---|
| **VS Code + GitHub Copilot Pro** | Primary editor with inline AI completions, chat, and code generation |
| **GitHub + GitHub Actions** | Version control + automated Steam builds |
| **Linear** | Task tracking (better than Jira for solo) |
| **Notion** | GDD, notes, living design doc |
| **Discord (personal server)** | Community when ready |

---

## 4. PHASED DEVELOPMENT PLAN

---

### PHASE 0 — First Playable Build
**Duration:** Weeks 1–8 (2 months)
**Goal:** A working match from start to finish. Ugly but functional. You can host a game, play Monster, and have friends join as Survivors.

**Scope (strict — cut anything not on this list):**

**Week 1–2: Project Foundation**
- [ ] Unity 6 project with HDRP, Netcode, Relay configured
- [ ] Scene: one grey-box map (Abandoned Art School layout, no art)
- [ ] Basic character controller: move, sprint, crouch (Survivors)
- [ ] Basic Monster controller: move, lunge attack
- [ ] Unity Relay lobby: host game, join by code, 5 players connected

**Week 3–4: Drawing Systems**
- [ ] Drawing canvas UI: open with TAB, draw with mouse, submit
- [ ] Unity Sentis + Quick,Draw! model integrated and returning a category + confidence score
- [ ] DQS formula implemented (Recognizability 50% + Completeness 30% + Detail 20%)
- [ ] One tool fully working end-to-end: **Key** — draw it, it spawns in world, use it on a door
- [ ] Ink Reserve (100 units) tracks and depletes

**Week 5–6: Monster Painting + Core Loop**
- [ ] Pre-match Monster paint lobby: flat 2D canvas, 6 body regions (head, torso, arms, legs)
- [ ] At least 3 feature recognitions working (wings → speed, large eyes → detection range, blank → vulnerability)
- [ ] Holding Frame mechanic: Monster downs Survivor, carries them, places in Frame
- [ ] Survivor freed by drawing a Key on Frame lock
- [ ] Victory condition: all 4 Frames filled simultaneously = Monster wins

**Week 7–8: Gallery Frames + Match Loop**
- [ ] 2 Gallery Frames placed in map with silhouette hints
- [ ] Completing both Frames powers 1 Exit Gate
- [ ] Survivor escapes through gate = Survivor wins
- [ ] End-of-match screen with winner display
- [ ] Basic Ink Stations (walk up, hold E, restores Ink)
- [ ] Playable from lobby → match → victory screen → lobby

**Phase 0 Definition of Done:**
> Five people can connect online, play a full match with a clear winner, and understand the core gameplay loop.

---

### PHASE 1 — Alpha
**Duration:** Months 3–6
**Goal:** All core systems in. The game feels like Dread Sketch, not a tech demo.

**Month 3:**
- [ ] All 9 Survivor tools implemented with DQS scaling
- [ ] All 10 Monster painted feature abilities
- [ ] 2 Gallery Frames per map → 4 Gallery Frames (full objective loop)
- [ ] 2 Exit Gates, Hatch (last survivor), full escape logic
- [ ] Anti-camp and anti-tunnel rules

**Month 4:**
- [ ] The Reveal sequence: canvas tear, forced cutscene, Monster emergence
- [ ] Fear Meter and Nervous Laughter Meter based on Monster design
- [ ] Critique System (survivor votes during Reveal)
- [ ] First art pass: one map with real textures and lighting (Abandoned Art School)
- [ ] Basic audio: footsteps, drawing SFX, Reveal sting

**Month 5:**
- [ ] Second map completed: Museum After Midnight (with map environmental rule)
- [ ] Badge system backend (track and award all Monster + Survivor badges)
- [ ] Post-match scoreboard with badges earned
- [ ] Basic progression: Sketch Styles unlocked by match count

**Month 6:**
- [ ] Closed Alpha: invite 20–50 friends/testers
- [ ] Collect DQS frustration data (are players annoyed by rejections?)
- [ ] Balance pass based on playtests
- [ ] Bug fix sprint

---

### PHASE 2 — Beta
**Duration:** Months 7–10
**Goal:** Content complete. Polished enough for Early Access.

**Month 7–8:**
- [ ] Maps 3–6 complete with art passes and environmental rules
- [ ] Full audio pass: per-map ambient, Monster audio profile, Ink Station hum
- [ ] Custom DQS model fine-tuned on Alpha drawing data
- [ ] Paint shader polish: drip particles, canvas tear VFX, wet ink bloom
- [ ] Living Archive MVP (community gallery, weekly votes)

**Month 9:**
- [ ] Steam page live with trailer
- [ ] Open Beta: public playtest via Steam
- [ ] Ranked / Casual mode split
- [ ] Controller support with drawing assist option
- [ ] Anti-cheat: basic session integrity checks

**Month 10:**
- [ ] Final balance pass from Beta feedback
- [ ] Remaining 4 maps (grey-box acceptable at launch, full art in post-launch updates)
- [ ] Accessibility: colorblind mode, UI scaling
- [ ] **Early Access launch on Steam**

---

### PHASE 3 — Post-Launch (Months 11–18)
**Goal:** Retain Early Access players, respond to community, ship 1.0.

- Monthly patch cadence based on player feedback
- Content Update 1 (Month 13): 1 new tool + 1 new map full art pass
- Content Update 2 (Month 16): Season 2 badge set, Living Archive champion announced
- Anti-cheat upgrade (BattlEye or Easy Anti-Cheat via Steam)
- **Full Release 1.0 (Month 18)**

---

## 5. BUDGET BREAKDOWN

No salaries. All costs are tools, services, and optional asset purchases.

### One-Time Costs

| Item | Cost | Notes |
|---|---|---|
| Unity Personal | $0 | Free until gross revenue exceeds $200K/yr |
| Unity Pro | $2,040/yr | Only required after $200K revenue — or to remove the splash screen earlier |
| VS Code + GitHub Copilot Pro | $100/yr | Covers all AI code assistance |
| Steamworks app registration | $100 | One-time |
| Key art / capsule art (Midjourney + freelance cleanup) | $200–$800 | — |
| Trailer editing (DaVinci Resolve — free, or freelance) | $0–$1,500 | — |
| Audio packs (Freesound + licensed SFX libraries) | $100–$300 | — |
| Meshy AI Pro (3D asset generation) | $240/yr | — |
| Midjourney Standard (textures, concept art) | $288/yr | — |
| **One-time subtotal (before Pro threshold)** | **~$1,100–$3,300** | Unity Pro not counted until needed |

### Monthly Running Costs

| Item | Monthly Cost |
|---|---|
| Unity Relay (up to ~200 CCU) | $9 |
| Supabase (free tier) | $0 |
| GitHub Pro | $4 |
| Linear (solo — free) | $0 |
| Udio or Suno (music generation) | $10–$30 |
| ElevenLabs SFX | $5–$22 |
| **Monthly subtotal** | **~$28–$65/mo** |

### Total 18-Month Budget Estimate

| Category | Total |
|---|---|
| Software subscriptions (18 months) | $400–$900 |
| One-time setup costs (no Unity Pro) | $1,100–$3,300 |
| Marketing (trailer, key art, streamer outreach) | $2,000–$8,000 |
| Contingency (15%) | $500–$1,800 |
| **Grand total** | **$4,000–$14,000** |

> Unity Pro is **not included** in this estimate. Add $2,040/yr only if you exceed $200K revenue or decide to remove the splash screen before that point. If Early Access earns money, reinvest in freelance art passes or a part-time contractor for the heaviest tasks.

---

## 6. CRITICAL PATH (SOLO)

Build in strict dependency order. Do not start art until systems work.

```
[1] Unity project + Netcode + Relay — 5 players connected online
    └── Everything depends on this. Do it in Week 1.

[2] DQS pipeline — Unity Sentis + Quick,Draw! model
    └── All survivor tools depend on this. Target: end of Week 3.

[3] One complete tool (Key) — draw → spawn → use on door
    └── Proves the full tool pipeline. Everything else follows this pattern.

[4] Monster paint canvas (basic) — 6 regions, 3 feature recognitions
    └── Blocks Reveal sequence and Fear Meter.

[5] Holding Frame catch/free loop + victory condition
    └── Blocks any meaningful balance testing.

[6] Gallery Frames → Gates → Escape
    └── Completes the survivor win condition. Now you have a real game.
```

---

## 7. RISKS & MITIGATIONS

### Solo-Specific Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Scope creep kills the 2-month target | Very High | The Phase 0 list is final. Nothing gets added. Extras go in a "Phase 1" backlog note, never into the current sprint. |
| Burnout | High | Cap working hours at 8/day. Take one full day off per week. A tired solo dev ships nothing. |
| Getting stuck on a system for days | High | 2-hour rule: if blocked, prompt AI, reduce scope, or skip and return later. Never let a single problem halt the project. |
| AI-generated code quality (subtle bugs) | Medium | Always playtest generated code immediately. Write one test per critical system (DQS scoring, victory condition, Ink deduction). |
| Multiplayer networking complexity | High | Unity Relay + Netcode removes server management. If Netcode is too complex, evaluate Fish-Net (simpler API) in Month 1. |
| Drawing recognition too inaccurate | High | Always accept any DQS > 0. Never hard-reject a drawing. Show the score. Players can redraw if unhappy. |

### Technical Risks

| Risk | Mitigation |
|---|---|
| Unity Sentis model inference too slow on low-end PCs | Run inference asynchronously; show a 0.5s "analyzing..." animation to mask latency |
| Monster body deformation too complex | Use 2D sprite overlay on 3D mesh for v1.0 — paint regions are flat decals on the body |
| Matchmaking queue too long (small player base) | Add LAN / invite-code mode so 5 friends can always play without matchmaking |
| Steam review bombing at Early Access | Be transparent in store page about EA state; post a public roadmap at launch |

---

## 8. AI USAGE GUIDELINES

You are using **VS Code + GitHub Copilot Pro**. Key features to use:
- **Inline completions** — accept with Tab as you type C# in Unity scripts
- **Copilot Chat (Ask / Edit mode)** — use for architecture questions and multi-file edits
- **Copilot Edits** — select a block of code and ask it to refactor or fix in-place
- **`@workspace` context** — prefix prompts with `@workspace` so Copilot reads your whole project before answering

**Effective prompting patterns:**
```
"In Unity 6 with Netcode for GameObjects, write a [ServerRpc / ClientRpc] that..."
"Write a C# MonoBehaviour for Unity that takes a Texture2D of a player's drawing
 and runs it through a Unity Sentis ONNX model, returning the top category and confidence."
"Refactor this Unity coroutine to use async/await with UniTask."
"This script causes a NullReferenceException when [X]. Here is the stack trace: [paste]. Fix it."
```

**What AI does well on this project:**
- Netcode boilerplate (ServerRpc, ClientRpc, NetworkVariable)
- Unity UI Toolkit event handling
- Shader Graph node explanations and HLSL snippets
- Steamworks integration (Facepunch wrapper has great Copilot coverage)
- State machine logic for Monster/Survivor phases

**What AI does poorly — review carefully:**
- Multiplayer race conditions (always test with 5 clients)
- DQS scoring balance (tune numbers manually after playtesting)
- Unity HDRP-specific lighting bugs (check Unity forums)
- Any code that interacts with player-generated content (sanitize inputs)

---

## 9. STEAM LAUNCH STRATEGY

### Getting to 15,000 Wishlists Before Early Access (Month 10)

- **Month 3:** Post a raw prototype video on Twitter/X and TikTok. Show the Monster painting + Reveal moment. No commentary needed — let the concept speak.
- **Month 5:** Post a "survivors draw terrible tools" clip. Drawing-game audiences love failure content.
- **Month 7:** Steam page goes live. Free Drawing Demo (solo Survivor puzzle map, no multiplayer needed).
- **Month 9:** Enter **Steam Next Fest** with demo build.
- **Month 10:** Early Access launch.

### Target Audiences (in priority order)
1. Dead by Daylight players who want something fresh
2. Gartic Phone / Skribbl.io players — they already love drawing under pressure
3. Horror game streamers — the Reveal is a guaranteed reaction clip
4. Art/creativity game communities

### Pricing

| Version | Price |
|---|---|
| Early Access | $14.99 |
| Full Release 1.0 | $19.99 |
| Supporter Pack | $24.99 (game + exclusive badge set) |

---

## 10. MILESTONE SUMMARY

```
WEEK 8    ★ FIRST PLAYABLE — 5 players online, full match loop
MONTH 3     All 9 tools + Monster abilities functional
MONTH 4     Reveal sequence + Fear Meter + Critique System
MONTH 5     2 maps art-complete, badge system live
MONTH 6     Closed Alpha (20–50 testers)
MONTH 7     Steam page + trailer published
MONTH 9     Open Beta via Steam Next Fest
MONTH 10  ★ EARLY ACCESS LAUNCH ($14.99)
MONTH 13    Content Update 1
MONTH 16    Content Update 2 + Season 2 badges
MONTH 18  ★ FULL RELEASE 1.0 ($19.99)
```

---

## 11. WEEKLY FOCUS — FIRST 8 WEEKS (DETAILED)

| Week | Focus | Key Deliverable |
|---|---|---|
| 1 | Unity setup, Netcode, Relay, grey-box map | 5 players connected in a lobby |
| 2 | Character controllers, basic movement, Monster lunge | All 5 roles move and interact |
| 3 | Drawing canvas UI + Unity Sentis + Quick,Draw! model | DQS score returns for any drawing |
| 4 | Key tool end-to-end + Ink Reserve | Draw key → spawn → open door |
| 5 | Monster paint canvas (6 regions, 3 abilities) | Monster can paint themselves pre-match |
| 6 | Holding Frame catch/free loop + victory condition | Monster wins by filling all 4 Frames |
| 7 | 2 Gallery Frames + 1 Exit Gate + escape | Survivor wins by escaping |
| 8 | Polish, connect all systems, end-to-end playtest | Full match from lobby to winner screen |

---

*Built alone. Shipped smart. Every match is canon.*
