# WET (Xbox 360)

Unofficial PC port of *WET* (2009) by Mohammed Albarghouthi, built with [ReXGlue](https://github.com/rexglue/rexglue-sdk). This repo does not include the game. You need an ISO dumped from a disc you own.

Support the project: https://buymeacoffee.com/mohmmadpodt

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

PLAY in the launcher starts that build. **CHECK FOR UPDATE** downloads the latest Windows zip from the GitHub releases page and replaces the port files. Your `game/` folder is left alone.

## Graphics

In the launcher **SETTINGS** tab (or F3 in-game):

- **Quality** (default) uses 2x render scale, FXAA Extreme, 16x filtering, and sharpen
- **Ultra** uses 3x at 1080p
- Render scale, anti-aliasing, filtering, and sharpen can also be set by hand

Resolution and render scale apply after a restart.

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
