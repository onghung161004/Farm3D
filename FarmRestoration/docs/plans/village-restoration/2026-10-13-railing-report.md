# Bridge railing collision delivery

Date:2026-10-13. No commit, pricing/save-version change, terrain mutation or APK build.

## Delivered

Actual non-trigger BoxCollider side rails generated from paired rows of current PolytopeBridgeWalkway geometry (6 segments for8 vertices). Unrepaired flag keeps them inactive; repaired flag activates them, including saved repairs. Both entrances remain open. BridgeWidening refreshes guard layout after replacing the current collider mesh; 2x/3x tiers have no barrier left at former width. Same source collider/navigation references retained. No per-frame rebuild, no renderer/material replacement; generated group owned by guard and removed on teardown.

This allows stepping off an unrepaired bridge; existing gravity/water/map-boundary recovery behavior remains authoritative. No swimming/drowning mechanic added; not a jump-proof enclosure if jumping is later introduced.

## Test plan / evidence

Unity6000.6.1f1 NUnit EditMode in isolated D:/ThienAn/.codex-tmp/farm-breeding-20261012-202239. Results railing-final-20261013.xml/log. No real gameplay save writes.

| Category | Coverage | Result |
|---|---|---|
| Before/after state | Unrepaired no side hit, repaired left/right solid BoxCollider, reverse state disables | Passed |
| Width/boundaries | Sphere sweeps hit new1x/2x/3x edges at expected distances, longitudinal sweeps pass beyond both ends | Passed |
| Native movement | CharacterController.Move4m across side succeeds unrepaired, CollisionFlags.Sides and stopped position after repair | Passed |
| Format/geometry | Rotated/sloped deck, null/unpaired input, disabled deck -> no invisible walls | Passed |
| Duplicate/state integration | Ensure/refresh does not duplicate6 segments; existing bridge purchase integration asserts6 active side guards | Passed |

8 new tests passed. Full regression226 tests,225 passed,1 pre-existing FarmHudPresentationTests.ReadabilityStyle_UsesLargeTypeAndTwoLineControls failure (expects>=34, existing32). No compile errors. Physics-fixture tests are not a substitute for real map/water/device inspection.

## Code Review Report

Scope:BridgeRailingCollision.cs, VillageRestorationVisuals.cs, BridgeWidening.cs, BridgeRailingCollisionTests.cs, BridgeUpgradeTests.cs integration assertion, README/docs. Current restoration/upgrade context reused.

Verdict:GREEN for scoped implementation; no unresolved code criticals/warnings. Native side/end/movement behavior verified. Generated collision lifecycle and body/entry alignment still need normal PlayMode/device smoke checks.

| Category | Verdict | Issues |
|---|---|---|
| Security | PASS | 0; local geometry, no network or new data stores |
| Business logic | PASS | 0; authoritative repair flag, side-only collision and width refresh |
| Performance | PASS | 0; mesh/state-triggered work, reused6 colliders |
| Clean code | PASS | 0; own group cleanup, null/readability guards, disabled old segments before replacement |
| Conventions | PASS | 0; existing namespace/Ensure/save/physics patterns, metadata |

## Manual checks

Stop/restart Play FarmDemo. With unrepaired save, move sideways off bridge and observe actual water/gravity/rescue behavior. Repair -> move against left/right rails, walk along them and enter/exit ends. Upgrade2x/3x -> confirm collider/visible-edge alignment and room for cow; saved repaired/upgraded state after relaunch should retain guard placement. Inspect uneven bank corners from prior widening feature. Android requires rebuilt APK and device touch/movement smoke run. Actual speaker/audio changes from previous task unaffected.
