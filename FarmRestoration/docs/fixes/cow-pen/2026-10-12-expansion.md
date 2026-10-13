# Visible pen expansion

Scope: upgrade wood/coin purchase and restoration, enclosure geometry, sign and regression tests. No commit.

Evidence: FarmProgression.TryUpgradeCowPen and Load previously only called SetCapacity(12). The woodcutting spec explicitly retained the footprint. The user now expects visible expansion; this was a requirements mismatch, not a failed payment or shader issue.

Fix: reuse the current CowPenFence wood panels. Add six metres towards the gate, shifting the centre by three metres and preserving the rear, all original side panels and any other child world positions. Move front fence pieces and workbench six metres forward, add three two-metre panels along each side, project moved objects to the existing ground collider. Increase the actual CowPen footprint (default 8x18 -> 8x24) and capacity to twelve. Apply idempotently on purchase and on existing upgraded saves, without charging again. Sign displays completed upgrade.

Tests (Unity NUnit EditMode): exact-cost purchase changes footprint; duplicate call does not charge/expand again; rotated enclosure preserves rear and cow positions; moved front/bench and new side panels; new inside/outside boundaries; existing JSON flag requires no new cost; null pen is safe. Existing cow containment, pathfinding and breeding tests remain regression checks.

Manual verification: restart FarmDemo Play mode using the same save. If already upgraded, the front fence/gate/workbench should be six metres farther forward; no need to repurchase. Scene edit-mode geometry remains the base enclosure because this is a saved gameplay upgrade.

Verification: Unity 6000.6.1f1 headless EditMode in isolated project: 132 tests, 130 passed. All pen expansion/economy/cow tests passed. The same two pre-existing failures remain (HUD font-size expectation and weather float boundary). Runtime scripts compiled. git diff --check passed. Actual FarmDemo rendered geometry/device play has not been visually verified here.

Code-reviewer scope: CowPenExpansion.cs, LumberWorkshop.cs, changed upgrade/load lines of FarmProgression.cs and WoodEconomyTests.cs. Verdict GREEN: upgrade payment guards remain intact, restored flag applies geometry before cows restore, expansion is idempotent, existing children retain world positions, new fences reuse existing models/materials and nothing is deleted. Security N/A; logic/performance/conventions PASS. No commit created.
