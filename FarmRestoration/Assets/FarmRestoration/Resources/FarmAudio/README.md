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

## Woodcutting recordings

- Source: [tree chop fall thud](https://opengameart.org/content/tree-chop-fall-thud), kheetor, CC0. Download: https://opengameart.org/sites/default/files/chop-tree-fall.ogg
- Original SHA-256: `21842DD004B46315A5D52BE80997773421D8B1D3A0D473DDBCAD388EFD93B33A`.
- `wood_hit_01.wav`: first wood impact, source 0.00-0.46 s. Used only for axe/tree interaction; hoe sound remains `chop.ogg`.
- `tree_fall.wav`: source 0.50-2.45 s, excluding the first chopping impact; retains creaking, foliage rustle and ground thud. Playback pitch 0.75 gives a slower/heavier fall, aligned with the 1.65 s animation.
- Both derived files are mono PCM16 48 kHz, averaged from source stereo, gain 0.90, 4 ms fade-in and 45 ms fade-out. Source is a published sound-effect composite, not a recording we made ourselves. No claim of microphone-field-recording provenance.
- Music remains the original generated pastoral loop; the generated fall is now only a fallback if the imported recording is absent.
