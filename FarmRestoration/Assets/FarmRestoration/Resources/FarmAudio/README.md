# Farm audio

Selected OGG effects from [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds), [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio), [Different steps on wood, stone, leaves, gravel and mud](https://lpc.opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud), and [40 CC0 water / splash / slime SFX](https://opengameart.org/content/40-cc0-water-splash-slime-sfx). These sources and the cow recording listed below are CC0. Only the clips used by this project are included.

- `footstep_grass_*`: soil and grass steps
- `footstep_wood_*`: TinyWorlds `wood01.ogg`, `wood02.ogg`, `wood03.ogg` for wooden bridge steps
- `loop_water_01.ogg`: real water recording by rubberduck; a short faded excerpt is made at runtime for watering
- `chop`: hoe; `cloth1`: sow; `dropLeather`: harvest
- `metalPot1`: processing; `handleCoins`: order delivery; `creak1`: repair
- `splash_09.ogg`, `splash_10.ogg`: short fishing-float splashes from [rubberduck's water/splash pack](https://opengameart.org/content/40-cc0-water-splash-slime-sfx) (CC0). One plays when the float lands in the pond or river.
- `cow_moo.mp3`: a real 3-second [cow moo recording by Joseph SARDIN](https://bigsoundbank.com/cow-moos-6-s2386.html) (CC0). Wild cows moo roughly every 14–17 seconds while the player is within 15 m; feeding and other cow interactions can also trigger spatial mooing.

The bridge's walkable MeshCollider carries `FootstepSurface` set to Wood. The audio component on Player raycasts downward; all unmarked ground defaults to grass.
