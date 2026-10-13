# Village quest chain

Source: README, FarmProgression/repair visuals and the previously proposed six-stage story. No separate wiki is present; use the user-approved code/docs fallback. Size M: shared economy, save, gameplay events, UI and navigation. Reuse existing successful actions and restored flags, not a separate currency/inventory or reconstructed village.

Stages (sequential reward claims, activity counted cumulatively):
1. Meet a resident/trader and harvest three carrots; 15 coins.
2. Produce one processed item and deliver one trader order; 20 coins.
3. Repair the existing bridge; 20 coins (existing price/wood requirement unchanged).
4. Keep two adult cows in the pen, feed two distinct cows, collect one milk; 25 coins.
5. Catch three fish, explicitly donate two at the quest board; 20 coins. No automatic stock subtraction.
6. Repair the existing house and return to the board to finish; 40 coins, final gratitude text and modest plaza decoration. Continue sandbox gameplay afterward.

Existing named residents represent the village; do not add a new chief character/model. A wooden board near the actual village well opens a TMP modal via ACTION; a QUESTS HUD button and keyboard Q provide access too. Board shows six-stage progress, current objectives, reward, explicit Claim/Give Fish and Track controls. Tracking opt-in draws one route using the existing ground pathfinder; cow/tree guides take precedence during those activities.

Save version 7 includes bounded quest counters, distinct fed cow IDs and claimed stage. Missing quest data in older saves seeds only minimum evidenced progress from current stock, completed trader orders and feeding/milking state; repairs remain credited as existing facts. Never pretend to know consumed historical quantities. Claims advance exactly once and cannot overflow coins. No new items, mission fees, asset downloads or commits.

Modal prevents other gameplay interactions and restores the movement lock it owns on close. Scene changes clean up subscriptions/owned materials. Navigation may show explicitly labelled bearing if ground route unavailable. Exact appearance and Android multi-touch still need device smoke testing.
