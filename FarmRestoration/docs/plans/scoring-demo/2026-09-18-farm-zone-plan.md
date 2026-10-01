# Implementation Plan: Third-Person Low-Poly Farm Zone

Spec Source: `docs/plans/scoring-demo/2026-09-18-farm-zone-spec.md`
Owner: Developer
Last Updated: `2026-09-18`
Status: `Approved strategy; implementation pending`

---

# 1. Context

**Problem:** `FarmDemo` has one functional plot but still looks like a test ground. It needs a compact, cohesive farm-village zone, a readable chibi player/NPC presentation, a three-step objective flow, world feedback, and an Android-ready build path.

**Affected Modules:**

- `Assets/FarmRestoration/Scenes/FarmDemo.unity` - all scene composition.
- `Scripts/Player` - camera framing and chibi presentation without altering movement/collider behavior.
- `Scripts/Farming` - preserves existing farm loop while exposing harvest to objectives.
- `Scripts/UI` - objective, pause, and completion presentation.
- `Editor` - safe, repeatable scene assembly menus.

**Non-Goals:** Multiple maps, save/load, economy, animals with AI, combat, procedural terrain, downloaded assets, multiplayer, or voice acting.

---

# 2. Constraints

**Language:** C# / Unity 6.6 URP.
**Architecture:** Existing small MonoBehaviour components plus pure helper classes tested through Unity EditMode tests. Editor setup components create/repair named scene roots safely and mark the scene dirty; they never delete the scene.

**Rules:**

- All new custom assets remain below `Assets/FarmRestoration/`.
- Use primitive low-poly meshes and URP Lit materials only.
- Keep ordinary gameplay below 250 active GameObjects; use one directional light and capped particle systems.
- Keep PlayerMovement, CharacterController, PlayerToolController, FarmPlot, and current HUD contracts backward compatible.
- Android is landscape/fullscreen and `FarmDemo` is the first enabled build scene.

---

# 3. Conventions

**Naming:** PascalCase classes/files/prefab roots; camelCase serialized fields; editor menus begin `Tools/Farm Restoration/`; generated roots use stable names (`FarmZone`, `FarmCharacters`, `QuestSystem`, `WorldLife`).

**Error Handling:** Setup commands validate active `FarmDemo` and dependencies before mutation, show an Editor dialog for wrong scene, and log a single actionable error for missing required components. Runtime invalid NPC/quest action returns false without changing progress.

**Testing:** Unity Test Framework EditMode tests below `Assets/FarmRestoration/Tests/EditMode`, inside the existing `FarmRestoration.EditModeTests` assembly. Test pure state transitions, mapping, timing, and quest transitions; scene composition gets manual smoke verification.

---

# 4. Contracts

## Enum: `QuestStep`

```text
TalkToGardener | HarvestCrop | DeliverToMerchant | Complete
```

## `QuestState`

```text
CurrentStep: QuestStep - starts TalkToGardener
TryTalk(npcRole: NpcRole, crops: int) -> bool
  - post: Gardener advances TalkToGardener -> HarvestCrop
  - post: Merchant advances DeliverToMerchant -> Complete only when crops >= 1
OnCropHarvested(amount: int) -> void
  - post: HarvestCrop -> DeliverToMerchant when amount > 0
ObjectiveChanged event -> Action<QuestStep>
```

## `NpcInteractable`

```text
TryInteract(tool: FarmTool) -> bool
  - pre: NPC has QuestState reference and player is in existing interaction range
  - post: delegates the NPC role to QuestState; tool is ignored
GetInteractionPrompt(tool) -> string
  - post: returns role/quest-sensitive prompt
```

## `DayNightController`

```text
NormalizedTime: float [0,1)
Advance(deltaSeconds: float) -> void
  - pre: deltaSeconds >= 0
  - post: wraps normalized time and evaluates light/ambient state
```

## `WeatherController`

```text
CurrentWeather: Clear | Rain
SetWeather(state) -> void
  - post: enables exactly the matching particle/audio presentation
```

---

# 5. Target Architecture

```text
PlayerToolController / FarmPlot.Harvested
  -> QuestState.OnCropHarvested()
  -> QuestState.ObjectiveChanged(step)
  -> ObjectiveHud and NpcInteractable prompts

PlayerToolController -> NpcInteractable.TryInteract()
  -> QuestState.TryTalk(role, InventoryState.HarvestedCrops)

DayNightController -> Directional Light + RenderSettings
WeatherController -> rain particle + rain AudioSource
```

Key decisions:

- Use one scene and short objective chain to show a complete narrative in a 3–5 minute assessment demo.
- Build props through repeatable editor setup menus so scene assembly stays editable and does not require third-party files.
- Keep weather cosmetic; it does not silently mutate crop state.

---

# 6. Artifact Registry

