Started at: 2026/10/13 10:53:58
Finished at: 2026/10/13 10:57:36
Total time: 3.6 minutes (isolated measurement through diagnosis)

# Bridge widening / invisible surface alignment

Complexity:M, shared model/walkway/rail geometry. Current task diagnostic only; no production fix applied, awaiting approval.

## Symptoms / reproduction
User screenshot PlayMode 3x WIDTH shows character beside visible wooden bridge over water. Expected: walkable area and side collision coincide with visible deck/rails after widening. Actual: inferred invisible support outside mesh. No error/stack reported. Unity6000.6.1f1 FarmDemo, recent paid-widening and rail-collision additions. Reproduction: repair, upgrade twice, approach side.

## Hypotheses
1. Original collider broader than visible model, amplified by widening (high).
2. Wrong transverse axis or transform basis (medium).
3. Rail guards derive collider edge rather than actual visible railing (medium).

## Evidence
Isolated diagnostic loads native FBX and actual generated PolytopeBridgeWalkway.asset, reproduces root position(-46.148766,3.497434,41.89164), quaternion(-.0247814,.24258925,-.09855708,.9647916), scale(4.5639205,1,1) from FarmDemo. Parent identity confirmed in scene.

- Model local bounds centre(1.29365,.47521,-.11248), size(4.46984,2.07946,1.79418).
- Actual deck in model-local space centre(1.30961,.19820,-.11248), size(4.56496,.84604,3.30000).
- Collider/model transverse-width ratio1.839278, already wrong before scaling. At3x: full model width5.38254 versus support9.90000; roughly2.26m invisible excess on each side compared even with full model bounds.
- Width centres agree(-.11248); model long axisX, widthZ, so zero-axis/centering hypothesis not cause of this discrepancy.
- PolytopeNatureSetup.CreateWalkway hardcodes +/-1.65m. BridgeWidening preserves/scales this independently of model size; BridgeRailingCollision uses same broad collider edges.
- Existing tests verified linear scaling with synthetic3.3m walkway, not real model-surface correspondence; they could pass while retaining mismatch.

Hypothesis1 confirmed; hypothesis3 follows from code. Hypothesis2 eliminated for measured actual scene.

Diagnostic Native test1/1 passed; evidence alignment-diag-20261013.log/xml in D:/ThienAn/.codex-tmp/farm-breeding-20261012-202239. Only scratch test/diagnostic asset and documentation added; production code/source asset/save unchanged.
