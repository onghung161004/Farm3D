# Audio controls implementation plan

Spec: 2026-10-13-volume-spec.md. Owner: current agent. Date:2026-10-13. Status: Implemented; automated verification complete, manual audio/device smoke checks pending.

## 1. Context
Separate two user-controlled gains while retaining existing source balance/spatial sound. Shared HUD/modal/player, music and SFX sources affected. No new clips, mixer assets, gameplay save migrations, commit or APK build.

## 2. Constraints
C#/Unity/TMP/InputSystem/PlayerPrefs/NUnit; no dependencies. Event-driven gain updates, no scene/source scanning each frame. Preserve dirty changes and avoid real preference writes in tests.

## 3. Conventions
FarmRestoration namespace, Ensure/UI helpers, clamped settings, per-device separate preferences. Owned-lock lifecycle and source subscriptions; NUnit fixtures with unique preference key.

## 4. Contracts
AudioVolumeState: music/effects volume0..1 default1, muted booleans and allMuted; Normalize clamps invalid data/non-finite values; Gain(group) zero for relevant mute else level; Clone independent.
FarmAudioSettings: lazy Load, Snapshot copy, SetVolume/SetMuted/SetAllMuted publish Changed without disk flush; Save dirty on close/pause/quit; Reload restores normalized JSON key FarmRestoration.Audio.v1. RestoreSession supplies normalized test/session values without write.
FarmAudioChannel.Bind(source,group,baseVolume) idempotent per source; Apply -> source.volume=base*effectiveGain; SetBaseVolume changes balanced volume, not user setting; unsubscribe on disable/destroy. Invalid source -> null.
FarmAudioSettingsView.Ensure(hud) idempotent, Show -> false if competing modal/fishing; Close restores only owned movement lock, saves; sliders/mute state reflect snapshot via no-notify refresh.

## 5. Architecture
HUD popup -> shared settings Changed -> registered source channels Apply -> existing AudioSource. Music fade and footsteps change channel base volume; global/user gains remain authoritative. Save independently from FarmDemoSave.

## 6. Artifacts
T1 new Scripts/Audio/AudioVolumeState.cs, FarmAudioSettings.cs, FarmAudioChannel.cs, folder/file metadata.
T2 modify FarmBackgroundMusic/FarmAudioFeedback/CowAudioFeedback/FishingFeedback/TreeFallAudio source registration.
T3 new UI/FarmAudioSettingsView.cs and AudioSettingsUi.cs; modify FarmHud and modal guards in PlayerToolController/MobileGameControls/FishingPondSpot/WoodcuttingGuide/FarmQuestNavigation/FarmQuestView.
T4 new EditMode/FarmAudioSettingsTests.cs metadata, README and verification report.

## 7. Phases/task graph
Approved complete implementation, prior no repeated questions instruction: core state/channel (T1), real source integration (T2), popup/control integration (T3), verification (T4). T1->T2->T3->T4. Separate mixer system rejected because existing sources are already enumerated and low count; listener-wide volume rejected because it cannot separate music/SFX. No delegation.

## 8. Tasks / acceptance
T1 consumes default/saved values, supplies gain API to T2/T3; volume0/1/out-of-range/NaN/null -> safe; group/global mute doesn't erase levels, saved clone independent, source idempotent. Each file <200LOC.
T2 consumes channels; all six source types use correct group; music ducking cannot reset slider, newly spawned cow/tree inherits gain, current sound adjusts on Changed.
T3 consumes shared settings/UI helpers; sliders and mute-all usable mouse/touch, no feedback loop, existing lock retained, no actions behind modal, live percentage reflects saved values. UI factory/view each <200LOC.
T4 covers state, independent gains, persisted roundtrip/corrupt input, late-created source, idempotence, cleanup and modal locks. Unique preference test key, no player-save writes.

## 9. Edge cases
Null/corrupt saved data and float boundaries/outliers:T1. Repeated bind/mute/slider input, source teardown and new sources:T1/T2. Existing modal/fishing/lock, popup destruction/small screens:T3. Legacy gameplay progress must remain untouched:T1/T4.

## 10. Risks
Existing source.volume writers can override settings: all writers use channel base API(T2). Modal routes/direct hotbar calls bypass UI: guard direct controller entry points(T3). Android slider hit area/notch placement: safe-area positioning and manual device checks(T3/T4).

## 11. Verification
Unity NUnit isolated state/source/UI fixtures plus full regression. Manual PC/Android: hear music/SFX independently at0/50/100, mute/unmute/allMute, spawning cows/treefall, popup pointer blocking, reopen app, safe area. Known HUD-font regression baseline disclosed separately. No commit.

Result: 14 new audio-control tests passed. Full EditMode suite:218 total,217 passed,1 known HUD-font baseline failure (32 versus >=34). No compile errors. See 2026-10-13-volume-report.md.
