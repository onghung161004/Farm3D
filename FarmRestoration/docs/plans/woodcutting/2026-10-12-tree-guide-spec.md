# Tree guide

Show one gold route when the player selects Axe (keyboard 8 or touch hotbar). Select the nearest active, standing ChoppableTree by horizontal distance; retain it until it is felled or unavailable, then select the next. Stumps are excluded until regrowth. No save changes.

Reuse the cow ground pathfinder with an arbitrary destination 1.8 m from the trunk. Sample terrain, avoid river water, use the bridge when a route can be found. Cache paths for two seconds or five metres of player motion. When no ground route is found, show a thin bearing line with an explicit warning, not a claimed walkable route.

Use non-intercepting TMP instructions below the cow guide. Hide while using other tools, fishing or dialogue/warehouse UI. Preserve all existing cow-guide behaviour and input mappings.
