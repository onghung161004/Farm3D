# Paid bridge widening delivery

Date: 2026-10-13. No commit, scene rebuild, real save write or APK build performed.

## Delivered

- Existing repair sign becomes UPGRADE BRIDGE after repair. E/ACTION buys the displayed next tier. Initial widening costs 40 coins + 15 wood for 2x original width; next costs 60 coins + 25 wood for 3x, then no further charge.
- Existing repair cost 45 coins + 10 wood and restoration quest repair flag unchanged. Insufficient either resource, unrepaired flag, missing/disabled/unreadable binding or max tier -> false, no charge.
- Runtime-owned LOD and walkway clones widen in the bridge-local transverse direction around the actual walkway centre (not origin; original model has an offset pivot). Root transform, longitudinal geometry/profile and bank centre endpoints are preserved. Original materials/UVs retained; transverse geometry is stretched, not replaced with a different art style.
- Same MeshCollider object updated, so cow/quest/tree guides and wooden footsteps retain their existing references. LOD bounds recalculated; repeated restoration refresh reapplies cached meshes without compounding width. Imported FBX and generated walkway both already have Read/Write enabled; no import-setting changes needed.
- Source mesh assets untouched; owned clones cleaned on teardown. TMP and older TextMesh signs both supported.
- Save8 bridgeUpgradeLevel; old saves default0, invalid levels clamp0..2, unrepaired flag forces0. Load/refresh never purchases or deducts resources.

## Test strategy / evidence

Unity 6000.6.1f1 + NUnit EditMode in D:/ThienAn/.codex-tmp/farm-breeding-20261012-202239; fixture Changed handlers disable disk writes before SaveGame. Native imported bridge LOD meshes exercised, plus a baked-space upward-facing walkway fixture with transformed/sloped model.

| Category | Cases | Result |
|---|---|---|
| Resource boundaries | 39/40 coins, 14/15 wood; second tier 59/60 coins, 24/25 wood | Passed |
| Prerequisite/null/state | Unrepaired, null binding, disabled collider, terminal third purchase | Passed |
| Full purchase/sign/save | Two actions cost exactly100 coins/40 wood, next/max prompt, snapshot8 | Passed |
| Native geometry | 2x/3x lateral vertices, fixed longitudinal/vertical local geometry, unchanged root, expanded-edge collider raycasts | Passed |
| Repeated refresh | Cached visual/collider objects reused, no cumulative scale or asset mutation | Passed |
| Migration/boundaries | Negative/oversized levels, no repair flag, legacy save7 | Passed |
| Format/lifecycle | Unicode fixture name, rotated/nonuniform bridge transform, invalid Apply, cleanup restores sources | Passed |

16 new tests passed. Full suite: 204 total, 203 passed, one pre-existing FarmHudPresentationTests.ReadabilityStyle_UsesLargeTypeAndTwoLineControls failure (expects >=34, actual32). No compiler errors. Final results bridge-final-20261013.xml/log in the isolated project.

Test fixture corrections during investigation: native centre is offset, so verify widening relative to walkway centre rather than zero; upward-facing fixture winding required for front-face collider raycasts. Production widening preserves the validated native transform basis and source topology.

## Code Review Report

Scope: BridgeWidening.cs, FarmProgression.Bridge.cs, FarmProgression.cs, FarmSaveData.cs, VillageRestorationVisuals.cs, VillageRepairPoint.cs, BridgeUpgradeTests.cs, FarmQuestTests.cs version assertion, README and feature artifacts. Prior task context reused.

Verdict: GREEN for scoped code, automated checks complete. No unresolved critical issues or warnings in reviewed code; device/visual checks below remain unverified.

| Category | Verdict | Issues |
|---|---|---|
| Security | PASS | 0; local shared inventory, no network/secrets |
| Business logic | PASS | 0; costs/prerequisite/max/legacy flags and no load charges verified |
| Performance | PASS | 0; one clone set per tier, no per-frame allocation/scans, collider sync only on mesh change |
| Clean code | PASS | 0; visual/economy responsibilities separated, source ownership cleanup |
| Conventions | PASS | 0; existing namespace/Ensure/false/save/TMP patterns and metadata |

## Manual checks still needed

Stop Play, allow compile, Play FarmDemo. Repair the bridge if needed; inspect UPGRADE BRIDGE sign and buy both tiers. Walk and lead a cow across middle/outer portions, confirm path alignment and wood footsteps. Reload and verify tier, prices/max status, unchanged money and wood. Outer corners of a wider deck can meet different terrain/foliage than the old narrow bridge: inspect these in the actual GameView; terrain/trees were not modified automatically. Also verify sign multiline readability.

Android: build new APK and verify ACTION at sign, native geometry/collision and reload on device. No device test or pixel-perfect bank-corner alignment claim is made.