| Artifact | Type | Owner | Implements |
|---|---|---|---|
| `Editor/FarmZoneSetup.cs` | editor setup | TASK-101 | low-poly zone roots/props |
| `Editor/FarmCharacterSetup.cs` | editor setup | TASK-102 | chibi player/NPC roots |
| `Scripts/Player/ChibiCharacterVisual.cs` | class | TASK-102 | visual-only player presentation |
| `Scripts/Quests/QuestStep.cs` | enum | TASK-103 | QuestStep/NpcRole |
| `Scripts/Quests/QuestState.cs` | class | TASK-103 | quest contract |
| `Scripts/Quests/NpcInteractable.cs` | class | TASK-104 | IInteractable NPC |
| `Scripts/UI/ObjectiveHud.cs` | class | TASK-104 | objective display |
| `Editor/FarmQuestSetup.cs` | editor setup | TASK-104 | NPC/objective scene wiring |
| `Scripts/World/DayNightController.cs` | class | TASK-105 | day/night contract |
| `Scripts/World/WeatherController.cs` | class | TASK-105 | weather contract |
| `Scripts/UI/PauseMenu.cs` | class | TASK-106 | pause/restart |
| `Editor/FarmWorldLifeSetup.cs` | editor setup | TASK-105, TASK-106 | scene effects/menu wiring |
| `Editor/FarmAndroidBuildSetup.cs` | editor setup | TASK-107 | build scene/profile settings |
| `docs/plans/farm-demo/2026-09-18-smoke-test.md` | test document | TASK-107 | editor/device verification |

---

# 7. Task Graph

**User-Approved Phase/Sprint Strategy:** Vertical Slices.

| Phase | Goal | Demoable Outcome |
|---|---|---|
| 1: Farm Zone | Complete compact area and readable characters | Walk through cottage, fields, orchard, market, and use visible tools. |
| 2: Story | Two NPCs and three-step restoration quest | Start objective, farm crop, deliver, see completion. |
| 3: World Life & Delivery | Day/night, weather, VFX/audio, pause, Android | Polished 3–5 minute assessment playthrough. |

| ID | Phase | Task | Depends On |
|---|---|---|---|
| TASK-101 | 1 | Build low-poly farm zone | existing FarmDemo |
| TASK-102 | 1 | Build chibi player and NPC visuals; adjust camera framing | TASK-101 |
| TASK-103 | 2 | Quest-state domain and tests | existing InventoryState/FarmPlot |
| TASK-104 | 2 | NPC interactions, objective HUD, completion scene feedback | TASK-102, TASK-103 |
| TASK-105 | 3 | Day/night, rain, particles and ambient presentation | TASK-101 |
| TASK-106 | 3 | Pause/restart and final HUD integration | TASK-104, TASK-105 |
| TASK-107 | 3 | Android build configuration and smoke verification | TASK-106 |

```text
TASK-101 -> TASK-102 -> TASK-104 -> TASK-106 -> TASK-107
                 ^           ^
TASK-103 --------'           |
TASK-101 -> TASK-105 --------'
```

---

# 8. Task Specifications

## TASK-101: Build the compact farm-village zone

**Phase:** 1: Farm Zone
**Input:** Existing `FarmDemo` scene and materials folder.
**Output:** `FarmZone` root containing cottage, well, fences, dirt path, crop fields, orchard, stall, props; consumed by TASK-102/104/105.
**Files:** Create `Editor/FarmZoneSetup.cs`; create generated materials/prefabs under `Assets/FarmRestoration`.
**Acceptance Criteria:**

- `Configure Farm Zone()` -> creates named zone roots without deleting Player, FarmPlot, HUD, or scene.
- Re-run setup -> does not duplicate any stable zone root.
- Scene contains cottage west, active plot/vegetable beds center, stall east, orchard/wheat north, path south.
- Active object count in EditMode scene -> <= 250.

## TASK-102: Build chibi characters and camera framing

**Phase:** 1: Farm Zone
**Input:** TASK-101 zone and existing player/tool holder.
**Output:** chibi visual children for player/Gardener/Merchant and elevated third-person framing; consumed by TASK-104.
**Files:** Create `Scripts/Player/ChibiCharacterVisual.cs`, `Editor/FarmCharacterSetup.cs`, tests for visual configuration mapping.
**Acceptance Criteria:**

- `Configure Characters()` -> preserves Player CharacterController and gameplay components while hiding/replacing only primitive display mesh.
- Player chibi root -> head, hair, torso, limbs and visible right-hand tool holder.
- Gardener/Merchant roots -> readable visual distinction and world-space objective marker.
- Follow camera -> keeps player and active central field in frame while moving.

## TASK-103: Implement quest domain

**Phase:** 2: Story
**Input:** Inventory count and `FarmPlot.Harvested` event.
**Output:** `QuestState`; consumed by TASK-104.
**Files:** Create `Scripts/Quests/QuestStep.cs`, `QuestState.cs`, `Tests/EditMode/QuestStateTests.cs`.
**Acceptance Criteria:**

