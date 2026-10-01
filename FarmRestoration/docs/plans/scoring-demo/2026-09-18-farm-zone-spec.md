# Third-Person Low-Poly Farm Zone

## Purpose

Replace the blank `FarmDemo` ground with a compact, readable low-poly farm-village zone inspired by the submitted visual reference while retaining the current third-person follow camera and interaction system.

## Camera and player

- Keep `PlayerFollowCamera` behind the player.
- Raise and pull back the follow offset enough to show the active farming area, house, and nearest NPC while preserving clear view of the player-held tool.
- Replace the capsule presentation with a chibi low-poly player assembled from primitive head, hair, torso, limbs, and simple clothing; retain the existing CharacterController and gameplay components.
- Keep selected tool models attached at the visible right-hand location.

## Zone layout

```text
North:  trees, rocks, apple orchard, wheat field
West:   cottage, well, Gardener NPC, fenced animal-decoration pen
Center: pumpkin/corn field, active farm plot, vegetable beds
East:   Merchant NPC, market stall, wooden crates and delivery marker
South:  dirt entrance road, hay bales, fence gate
```

The player spawns on the south-center dirt path, faces toward the cottage and active field, and can reach the active farm plot and both NPCs without loading another scene.

## Art direction

- Warm stylized low-poly palette: saturated grass, dark brown soil, warm wood, pale cottage walls, yellow crops, orange pumpkins, red apples, blue water/rain accents.
- All world props are primitive-based prefabs with URP Lit materials created within the project; no copied third-party game assets.
- Use fences, crop rows, stone clusters, flowers, grass tufts, and a dirt path to break up the ground plane and establish depth.
- Reuse materials and simple meshes; generate no more than 250 active objects in the ordinary demo scene.

## NPCs and quests

- Gardener chibi NPC near the cottage: objective 1, explain farm restoration.
- Merchant chibi NPC at the stall: objective 3, accept one harvested crop.
- Objective sequence: speak to Gardener → harvest one crop → deliver crop to Merchant → show a restored-market completion effect.
- NPC interaction uses the existing in-range interaction pattern and receives a clear objective marker.

## World life and feedback

- Short day/night loop shifts main light colour/intensity and ambient sky colour.
- Clear/rain weather state: rain uses capped particles and a low-volume loop; weather visual appears only during its active state.
- Farming events show small particles and sound cues; harvest shows a pop/collection effect.
- Existing HUD gains objective text; pause/restart can be accessed during play.

## Android quality bar

- One directional light; keep shadows modest and limit particle count.
- Landscape fullscreen build; `FarmDemo` is the first enabled build scene.
- Verify movement, all farming states, two NPC interactions, menu, and no Console errors in editor before Android build testing.

## Acceptance walkthrough

1. Start at the south dirt path and see the cottage, fields, market stall, player character, and first objective.
2. Walk to Gardener; objective advances.
3. Use visible hand tools to farm one crop, with growth/time/weather feedback.
4. Deliver crop to Merchant; market restoration/completion feedback appears.
5. Pause/restart and Android build path work without errors.
