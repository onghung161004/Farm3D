# Rain watering

Confirmed cause: FarmWeatherController.Advance only called WaterSeededPlots when weather changed to Rain. Crops planted after that transition were not visited during the current rainy phase.

User-approved correction: recheck the cached plot list on each valid Advance while already raining, using existing TryWaterFromRain. Only unlocked, active Seeded plots can enter Watered; established growing crops retain their UTC deadline. Untilled/Tilled plots remain unchanged; no persisted wet-soil state is introduced.

Also correct the related phase-boundary rounding failure: compare transition times with a one-microsecond tolerance and clamp remaining duration to zero. The existing 1.9f + 0.1f test now represents the intended two-second boundary without delaying rain by one frame.

Scope: FarmWeatherController.cs and its EditMode tests. No save schema, economy, scene, visual or audio changes. Constant-time per cached plot, no per-frame scene discovery or allocation. Missing list remains safe.

Tests: plant during rain for all three crop types; water exactly once and preserve deadlines across rain ticks; planting after rain ends remains dry; inactive plot becomes eligible when enabled; existing rain transition, invalid/long delta and empty-list tests. Device smoke check: build new APK, start rain, till/plant a fresh plot, confirm wet/growing state without watering can.

Verification: Unity 6000.6.1f1 isolated headless EditMode run: 149 total, 148 passed. All eight weather test cases pass; the only remaining pre-existing failure is FarmHudPresentationTests.ReadabilityStyle_UsesLargeTypeAndTwoLineControls (32 vs expected >=34). Runtime compilation and git diff --check passed. No device/GameView visual verification or commit performed.

Code-reviewer verdict GREEN: only valid advances water the already-raining phase, repeated visits cannot reset growth because FarmPlot only accepts Seeded, existing locked/inactive checks remain, transitions and long-delta bounding remain unchanged except microsecond float tolerance. No new dependencies or per-frame allocation.
