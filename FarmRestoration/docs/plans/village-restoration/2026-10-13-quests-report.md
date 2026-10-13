# Village quest chain delivery

Date: 2026-10-13. Implemented without committing or editing the user's live scene/save.

## Delivered

- Six cumulative chapters, sequential explicit reward claims (15/20/20/25/20/40 coins).
- Actual successful harvest/production/talk/feed/milk/fishing hooks; distinct fed-cow IDs, shared inventory and existing repair flags.
- TMP objective tracker and QUESTS/Track buttons; Q/E/ACTION access, Close/Q/Esc dismissal. Movement lock ownership, ACTION/CAST/recipe/tool-selection blocking.
- Wooden board near VillageWell (trader fallback), supply crates after chapter 2, wooden bench and gratitude after chapter 6. Existing house/bridge visual restoration remains authoritative; no structure replacement or scene rebuild.
- Donation uses current stock, one fish per click, max two; donation/final claim buttons require player within 4 horizontal metres of the board.
- Opt-in gold ground path, throttled to 2 seconds; existing cow/axe guides take precedence. No false direct line through river when pathfinding fails; warning throttled to 10 seconds.
- Save version 7 with bounded counters and copied/deduplicated cow IDs. Legacy migration grants only evidenced minimum activities; cannot recover quantities already consumed/sold before counters existed.
- Metadata and README controls/progression instructions included.

## Test plan / results

Unity 6000.6.1f1, NUnit EditMode, isolated project D:/ThienAn/.codex-tmp/farm-breeding-20261012-202239. Fixtures suppress disk writes; actual player save untouched.

| Category | Coverage | Result |
|---|---|---|
| Happy/error paths | Complete/empty goals for all 6 chapters, real harvest/production/fish hooks | Passed |
| Null/empty | Missing quest save, null/blank IDs, missing HUD, empty fish stock | Passed |
| Boundaries | Counter saturation, invalid stage clamp, zero/negative fish, coin overflow | Passed |
| Format outliers | Unicode and duplicate cow IDs, >256-character IDs, JSON round-trip | Passed |
| State / duplicate input | Reward once, two donations then blocked third, full 6-claim chain, terminal reload | Passed |
| Integration / UI | Legacy/current save behavior, board idempotence, preserved well, final bench, own/pre-existing movement locks, lifecycle handlers, ACTION/tool/recipe blocking | Passed |

31 new tests passed. Complete regression run: 188 tests, 187 passed, 1 baseline failure: FarmHudPresentationTests.ReadabilityStyle_UsesLargeTypeAndTwoLineControls (expects >=34, existing value 32). No compiler errors. Results: quests-reviewed-20261013.xml; log: quests-reviewed-20261013.log in the isolated project. EditMode lifecycle tests invoke handlers explicitly; not a substitute for native PlayMode scene-unload checks.

## Code Review Report

Scope: six Scripts/Quests files; FarmProgression.Quests/FarmProgression/FarmSaveData; resident/trader talk hooks; PlayerToolController/MobileGameControls/FishingPondSpot/WoodcuttingGuide modal integration; FarmQuestTests/FarmQuestViewTests/WoodEconomyTests. Context reused from this feature's spec and plan.

Verdict: GREEN for implemented code. No unresolved critical issues or code warnings within this scope. Test-suite baseline exception and pending manual checks are disclosed, not represented as verified device behavior.

| Category | Verdict | Issues |
|---|---|---|
| Security | PASS | 0; local single-player state, no new credentials/network dependencies |
| Business logic | PASS | 0; guard eligibility/stock/overflow, bounded migration, unique claims |
| Performance | PASS | 0; event-driven UI, cached targets, throttled routes/notices |
| Clean code | PASS | 0; separated rules/progress/adapter/UI/board/nav; subscriptions/material ownership cleanup |
| Conventions | PASS | 0; existing namespace/TMP/Ensure/shared-save patterns, .meta files |

Review changes before delivery: notify quest UI when cow pen state changes; block hotbar/direct ACTION entry points while modal open; remove owned line object during navigation cleanup; filter locked expansion plots from destinations; use board proximity for donation/final-claim buttons. Rerun after fixes passed all 31 quest tests.

## Manual smoke checks still required

1. Stop Play mode if running, let Unity compile, then Play FarmDemo. Check tracker placement, text clipping and wooden board facing/terrain placement near the actual well; no scene rebuild required.
2. Fresh save: complete all six chapters, confirm existing repair costs unchanged, crates/bench appear, claim final reward near board, close/reopen game and verify retained progress.
3. Old save: already repaired house/bridge remain repaired, no repurchase. Historical sold fish/milk may need collecting again because old saves lacked counters.
4. Android: rebuild APK; check QUESTS/Track/Close/Give Fish/Claim hit areas, notched screens, touch release when opening modal, and controls after closing.
5. Native PlayMode scene unload: quest view releases only its own movement lock; no lingering subscriptions or trail objects.

No Android APK or device visual/multitouch run was performed in this task. Rewards are initial values, not playtest-proven balancing.
