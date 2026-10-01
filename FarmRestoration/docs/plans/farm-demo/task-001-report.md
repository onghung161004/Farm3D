# TASK-001 Report: Project structure and FarmDemo scene shell

## Changed files

- `Assets/FarmRestoration/Editor/FarmDemoSceneGenerator.cs`
- `Assets/FarmRestoration/{Scenes,Scripts,Prefabs,Materials,Models,Textures,Animations,Audio,UI,Tests/EditMode}` (created through `AssetDatabase`)
- `Assets/FarmRestoration/Materials/FarmGround.mat` (created by the generator)
- `Assets/FarmRestoration/Materials/FarmWood.mat` (created by the generator)
- `Assets/FarmRestoration/Materials/FarmSoil.mat` (created by the generator)
- `Assets/FarmRestoration/Scenes/FarmDemo.unity` (created by the generator)

## Use

In Unity, wait for compilation to finish, then select **Tools > Farm Restoration > Create FarmDemo Scene (If Missing)**. It creates `FarmDemo.unity` and its three low-poly URP materials only when the scene does not already exist, so future player, plot, and UI work cannot be accidentally removed.

**Tools > Farm Restoration > Overwrite FarmDemo Scene (Destructive)** is a separate operation. It displays a confirmation dialog before permanently replacing the scene and should only be used before downstream scene work starts.

The generated scene contains an empty `FarmDemoBootstrap` root, an `Environment` root with the lit `FarmGround` plane, a directional light, and a Main Camera. It has no custom runtime scripts, so it cannot produce missing-script warnings.

## Batch-mode verification

From the project root (after the editor has imported the generator), run:

```powershell
& 'D:\UnityHub\6000.6.1f1\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\ThienAn\Game3D\FarmRestoration' -executeMethod FarmRestoration.Editor.FarmDemoSceneGenerator.CreateFarmDemoIfMissing -logFile 'D:\ThienAn\Game3D\FarmRestoration\Logs\task-001-unity.log'
```

Verify exit code `0` and that the log includes `FarmDemo scene shell created`. Then open `Assets/FarmRestoration/Scenes/FarmDemo.unity` in Unity and confirm the ground is lit and the Console has no missing-script warnings.

## Verification status

The batch command was attempted, but Unity correctly refused it because this project is already open in another Unity editor process. Run the menu command above from that open editor (or close it before using batch mode) to generate and inspect the scene. A direct `dotnet build` is not a Unity compiler substitute on this machine because the local .NET Framework 4.7.1 targeting pack is absent.
