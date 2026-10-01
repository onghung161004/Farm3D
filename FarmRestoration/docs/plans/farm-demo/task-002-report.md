# TASK-002 Report: Player movement and third-person camera

## Delivered

- `Assets/FarmRestoration/Scripts/Player/PlayerMovement.cs`
  - Uses the Unity Input System's `Keyboard.current` for WASD and arrow-key input.
  - Converts movement to camera-relative planar movement, clamps diagonal input, drives a `CharacterController`, applies gravity, and smoothly rotates toward movement.
- `Assets/FarmRestoration/Scripts/Player/PlayerFollowCamera.cs`
  - Implements a damped third-person camera using `Vector3.SmoothDamp` and frame-rate independent rotation smoothing.
- `Assets/FarmRestoration/Scripts/Player/PlayerMovementMath.cs`
  - Keeps the pure diagonal-input clamp and camera-relative planar-direction calculation separate and testable.
- `Assets/FarmRestoration/Tests/EditMode/PlayerMovementMathTests.cs`
  - EditMode tests cover normalizing diagonal input, preserving input that is already inside the unit circle, zero movement, and camera-relative planar forward movement.
- `Assets/FarmRestoration/Editor/FarmDemoPlayerSetup.cs`
  - Safely adds/configures a capsule `Player`, `CharacterController`, `PlayerMovement`, and `PlayerFollowCamera` in the open `FarmDemo` scene without deleting or overwriting the scene.
  - Sets `Player` to `y = 1`, with a `CharacterController` height of `2` and center of `(0, 0, 0)`, so its bottom rests on the `y = 0` ground plane.
- `Assets/FarmRestoration/Scripts/FarmRestoration.Runtime.asmdef`
  - Defines the runtime assembly consumed by Editor tooling and tests.
- `Assets/FarmRestoration/Editor/FarmRestoration.Editor.asmdef`
  - Defines the Editor-only assembly and explicitly references the runtime assembly.
- `Assets/FarmRestoration/Tests/EditMode/FarmRestoration.EditModeTests.asmdef`
  - Defines an Editor-only Test Framework assembly, explicitly references the runtime assembly, and marks it as `TestAssemblies` for reliable Unity Test Runner discovery.

## TDD evidence

1. `PlayerMovementMathTests.cs` was written before `PlayerMovementMath.cs`; it referenced the intentionally absent `PlayerMovementMath.NormalizePlanarInput` API.
2. A Unity batch EditMode test run was attempted immediately afterward. Unity correctly refused to open a second instance because the project is already open in the Unity Editor:

```text
Aborting batchmode due to fatal error:
It looks like another Unity instance is running with this project open.
Multiple Unity instances cannot open the same project.
```

3. The minimal production helper and runtime scripts were then added. The final EditMode run must be launched from the already-open Editor because its project lock prevents a safe parallel command-line run.

## Run in Unity

1. Open `Assets/FarmRestoration/Scenes/FarmDemo.unity`.
2. Run **Tools > Farm Restoration > Configure FarmDemo Player and Camera**.
3. Press `Ctrl+S` to save the scene change.
4. Press Play. Use WASD or arrow keys to move. The camera follows and the player turns toward movement.
5. Verify tests through **Window > General > Test Runner > EditMode > Run All**.

## Verification status

- Static input setting checked: `ProjectSettings/ProjectSettings.asset` has `activeInputHandler: 1` (Input System enabled).
- Unity Console evidence: `PlayerMovement.cs(2,19)` reported `CS0234` for `UnityEngine.InputSystem` even though `com.unity.inputsystem` is installed. The cause was that the new named `FarmRestoration.Runtime` assembly had an empty `references` list; named assemblies do not implicitly reference package assemblies. `FarmRestoration.Runtime.asmdef` now explicitly references `Unity.InputSystem`.
- Automated Unity test execution is blocked only by the active Editor's single-project lock; no source error was reported by the batch process because it did not acquire the project.
