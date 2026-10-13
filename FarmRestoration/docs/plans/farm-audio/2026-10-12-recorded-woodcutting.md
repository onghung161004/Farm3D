# Recorded woodcutting correction

User rejected the generic hoe/chop hit and synthesized tree fall. Reuse the existing source routing, but add separate imported wood/tree clips sourced from kheetor's CC0 tree chop fall thud (see Resources/FarmAudio/README.md for link, hash and processing).

Implementation: axe/tree interactions select wood_hit_01 while hoe remains unchanged. TreeFallAudio loads tree_fall recording rather than synthesizing it, with pitch 0.75. Recorded impact at source ~1.7 s, trimmed start 0.5 s, becomes ~1.6 s of playback; TreeHarvestVisual duration changes from 1.1 to 1.65 s. Final-hit trigger, cooldown, regrowth, wood awards and spatial rolloff unchanged. Music untouched.

Clip preparation: Unity decoded source OGG (2.670292 s, stereo 48 kHz); prepared mono PCM16 WAV clips using sample averaging, 0.90 gain, short endpoint fades, ranges 0-0.46 and 0.50-2.45 s. Processing helper is retained in the isolated project's AudioPreparation.cs; no source download or export utility runs in the shipped game.

Verification: imported duration/channel checks, correct recorded fall selection/pitch, synthesized fallback timing follows updated animation constant, and existing woodcutting/fishing regressions. Speaker/headphone listening and Android APK validation remain manual; cannot certify subjective realism from automated waveform checks.

Result: Unity 6000.6.1f1 headless EditMode, isolated project: 153 tests, 152 passed; all four soundtrack tests and existing woodcutting/fishing tests pass. Only the pre-existing HUD font-size expectation fails. Audio imports and runtime compilation succeeded. git diff --check passed. No APK build, manual audition or commit performed.

Code-reviewer verdict GREEN: source license/hash and derivative edits documented, no generated chirp used when the shipped fall resource is available, wood sound changes only tree actions, recorded fall duration and pitch align with the animation, assets are small mono WAVs (44,204 and 187,244 bytes), ordinary hoe/music and game progression unchanged.
