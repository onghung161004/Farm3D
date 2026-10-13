# Soundtrack implementation plan

Status: Done. Spec: 2026-10-12-soundtrack-spec.md. Strategy: small end-to-end implementation under existing instruction to implement without repeated questions.

1. Core audio: FarmSoundGenerator builds original 32-second stereo plucked-string/bass melody and two-second mono crack/rustle/thud. Clamp samples, taper clip endpoints, deterministic noise. Fall impact uses TreeHarvestVisual.FallDuration. No external dependencies or media files.
2. Runtime: FarmBackgroundMusic.Ensure(FarmHud) creates one loop source, spatialBlend=0, volume=0.18, fade-in two seconds, fishing mix=0.10, application-pause handling and owned clip cleanup. TreeFallAudio creates a spatial source at the felled tree, maxDistance=28, volume=0.75; only final successful hit triggers it. Remove old final creak layer, preserve ordinary chopping SFX.
3. Verification: NUnit generated sample bounds/energy/stereo/loop duration, spatial fall source and idempotent background installer; compile and run existing woodcutting/fishing regressions. Manual listen on headphones/Android; rebuild APK before device test.

Contracts: BuildMusicSamples(sampleRate) returns stereo PCM for MusicSeconds; BuildFallSamples(sampleRate, impactAt) returns mono PCM for FallSeconds; imported Resources clips take precedence. Missing shader/audio assets do not block synthesized audio. Empty/invalid sample rate rejects explicitly. Repeated installer calls cannot duplicate sources. Clip disposal handles EditMode and runtime correctly; imported clips are never destroyed.

Risks: generated music is an original simple synthesized instrumental, not a studio recording. Approximately 5.6 MB PCM for the stereo loop at 22.05 kHz plus its temporary generation buffer; fall audio is generated only when a tree falls. No save schema/UI/economy changes and no commit.

Verification: Unity 6000.6.1f1 isolated headless EditMode: 152 total tests, 151 passed. All three new soundtrack tests and existing woodcutting/fishing tests pass. The one existing HUD font-size failure remains unchanged. Runtime compilation succeeded. No actual headphone/Android listening test or APK build performed.

Code-reviewer verdict GREEN for generated sample validity, idempotent runtime installers, owned/imported clip lifecycle, fall-animation timing and final-hit hook. Pause stops background playback and resume preserves loop position; fishing reduces soundtrack gain. No per-frame audio synthesis or new media dependencies. Chopping action audio is retained; the old generic extra creak layer is removed to avoid doubling the fall sound.
