# Mobile ACTION fishing

Root cause: CAST ROD invokes FishingHud.nearbySpot directly, selected by fishing range eligibility. Mobile ACTION only queued general PlayerToolController raycast/overlap interaction, whose target can differ or be omitted by its eight-collider buffer. Thus a visible CAST button did not guarantee that ACTION starts the same spot.

User approved correction: share one TryCastShownSpot path for CAST and mobile ACTION. On a new mobile press, try the shown eligible fishing spot before queueing general interaction. Revalidate via FishingPondSpot.TryBeginFromTouch; if not visible, stale/out of range, or unsuccessful, leave general interaction behaviour intact. Successful cast clears queued farm interaction and held-garden state. Keyboard E, fishing pull/reward, save data and other interactions unchanged.

Verification plan (Unity NUnit): shown spot begins fishing despite a different general target; duplicate ACTION cannot recast; hidden/no spot and stale range do not start fishing and keep general input queued; CAST and ACTION share destination. Check on Android after rebuild: stand where CAST ROD is visible, press ACTION once, confirm casting/waiting, then hold/release REEL after bite. Device verification remains required.

Result: runtime compiled; all three MobileFishingActionTests pass in Unity 6000.6.1f1 isolated EditMode project. Full suite 135 tests: 133 passed, with the same two unrelated baseline failures (HUD font size and weather float boundary). git diff --check passed. No APK build/device verification performed, no commit created.

Review (code-reviewer): GREEN for FishingHud.cs, the new mobile input branch of PlayerToolController.cs, and MobileFishingActionTests.cs. Shared cast method does not create a HUD when absent; checks shown button/session and delegates current eligibility to the existing spot. Normal interaction is retained on failure, successful cast removes queued garden action, and no save/gameplay economy changes occur. Rebuild and install an updated APK to test on the phone; the installed old APK will not acquire local code edits.
