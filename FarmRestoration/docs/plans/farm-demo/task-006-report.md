# TASK-006: visual feedback and integration

## Added

- `PlayerToolVisual` displays a primitive low-poly tool in a `ToolHolder` on the Player.
- Keys `1` through `4` now visibly swap Hoe, Seeds, Watering Can, and Harvest tools.
- A successful `E` interaction raises `ToolUsedSuccessfully`; the held tool makes a short downward swing/bob.
- `G` is an explicitly temporary demo-growth control: when the nearby plot is `Watered`, press `G` once for `Growing` and a second time for `ReadyToHarvest`. It does nothing for other states or non-plot targets.
- The safe editor polish commands create a plot border and make every plot state use a strongly different colour/shape.

## Unity setup

With `FarmDemo.unity` open and outside Play mode, run in this order:

1. `Tools > Farm Restoration > Configure FarmDemo Tools and Interaction` (only if not already done).
2. `Tools > Farm Restoration > Add Player Tool Visuals (Safe)`.
3. `Tools > Farm Restoration > Polish Farm Plot Visuals (Safe)`.
4. Save with `Ctrl+S`.

The player-tool command repairs a partially configured `ToolHolder` by adding only the missing tool roots; it also re-enables and subscribes an existing `PlayerToolVisual`. The plot command leaves an existing polish root unchanged. Created objects and renderer material assignments are registered with Undo; neither command overwrites the scene or prefab.

## Manual demo controls

- `WASD` / arrows: move.
- `1`: Hoe; `2`: Seeds; `3`: Watering Can; `4`: Harvest.
- `E`: use the selected tool on the nearby plot. A valid use makes the held tool bob.
- `G`: labelled temporary demo growth advance after watering.

Full loop: move to plot -> `1`, `E` -> `2`, `E` -> `3`, `E` -> `G` -> `G` -> `4`, `E`.

## TDD evidence

`PlayerToolVisualStateTests` was added before `PlayerToolVisualState` and `FarmGrowthDemo`. It covers all four visible-tool name mappings, visible mid-swing bob offset, and the watered-to-growing `G` demo transition.

It also covers setup-after-component-creation, ensuring the safe menu's late configuration leaves `PlayerToolVisual` enabled and configured rather than permanently disabling it when Unity invokes `Awake` first.
