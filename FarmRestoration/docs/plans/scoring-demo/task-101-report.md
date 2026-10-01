# TASK-101: Farm zone setup review fixes

## Scope

This follow-up addresses the review findings for the editor-generated farm zone only.

## Changes

- `FarmZoneSetup` no longer exits when an area root already has children. Re-running the menu now repairs a partial area by reusing stable named children through `GetOrCreate` behavior in `CreatePart`.
- Repeated props use deterministic child names (for example `Pumpkin_-2_5`, `AppleTree_0_Trunk`, and `PenNorth_Post_0`) so rerunning setup does not create duplicates.
- Setup validates the URP Lit shader before creating scene content. If it is unavailable, the command logs an actionable error and aborts.
- After generation, the editor counts actual descendants in every farm area, logs the per-area counts, and fails the budget check against `FarmZoneLayout.MobileObjectBudget` using the scene's actual generated count.
- A warning is emitted when the declarative `FarmZoneLayout` count differs from the generated scene count; this keeps the budget gate grounded in real output while making stale estimates visible.

## Verification

- EditMode layout tests remain applicable for area coverage and the declared budget.
- Manual verification: run `Tools > Farm Restoration > Configure Farm Zone` twice, then inspect the `FarmZone` area roots. The second run should reuse named children and should not increase the generated child count.
- Confirm the Unity Console reports per-area generated counts and no URP shader error in a URP project.
