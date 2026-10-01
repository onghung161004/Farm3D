# TASK-005: Inventory and farming HUD

## Delivered

- `Assets/FarmRestoration/Scripts/UI/InventoryState.cs` owns the non-negative harvested-crop count and emits a count-change event only after a positive addition.
- `Assets/FarmRestoration/Scripts/UI/FarmHud.cs` subscribes and unsubscribes safely to tool, prompt, harvest, and inventory events.
- `Assets/FarmRestoration/Scripts/UI/FarmHudPresentation.cs` is the small, framework-free text formatter used by the HUD.
- `Assets/FarmRestoration/Editor/FarmDemoHudSetup.cs` is Unity-importable and adds the HUD to the active FarmDemo scene without deleting scene objects.
- `Assets/FarmRestoration/Tests/EditMode/InventoryStateTests.cs` covers positive/zero inventory behavior and their event behavior.
- `Assets/FarmRestoration/Tests/EditMode/FarmHudPresentationTests.cs` covers readable watering-can, prompt, and crop-count text.

## TDD and verification

The inventory tests were created before the original inventory implementation. The additional presentation tests describe the expected UI text without depending on TextMeshPro or scene objects. Run:

`Window > General > Test Runner > EditMode > Run All`

after Unity finishes importing. The expected suite contains the existing tests plus four inventory and three HUD-presentation tests.

## Safe HUD setup and manual check

1. Open `Assets/FarmRestoration/Scenes/FarmDemo.unity`.
2. Run `Tools > Farm Restoration > Configure FarmDemo HUD`.
3. Save with `Ctrl+S`.
4. Enter Play Mode. A dark, top-left panel must show the selected tool, contextual action, crop counter, and keyboard controls.
5. Press `3`; the panel must show `Tool: Watering Can`. Move close enough to the plot to see its contextual prompt. Complete a harvest; the panel must update to `Crops: 1`.

The menu validates that the active scene is FarmDemo and that it contains both a `PlayerToolController` and a `FarmPlot`; on a failed prerequisite it leaves the scene unchanged.

## Robustness follow-up

- If an existing HUD panel contains a named Tool, Prompt, or Crops child without `TextMeshProUGUI`, setup adds the missing component before configuring it; no null reference is produced.
- Presentation tests now cover all supported tool names, null/empty prompts, and a defensive negative crop display clamp (`Crops: 0`). `InventoryState` itself never permits a negative count.
- Unity 6 uses `TMP_Text.textWrappingMode`; the HUD labels explicitly use `TextWrappingModes.NoWrap` instead of the obsolete `enableWordWrapping` property.

## HUD readability polish (2026-09-18)

- `FarmHudReadabilityStyle` defines testable minimum readable sizes: Tool 56pt, Action 40pt, Crops 40pt, and Controls 28pt.
- The HUD setup now reapplies its layout every time it runs: a 1040×350 opaque charcoal panel, bold high-contrast status labels, black TMP outlines, and two concise control lines.
- Re-running the menu updates the existing `FarmHud`/`Panel`/label objects instead of creating duplicates. The changes are registered with Unity Undo and the scene is marked dirty for an explicit save.

### Reconfigure after importing

1. Open `Assets/FarmRestoration/Scenes/FarmDemo.unity`.
2. Wait for Unity to finish compiling, then run `Tools > Farm Restoration > Configure FarmDemo HUD`.
3. Press `Ctrl+S`, enter Play Mode, and confirm the upper-left HUD is legible at the Game view's current scale.
4. If the previous layout was saved, run the same menu command once; it safely restyles that existing HUD in place.

Run `Window > General > Test Runner > EditMode > Run All`. The readability-policy test verifies the four font-size thresholds and the required two-line controls hint.
