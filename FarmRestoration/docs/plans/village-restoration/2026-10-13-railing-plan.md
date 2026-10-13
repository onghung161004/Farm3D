# Railing collision implementation plan

Spec:2026-10-13-railing-spec.md. Date:2026-10-13. Owner:current agent. Status:Implemented and automated checks verified; device/GameView smoke checks pending.

## 1. Context
Railing meshes are cosmetic because original full model collider is disabled; separate deck collider supports walking but not rails. Add only side collision controlled by existing repaired flag. Non-goals: river/terrain edits, swimming/drowning, pricing/state changes, commit.
## 2. Constraints
C#/Unity native colliders; original source assets/dirty edits preserved. No per-frame geometry/scans or duplicate collider creation.
## 3. Conventions
FarmRestoration namespace, Ensure, Unity null checks, generated owned groups, NUnit isolated fixtures.
## 4. Contracts
BridgeRailingCollision.Ensure(walkway) -> same component or null for missing/unreadable/unpaired data. SetRestored(bool) -> only repaired side colliders active. Refresh -> paired current mesh rows generate two side chains; same mesh unchanged -> no rebuild. Width updates derive from current deck, not constants/current transform scaling.
## 5. Architecture
Restoration ApplyState -> current width apply -> SetRestored -> active side guards. BridgeWidening.Apply -> collider sharedMesh update -> guard.Refresh -> physics sync. Same save repair flag, same walkway object/nav references.
## 6. Artifacts
T1 new Progression/BridgeRailingCollision.cs +metadata. T2 modify VillageRestorationVisuals.cs/BridgeWidening.cs. T3 new EditMode/BridgeRailingCollisionTests.cs +metadata, README/report.
## 7. Graph/phases
Existing user requested exact before/after behavior; implement geometry T1 -> restoration/upgrade integration T2 -> verification T3. No delegation.
## 8. Tasks
T1 consumes paired deck vertices; supplies side-only colliders. Unrepaired -> disabled, repaired -> side cast hit, centre entrances -> no guard hit, repeated Ensure ->same/count unchanged. Under150LOC.
T2 consumes guards and existing repair/upgrade state; repair/reload enable, upgrading2x/3x relocates sides, no old-width invisible obstacle. No new save fields/charges. Under30LOC changed.
T3 verifies null/unpaired, original/repaired and each width, rotated/sloped deck, side sweep/collision, open ends and idempotence. Full isolated Unity regression; no real save writes.
## 9. Edge cases
Missing/unreadable/unpaired geometry:T1. Restore order/current width refresh and false state:T2. Duplicate calls, side versus entrance, transform/slopes:T1/T3.
## 10. Risks
Native physics test in EditMode not same as device controller; use native shape/cast and actual GameView manual steps. Wider bank geometry inspection remains prior feature's manual check. No promise to prevent jumping over rails if jumping is later added.
## 11. Verification
NUnit collider/state integration plus full regression. Manual fresh unrepaired -> walk off side; repair -> push along both rails; enter/exit ends; upgrade2x/3x/reload -> barriers match visible width. Android new APK. Known HUD-font failure separated.

Result:8 new tests passed, including CharacterController.Move before/after repair. Full suite226 tests,225 passed,1 known HUD-font baseline failure. See2026-10-13-railing-report.md.
