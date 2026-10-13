Started at: 2026/10/13 11:06:19
Finished at: 2026/10/13 11:31:03
Total time: 24.7 minutes (reproduction test creation through verified implementation)

# Bridge alignment fix result

Verdict: FIXED in automated native geometry/physics fixtures; actual user GameView/device confirmation pending. No commit or real gameplay-save modification.

## Root cause / scope

Hardcoded3.3m collider versus1.79418m full model width, amplified by independent widening; side guards followed incorrect collider edges. Four-row collider was also too low relative to native curved plank tops (e.g. centre plank localy~.63 versus coarse support~.50). Old tests asserted scale factors on synthetic broad walkway, not correspondence with actual scene geometry.

User approved fix. New BridgeDeckGeometry extracts upward-facing faces crossing the centreline, excludes elevated rail caps/low supports, samples actual lateral plank extents/height into20 interior rows and retains bank-centre joins. Tiny plank gaps are bridged by continuous collision; small collision offset.015m avoids feet sinking into boards. Short approach quads get same existing wood material; original plank art remains untouched. Real elevated inner rail limits provide side anchors rather than old collider width.

BridgeWidening caches corrected base plus2x/3x walking/approach/anchor tiers, retaining source mesh for cleanup. Correction applies at initial unupgraded/unrepaired setup as well as saved upgraded state. BridgeRailingCollision uses calibrated anchors with thickness offset outward and remains disabled until repair. Restoration's unrepaired pass calibrates deck then reapplies damaged visible meshes, so collision calibration does not falsely show repaired rails.

Files: Progression/BridgeDeckGeometry.cs(new), BridgeWidening.cs, BridgeRailingCollision.cs, VillageRestorationVisuals.cs; EditMode/BridgeAlignmentTests.cs(new), BridgeUpgradeTests.cs adapted to corrected base rather than incorrect raw asset; metadata, README/fix docs. No economic/save schema/terrain/imported mesh changes. Editor generator/raw baked collider remains as-is; runtime correction covers existing scenes without scene rebuild.

## Evidence

Reproduction test uses native FBX, real generated PolytopeBridgeWalkway.asset, exact FarmDemo root pose/scale and disabled body colliders matching actual scene. Before patch: ActualSceneColliderCannotSupportOutsideVisibleBridge fails at all0/1/2 tiers (3failed/3). After:9 new geometry/height/guard cases pass.

| Check | Result |
|---|---|
| Outside full visible width cannot be supported, all3 tiers | Passed |
| Height against original/widened actual plank MeshCollider,3 longitudinal positions, all3 tiers | Passed; tolerance-.015..+.04m |
| Guard sphere sweep hits solid BoxCollider near actual rail, not old1.65m half-width, all3 tiers | Passed |
| Existing exact resource/prerequisite/save/max-tier tests | Passed; no charging/version logic modified |
| Existing unrepaired/repaired CharacterController tests and open entrances | Passed |
| Existing source mesh untouched, root transform unchanged, repeated width refresh/cache | Passed |
| Full isolated regression | 236total,235passed,1known HUD-font baseline failure; includes1scratch diagnostic beyond project tests |
| Original GameView/device walkthrough | Not run; user should stop/restart Play and confirm |

Evidence: alignment-before.xml/log, alignment-final.xml/log in D:/ThienAn/.codex-tmp/farm-breeding-20261012-202239. Native visible-height tests cast along model-up to avoid mistaking narrow plank gaps for collider misalignment; lateral guard tests use world physics. Source fixture body MeshColliders explicitly disabled as in FarmDemo, eliminating stale1x body colliders from test-only FBX instantiation.

Known baseline: FarmHudPresentationTests.ReadabilityStyle_UsesLargeTypeAndTwoLineControls expects>=34, existing32. No compile errors. Test fixture/data mismatch corrections are not failed production-fix attempts: only one geometry-calibration implementation was applied.

## Code Review Report

Date:2026-10-13. Scope: six listed C# files plus direct original mesh/restoration consumers. Verdict:GREEN for scoped implementation; no unresolved code criticals/warnings. User map/device smoke checks below remain unverified.

| Category | Verdict | Issues |
|---|---|---|
| Security | PASS | 0; local geometry only, no secret/network/data authority change |
| Logic | PASS | 0; actual geometry rather than guessed width, saved level unchanged, damaged visuals retained |
| Performance | PASS | 0; calibrated once, cached tiers/anchors, no frame scans;42static side segments for this22-row calibrated skin |
| Ownership | PASS | 0; owned approach/calibration/tier meshes cleaned, original mesh assets retained |
| Conventions | PASS | 0; existing Ensure/runtime patterns, source/test metadata |

## Remaining manual checks

Stop Play, compile, restart FarmDemo. Saved3x purchase should reapply corrected skin/rails without resource change. Walk middle/edges/both bank joins, compare foot height and visible posts, lead a cow and inspect path. Fresh unrepaired save still allows stepping off sides; repaired stops lateral movement, entrances open. Inspect bank geometry and small wood joins under actual lighting. Updated APK needed for Android; no actual device test performed.

Prevention: use real native geometry + scene pose + actual baked asset in collision tests; ratio-only/synthetic fixtures are insufficient. Confirm calibration against rendered surface, not whole renderer bounds including rail/post tips.
