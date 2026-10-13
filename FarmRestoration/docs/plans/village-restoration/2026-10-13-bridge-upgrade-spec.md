# Paid bridge widening

Approved by user: implement both sequential upgrades, original -> 2x width for 40 coins + 15 wood, then -> 3x original width for 60 coins + 25 wood. Existing bridge repair prerequisite and price remain unchanged. Interaction through existing repair sign E/ACTION; current next price visible before pressing. No separate currency, no automatic charge during load, no commit.

Retain existing wooden materials, actual bridge root position/rotation/scale, length and centreline, bank centre endpoints. Widen original LOD meshes and actual PolytopeBridgeWalkway collider in the bridge's transverse local Z direction, using runtime-owned clones only. Keep the same walkway component so cow/tree/quest navigation and wood footsteps immediately use expanded surface. Restore original meshes on component teardown. Missing/non-readable/mismatched geometry blocks payment.

Save version 8 adds bridgeUpgradeLevel (0..2); legacy saves default to 0, invalid values clamped, unrepaired bridges cannot have an effective upgrade. Restored visual refresh must reapply cached widened meshes rather than erase upgrades or multiply width cumulatively. Maxed sign reports 3x and stops charges. Double click cannot buy same tier twice; each E/ACTION is an explicit next-tier purchase. Insufficient coins OR wood leaves all state/meshes unchanged.

Tests: exact cost boundaries, repair prerequisite, terminal tier, save migration, failed binding, native original LOD models, rotation/nonuniform scale, doubled/tripled mesh and collider width, unchanged centre/length, repeated refresh and cleanup. Device/GameView bank-edge alignment remains a manual check; no claim of Android testing.
