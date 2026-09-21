# WET (Xbox 360)

PC port of *WET* (2009) via [ReXGlue](https://github.com/rexglue/rexglue-sdk). This repo does not include the game. You need an ISO dumped from a disc you own.

## Requirements

- Windows 10/11, x64
- Clang, CMake, Ninja, Git
- A WET Xbox 360 `.iso`

## Build

1. Open `launcher/WetLauncher/WetLauncher.csproj` in Visual Studio (or MSBuild) and build **Release**.
2. Run the launcher, click **ADD ISO**, pick your dump. Files go in `game/`.
3. From the repo root:

```powershell
pwsh .\setup.ps1
pwsh .\build.ps1
```

`setup.ps1` clones ReXGlue, applies the mouse/keyboard patch, and runs codegen. `build.ps1` produces `wet.exe`.

PLAY in the launcher starts that build.

## Controls

| | |
|---|---|
| WASD | Move |
| Mouse | Look |
| Left click | Shoot |
| Right click | Sword |
| F3 | Settings |

Rebind keys in the launcher **CONTROLS** tab before you hit PLAY.

## License

Port code is MIT. *WET* and its assets belong to their copyright holders. Don't ship `game/` or ISOs.
