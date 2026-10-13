# Audio volume controls delivery

Date:2026-10-13. No commit, live gameplay-save modification or APK build.

## Implemented

HUD AUDIO button below the economy panel opens dark-green/gold TMP popup: separate music/SFX sliders0..100%, per-group Mute/Unmute, global Mute all/Unmute all, Close/Esc. Touch handles40 high with slider48 high; button respects right/top safe-area insets and popup scales down to canvas dimensions. Overlay blocks pointer click-through; direct keyboard/mobile gameplay entry points and other modal/fishing opening are guarded. Own movement lock only, no Time.timeScale change.

AudioVolumeState normalizes/clones settings; FarmAudioSettings publishes changes and saves separate device preference FarmRestoration.Audio.v1 on close/pause/quit, not each drag. Missing fields preserve default1, corrupt/non-finite/out-of-range values recover safely. Unmuting preserves slider values and other group's mute flag. Gameplay JSON version remains8 and untouched by audio settings.

All current runtime AudioSources registered with FarmAudioChannel: background music, footstep/actions, cow, fishing feedback, tree fall. Base/source mix retained: slider1 corresponds to old levels. Music fades/ducking and footsteps update channel base, never overwrite group gain. Existing one-shot gain, clip, pitch, spatial falloff, herd timers, fish haptic feedback and gameplay events unchanged. New sources inherit settings before first sound; disabling/destroying channels unsubscribes callbacks.

## Test plan / result

NUnit EditMode isolated Unity6000.6.1f1 project D:/ThienAn/.codex-tmp/farm-breeding-20261012-202239. Per-test unique preference key; real user key restored, no gameplay saves written.

| Category | Coverage | Result |
|---|---|---|
| Boundaries/null/format | null state, -1/0/.5/1/100, NaN/Infinity, corrupt and partial JSON, Unicode source name | Passed |
| State/repeated input | Independent gains, group/global mute retains raw levels, idempotent Bind, snapshot copy | Passed |
| Source integration | Real music/tree/footstep/action/cow/fishing sources receive correct group gain; muted new source, base fade while muted, unsubscribe/re-enable | Passed |
| Persistence | No writes per slider move; Save/Reload roundtrip, bad preference fallback | Passed |
| UI | Ensure once, live slider change without recursive notification, direct hotbar/ACTION/recipe blocking, own/pre-existing lock on close | Passed |

14 new tests passed; full regression218 tests,217 passed,1 existing FarmHudPresentationTests.ReadabilityStyle_UsesLargeTypeAndTwoLineControls failure (expects >=34, current32). No compiler errors. Evidence volume-final-20261013.xml/log in isolated project. Headless tests verify source gain/state, not actual speaker/headphone output. EditMode cleanup handlers explicitly invoked where Unity does not dispatch normal PlayMode callbacks.

## Code Review Report

Scope:3 Audio scripts,2 new UI scripts,5 audio adapters, FarmHud, PlayerToolController/MobileGameControls/FishingPondSpot/WoodcuttingGuide/FarmQuestNavigation/FarmQuestView modal guards, FarmAudioSettingsTests and docs. Context reused from approved feature spec/plan.

Verdict:GREEN for reviewed implementation; no unresolved code criticals/warnings. Manual device/visual/audio checks remain outstanding and are not claimed verified.

| Category | Verdict | Issues |
|---|---|---|
| Security | PASS | 0; local preferences, no secrets/network/new dependencies |
| Business logic | PASS | 0; independent/global gains, live updates, default/migration behavior and retained levels |
| Performance | PASS | 0; event-driven source updates, no per-frame discovery/persistence writes |
| Clean code | PASS | 0; separated settings/channel/view/factory, subscription cleanup and own-lock lifecycle |
| Conventions | PASS | 0; existing namespace/TMP/Ensure patterns, matching darkgreen/gold UI and metadata |

## Manual smoke checks

1. Restart Play FarmDemo, find AUDIO at right below economy summary; inspect text/button placement and overlay. Open/close while holding movement/ACTION, verify no input leaks and normal controls afterward.
2. Music0/50/100, SFX0/50/100, individual/global mute. Verify audible mix using real speakers/headphones; 100% means original quiet music mix, not full-scale source loudness.
3. Hear footsteps, farm actions, wild/led cows, cast/bite/results and tree fall; adjust volume during an existing sound where possible. Muted effects still allow game operations and Android bite vibration. Popup opening during active fishing is deliberately rejected; adjust before casting or after finishing/cancelling.
4. Close popup, quit/reopen game, confirm percentages/mutes retained and FarmDemo progress unchanged. Check new calf/cow and newly created tree/fishing sources inherit setting.
5. Rebuild APK; verify notch-safe button placement, slider drag and Close/Mute hit targets on actual phone. Test native scene unload/pause/resume cleanup.

No real-device test or audible-output confirmation was performed.
