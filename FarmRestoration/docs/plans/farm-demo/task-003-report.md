# TASK-003 Report: Player tool input and interaction targeting

## Delivered

- `Assets/FarmRestoration/Scripts/Player/PlayerToolSelection.cs`
  - Provides the pure `TryGetTool(int, out FarmTool)` shortcut mapping.
  - Maps `1..4` to `Hoe`, `Seeds`, `WateringCan`, and `Harvest`; invalid indexes return `false` and use `Hoe` only as a safe out-value.
- `Assets/FarmRestoration/Scripts/Player/PlayerToolController.cs`
  - Uses the Unity Input System `Keyboard` API.
  - Selects tools with the top-row `1`, `2`, `3`, and `4` keys.
  - Uses `E` only on `wasPressedThisFrame`, so one key press invokes a targeted plot once.
  - Finds the closest in-range `IInteractable` implemented by any `MonoBehaviour`, through a short camera-forward raycast with a short non-allocating nearby-range fallback.
  - Returns `false` without calling a plot when no target is in range.
  - Publishes `SelectedToolChanged` and `InteractionPromptChanged` events for the later HUD task.
  - Reuses fixed `RaycastHit` and `Collider` buffers; its `Update` loop creates no managed allocations.
- `Assets/FarmRestoration/Editor/FarmDemoToolControllerSetup.cs`
  - Adds/configures `PlayerToolController` only on the existing `Player` in the currently open scene.
  - Uses Unity Undo for the added/configured component, marks the current scene dirty, and never overwrites a scene or prefab.
- `Assets/FarmRestoration/Tests/EditMode/PlayerToolSelectionTests.cs`
  - Covers all four valid mappings and invalid low/high indexes.
- `Assets/FarmRestoration/Scripts/Player/InteractionTargetResolver.cs`
  - Resolves an `IInteractable` from a supplied collection of `MonoBehaviour` components without hard-coding `FarmPlot`.
- `Assets/FarmRestoration/Scripts/Player/PlayerToolInteraction.cs`
  - Owns the one-call interaction handoff: `null` target returns `false`; a valid target receives the selected tool exactly once.
- `Assets/FarmRestoration/Scripts/Player/IInteractionTargetQuery.cs`
  - Defines the injectable target-query boundary used by `PlayerToolController`; the normal runtime path remains its allocation-free physics query, while tests can supply an explicit no-target/out-of-range result.
- `Assets/FarmRestoration/Tests/EditMode/PlayerToolInteractionTests.cs`
  - Uses a concrete test-only `MonoBehaviour` implementing `IInteractable` (not a mock) to verify no-target safety, exactly-once tool forwarding, interface resolution without a `FarmPlot` type, and `PlayerToolController.TryUseTool()` behavior when its target query rejects an out-of-range candidate.

## TDD record

`PlayerToolSelectionTests.cs` was created first and referenced the intentionally absent `PlayerToolSelection` helper, producing the expected red compile failure until the helper was added. The helper was then implemented with only the required `1..4` mapping behavior.

The targeting review tests were then added first against the intentionally absent `InteractionTargetResolver` and `PlayerToolInteraction` helpers. The runtime controller was changed only after those tests described interface-based resolution, no-target safety, and one-call forwarding.

The final controller-path test was added first against the intentionally absent `IInteractionTargetQuery` boundary and `PlayerToolController.ConfigureTargetQuery`. It supplies a real interactable positioned beyond the default three-unit range; the query rejects it, and the test verifies the controller returns `false` without invoking that object.

Because the project is currently open in the Unity Editor, do not start a parallel or batch Unity process. Run the checks from the open Editor:

`Window > General > Test Runner > EditMode > Run All`

Expected result: the existing 8 tests plus 6 tool-selection cases and 4 interaction cases are green (18 total).

## Exact scene setup menu

1. Open `Assets/FarmRestoration/Scenes/FarmDemo.unity`.
2. Select `Tools > Farm Restoration > Configure FarmDemo Tools and Interaction`.
3. Press `Ctrl+S` to save the current scene.

The command selects the configured `Player`. It leaves the scene unchanged if no object named `Player` exists, reporting that setup error in the Console.

## Manual controls and check

1. Press Play with `FarmDemo` open.
2. Move near the `FarmPlot` using `WASD` or arrow keys.
3. Press `1` to choose Hoe, then `E` to till the plot.
4. Press `2` then `E` to plant seeds.
5. Press `3` then `E` to water.
6. The current plot state machine still requires its two growth advances (TASK-004 test hook/integration) before it reaches harvest-ready; once ready, press `4` then `E` to harvest.
7. Stand farther than the configured three-unit range and press `E`: the plot must not change.

For the next HUD task, subscribe to `SelectedToolChanged` for the selected tool label and `InteractionPromptChanged` for contextual action text.
