# Tree guide delivery

Status: Done.

- Axe selection shows one gold ground trail to the nearest standing active wood tree. It retains the target until felled/disabled/destroyed; stumps are excluded until regrowth.
- TMP distance/hit instructions; keyboard E and mobile ACTION retain existing controls. Other tools and fishing/dialogue/warehouse hide the guide. UI does not intercept touch.
- Arbitrary-destination pathfinding reuses the cow grid, river and bridge checks. Clear segments now sample the terrain every 0.8 m to follow slopes. Recalculation is throttled to two seconds or five metres of motion, with tree discovery once per second.
- A thin bearing plus explicit warning is shown if a walkable route cannot be found. It is not guaranteed navigation across water/obstacles. Existing route search size cap remains 80 by 80 cells.
- Runtime installation is idempotent, does not edit the scene or save schema, and cleans its generated material on destruction.

Verification: Unity 6000.6.1f1 headless EditMode, isolated project with current runtime scripts/tests and installed local package cache. 129 tests: 127 passed, 2 pre-existing failures (FarmHudPresentationTests.ReadabilityStyle_UsesLargeTypeAndTwoLineControls; FarmWeatherControllerTests.RainTransition_WatersSeededPlotOnlyOnce). All three new guide tests and the existing cow-routing/woodcutting tests pass. Runtime assembly compiled successfully.

Review verdict: GREEN for this change. No commit made. Actual FarmDemo rendered appearance and Android device interaction have not been visually tested here. Manual check: enter FarmDemo Play mode, select 8/Axe, follow the gold line, chop three times, confirm the route selects another grown tree, then change tool to hide it.