- `TryTalk(Gardener, 0)` at TalkToGardener -> true and step HarvestCrop.
- `OnCropHarvested(1)` at HarvestCrop -> step DeliverToMerchant.
- `TryTalk(Merchant, 0)` at DeliverToMerchant -> false and state unchanged.
- `TryTalk(Merchant, 1)` at DeliverToMerchant -> true and step Complete.

## TASK-104: Connect NPCs, objective HUD, and completion feedback

**Phase:** 2: Story
**Input:** TASK-102 character roots and TASK-103 QuestState.
**Output:** playable objective chain; consumed by TASK-106.
**Files:** Create `NpcInteractable.cs`, `ObjectiveHud.cs`, `Editor/FarmQuestSetup.cs`, focused tests for NPC prompt mapping.
**Acceptance Criteria:**

- Gardener interaction -> HUD displays `Harvest a crop`.
- Harvest event -> HUD displays `Deliver crop to Merchant`.
- Merchant with one crop -> HUD displays completion and enables restored-market visual root.
- Interacting with an NPC out of range -> existing controller returns false/no state change.

## TASK-105: Add day/night, rain, and farming presentation

**Phase:** 3: World Life & Delivery
**Input:** TASK-101 zone.
**Output:** day/night and weather presentation, consumed by TASK-106.
**Files:** Create `DayNightController.cs`, `WeatherController.cs`, `Editor/FarmWorldLifeSetup.cs`, EditMode timing tests.
**Acceptance Criteria:**

- `Advance(cycleSeconds)` -> wraps time into `[0,1)`.
- Clear -> rain state -> exactly one rain particle/audio presentation is enabled.
- Rain particle maxParticles -> <= 80.
- Full day cycle -> directional light and ambient colour visibly change without additional lights.

## TASK-106: Pause/restart and final integration pass

**Phase:** 3: World Life & Delivery
**Input:** TASK-104 quest chain and TASK-105 world-life components.
**Output:** polished assessment loop, consumed by TASK-107.
**Files:** Create `PauseMenu.cs`; modify HUD/editor setup files; add pause-state tests.
**Acceptance Criteria:**

- Escape -> `Time.timeScale == 0` and pause panel visible.
- Resume -> `Time.timeScale == 1` and panel hidden.
- Restart -> reloads `FarmDemo` at initial quest/objective state.
- 3-minute manual walkthrough -> no Console error and all objective transitions work.

## TASK-107: Configure Android build and verify

**Phase:** 3: World Life & Delivery
**Input:** TASK-106 scene.
**Output:** Android-ready profile and smoke-test record.
**Files:** Create `Editor/FarmAndroidBuildSetup.cs`; modify Unity build settings through editor API; create smoke-test document.
**Acceptance Criteria:**

- Build scene list -> `FarmDemo` first and enabled; generated SampleScene absent/disabled.
- Android profile -> landscape and fullscreen.
- Editor smoke test -> movement, quest, day/night, rain, pause/restart pass.
- Android build -> completes without SDK/NDK/JDK or missing-scene error.

---

# 9. Edge Cases

| Scenario | Expected Behavior | Handled In |
|---|---|---|
| Setup menu run twice | Named roots are reused; no duplicate cottage/NPC/HUD roots | TASK-101,102,104,105 |
| Merchant before harvest | Prompt says crop required; quest unchanged | TASK-103,104 |
| Harvest event outside HarvestCrop | Objective unchanged | TASK-103 |
| Missing light/particle/audio reference | Component logs once and disables only its presentation path | TASK-105 |
| Pause during rain/day cycle | Time-dependent effects pause and resume coherently | TASK-106 |
| Android lacks configured support | Build document directs installing Android SDK/NDK/OpenJDK | TASK-107 |

---

# 10. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Primitive-rich zone exceeds mobile object budget | Medium | Stable roots, shared materials, <=250 active objects, capped particles. |
| Editor menu updates overwrite scene work | High | Only create/repair named roots; separate explicit destructive actions are prohibited. |
| TMP/HUD API differences in Unity 6 | Medium | Reuse current Unity-6-compatible `textWrappingMode` pattern. |
| Android build setup cannot be verified while editor is open | Low | Use safe setup menu and manual build smoke checklist. |

---

# 11. Verification Plan

- EditMode: QuestState transitions, day/night wrap, weather state, pause state, visual mappings.
- Manual: create/refresh each scene setup menu once; verify it is idempotent once more; complete narrative walkthrough.
- Build: switch Android profile, build FarmDemo, launch on device, check landscape/fullscreen and Console/log output.

Success means the acceptance walkthrough in the source spec works, all available EditMode tests pass, scene stays within object/particle budgets, and no Console errors occur during the walkthrough.
