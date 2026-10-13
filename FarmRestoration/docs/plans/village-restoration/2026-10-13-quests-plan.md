# Quest implementation plan

Spec: 2026-10-13-quests-spec.md. Owner: current agent. Status: Implemented; automated verification complete, device/visual smoke checks pending.

## 1. Context
Connect existing activities into the proposed restoration chain. Shared money/stock and existing visible bridge/house restoration remain authoritative. Non-goals: new chief NPC, new map, rare fish/tool upgrades or changed repair prices.

## 2. Constraints
C#/Unity/TMP/InputSystem/NUnit. No external dependencies. Event-driven quest updates, route updates throttled to two seconds, cached scene targets. Preserve dirty user changes; no Git commit.

## 3. Conventions
FarmRestoration namespace, existing static UI Ensure pattern and dark-green/gold wood style. Clamp invalid persisted counters; safe null guards. NUnit isolated fixtures, no real save writes in tests.

## 4. Contracts
FarmQuestProgress: snapshot/restore isolated values and two distinct IDs; saturating Record methods; claimedStage 0..6.
FarmQuestRules: Titles/rewards/CanClaim/Objectives from progress plus read-only live context (orders, adult penned cows, repair flags).
FarmProgression quest adapter: successful harvest/produce/feed/milk/fish/talk record; explicit GiveFish consumes one only during stage 4 up to two; TryClaim checks readiness and coin space, advances and saves once.
FarmQuestView: Ensure, Show/Close, IsOpen, IsTracking; owns one modal and optional movement lock.
FarmQuestBoard/Navigation: idempotent wood board at well, final decoration by claimedStage=6; tracked target appropriate to unmet goal, no movement of existing structures.

## 5. Architecture
Existing successful action -> bounded progress -> same NotifyAndSave -> UI/board refresh. Quest Claim -> rule eligibility -> shared coins -> stage advance -> same save. Tracking -> existing FindTo pathfinder -> line only, no automatic player movement.

## 6. Artifacts
TASK-001: Scripts/Quests/FarmQuestProgress.cs and FarmQuestRules.cs.
TASK-002: Progression/FarmProgression.Quests.cs, FarmProgression.cs, FarmSaveData.cs; resident/trader talk hooks.
TASK-003: Quests/FarmQuestView.cs and QuestUiFactory.cs.
TASK-004: Quests/FarmQuestBoard.cs and FarmQuestNavigation.cs; modal guards in player/mobile/fishing/tree guide.
TASK-005: EditMode quest state/integration tests, README and delivery report. Include metadata for new files/folder.

## 7. Task graph / phases
Use end-to-end implementation under user's continued request and prior instruction not to repeatedly ask. Phase A domain/save, Phase B board/HUD/navigation, Phase C verification. TASK-001 -> TASK-002 -> TASK-003 -> TASK-004 -> TASK-005. Alternative standalone parallel inventory/quest economy rejected because it duplicates state; purely narrative labels rejected because they do not track actions. No delegation.

## 8. Acceptance criteria
A: each goal increments only after successful action; milk stock sale doesn't erase collected goal; duplicate cow feed doesn't count a second cow; restore copied IDs not shared. Claim unmet stage -> false/no coins; same reward cannot be claimed twice.
B: mobile buttons and Q open same modal; modal blocks ACTION/CAST/recipe and closes cleanly; no pointer interception by labels/trail. Tracking opt-in and hides behind existing cow/tree guides. Existing repaired bridge/house never charged again.
C: domain + integration tests cover all six claims, explicit fish delivery, legacy minimum-progress migration, save round-trip, invalid counters and UI lifecycle. Compile full runtime and regression suite in isolated Unity project.

## 9. Edge cases
Null/negative/oversized save counters and duplicate/Unicode IDs (001); double click, coin overflow, insufficient fish and failed actions (002); other modal/fishing active and destroyed player (003/004); no route/structure and final stage after reload (004); existing saves without historical consumed-stock information (002).

## 10. Risks
Old saves lack lifetime counters: credit known stock/orders/feeding state only and clearly document limits. Runtime world sign near well must be visually checked against NPC paths. Android aspect ratio requires real-device UI inspection. Reward tuning is an initial implementation value, not playtest-proven balance.

## 11. Verification
NUnit pure rule tests plus game adapter/economy tests without disk writes; installed board/UI idempotence and lifecycle. Manual fresh save -> six stages -> final screen -> reopen save; existing save -> credited repairs; Android tap/hold/input locking. Keep baseline HUD-font failure separate from regressions.

Result: 31 new quest tests passed. Full isolated Unity EditMode suite: 188 total, 187 passed, 1 existing HUD-font assertion failed (expects >=34, current value 32). No compile errors. See 2026-10-13-quests-report.md for delivery/review evidence and remaining manual checks.
