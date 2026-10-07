# Farm audio

Selected OGG effects from [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds), [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio), [Different steps on wood, stone, leaves, gravel and mud](https://lpc.opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud), and [40 CC0 water / splash / slime SFX](https://opengameart.org/content/40-cc0-water-splash-slime-sfx). All four sources are CC0. Only the clips used by this project are included.

- `footstep_grass_*`: soil and grass steps
- `footstep_wood_*`: TinyWorlds `wood01.ogg`, `wood02.ogg`, `wood03.ogg` for wooden bridge steps
- `loop_water_01.ogg`: real water recording by rubberduck; a short faded excerpt is made at runtime for watering
- `chop`: hoe; `cloth1`: sow; `dropLeather`: harvest
- `metalPot1`: processing; `handleCoins`: order delivery; `creak1`: repair

The bridge's walkable MeshCollider carries `FootstepSurface` set to Wood. The audio component on Player raycasts downward; all unmarked ground defaults to grass.
