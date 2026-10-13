# Implementation Plan: bridge widening

Spec Source: 2026-10-13-bridge-upgrade-spec.md. Owner: current agent. Date: 2026-10-13. Status: Implemented and automated tests verified; manual GameView/device checks pending.

## 1. Context
Paid visible widening connects current wood/coin economy to easier cow/player crossing. Affects actual bridge visual/collider, repair sign, progression and JSON save. Non-goals: new bridge model, altered river/terrain, replacement quests, commit/build APK.

## 2. Constraints
C#/Unity 6.6/TMP/NUnit; existing runtime Ensure and shared-save architecture. Preserve dirty changes and original mesh assets. Mesh allocation only once per tier, no per-frame scans/geometry work; no new dependencies.

## 3. Conventions
FarmRestoration namespace, public PascalCase APIs, false for invalid actions, null/readability validation before charging. Native Unity object null checks and owned mesh cleanup. Isolated EditMode fixtures with disk writes suppressed.

## 4. Contracts
BridgeWidening.Install(root,walkway,filters,originals) -> one initialized component or null if missing/unreadable/mismatched arrays; no charge. CanUpgrade -> valid active geometry. Apply(level 0..2) -> false invalid binding/level; success assigns cached cloned visual/collider geometry measured from original source, never multiplies current width. Level 0 restores originals.
FarmProgression.TryUpgradeBridge(BridgeWidening) -> false unless ready, repair1 true, level<2, sufficient currency/wood, valid active bridge. On success apply next tier before subtracting 40/15 or 60/25, increment level, NotifyAndSave; no exception expected on validated normal geometry. BridgeUpgradeLevel is effective 0..2. RestoreBridgeUpgradeLevel(saved,repaired) clamps and resets unrepaired case.

## 5. Architecture
Repair sign -> original repair when unrepaired / TryUpgradeBridge when repaired -> validated BridgeWidening.Apply -> shared resources/level -> Changed refresh sign/restoration -> save8. Restoration ApplyState -> base meshes -> cached widening, preserving legacy repairs and pathfinder's collider reference.

## 6. Artifacts
T1: new Scripts/Progression/BridgeWidening.cs and metadata; modify VillageRestorationVisuals.cs.
T2: new FarmProgression.Bridge.cs and metadata; modify FarmProgression.cs/FarmSaveData.cs.
T3: modify VillageRepairPoint.cs; next-cost/max-level TMP labels and action dispatch.
T4: new Tests/EditMode/BridgeUpgradeTests.cs and metadata, native geometry/cleanup/integration tests; update FarmQuestTests version expectation, README and delivery report.

## 7. Task graph / phases
User approved end-to-end implementation, prior instruction no repeated questions. Geometry/risk first (T1: prove separate walkway deformation), then economy/save and sign (T2->T3), verification (T4). T1 -> T2 -> T3 -> T4, no delegation. Economy-first would risk charging for broken visuals; whole-transform scaling would incorrectly alter attached signs/debris and neglect sibling collider.

## 8. Task specifications
T1 consumes existing repaired LOD meshes and sibling collider, supplies stable widening API to T2/T3. Native 2x/3x width, fixed centre/length and source assets, repeated Apply -> no compounding; restoration refresh -> same expanded meshes. Under 200 runtime LOC.
T2 consumes T1, supplies current level and purchase to T3. TryUpgradeBridge at 40coin/15wood -> exact first cost; 39coin or14wood -> false unchanged; unrepaired or invalid visual -> false; third purchase -> false. Snapshot includes level, restore old save ->0/no charge. Under 100 LOC plus bounded main-file edits.
T3 consumes T2/T1, supplies E/ACTION/sign prompts. Unrepaired -> original repair; repaired -> visible next upgrade price/owned resources; max ->3x status. Same repair flag still satisfies quests. Under 100 changed LOC.
T4 tests all contracts and legacy interactions in isolated Unity, validates native collider raycasts at expanded edges, updates docs. No real player save writes.

## 9. Edge cases
Missing meshes/collider, unreadable data, transformed parent and Unicode names: T1. Tier clamp, old save, unrepaired flag, cost boundaries, double submissions and final tier: T2. Repeated visual refresh and sign after load: T1/T3. Source/owned-mesh cleanup, regression baseline and device limits: T4.

## 10. Risks
Visible/collision axes differ because walkway vertices are baked in parent space: transform via bridge-local basis (T1, native rotated tests). Original restoration resets mesh on Changed: explicitly reapply cached widening (T1). Wide outer bank corners can intersect uneven terrain/foliage: preserve centre endpoints/longitudinal profile and require manual GameView inspection; no silent terrain mutation. Imported meshes may be unreadable: block payment.

## 11. Verification
NUnit economy/state/save and native mesh/collider tests; full runtime compile/regression in isolated project. Manual Play FarmDemo: repair, two upgrades, walk across middle and outer edges with cow, reload and compare width/resources/sign. Android requires new APK and device smoke test. Document pre-existing HUD font test failure separately.

Results: 16 new bridge tests passed. Full suite: 204 total, 203 passed, one known HUD-font assertion failure (existing 32 versus expected >=34). No compile errors. Delivery/review: 2026-10-13-bridge-upgrade-report.md.
