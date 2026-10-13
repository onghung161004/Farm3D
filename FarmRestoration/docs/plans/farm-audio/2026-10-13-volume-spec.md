# Audio controls

User approved AUDIO HUD button -> popup, Background music and SFX/actions sliders0..100%, independent mute buttons and mute-all. Changes apply immediately, preserve chosen levels when unmuted, persist per device with a separate PlayerPrefs key (no gameplay save version change).

Reuse current sources/clip/pitch/spatial falloff and mix: slider100% means original source balance, not amplified music. SFX covers footsteps, farming/crafting/repair actions, fishing cast/bite/results, cows and tree fall. A lightweight source channel multiplies base volume by group gain; music fade/ducking updates base volume separately so it cannot overwrite user settings.

Dark-green/gold TMP popup with large touch sliders, Close/Esc. Opening owns only its movement lock, blocks ACTION/CAST/hotbar/recipe and mobile look, does not reset quests or timescale. Avoid overlapping other modal/fishing sessions. Muting sound does not disable Android haptic feedback.

Bound saved values and non-finite floats; preserve valid level when muted or all-muted. Save on close/app pause/quit rather than flushing preferences on every drag. Scene/source teardown unsubscribes events. Device visual and audible output require manual smoke tests.
