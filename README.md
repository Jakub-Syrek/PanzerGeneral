# PanzerGeneral

[![CI](https://github.com/Jakub-Syrek/PanzerGeneral/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Jakub-Syrek/PanzerGeneral/actions/workflows/ci.yml)
[![Release](https://github.com/Jakub-Syrek/PanzerGeneral/actions/workflows/release.yml/badge.svg)](https://github.com/Jakub-Syrek/PanzerGeneral/actions/workflows/release.yml)
[![Latest release](https://img.shields.io/github/v/release/Jakub-Syrek/PanzerGeneral?include_prereleases&sort=semver)](https://github.com/Jakub-Syrek/PanzerGeneral/releases)
![.NET](https://img.shields.io/badge/.NET-9.0--windows-512BD4?logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![Genre](https://img.shields.io/badge/genre-hex%20wargame-8B4513)
![Last commit](https://img.shields.io/github/last-commit/Jakub-Syrek/PanzerGeneral)
![Code size](https://img.shields.io/github/languages/code-size/Jakub-Syrek/PanzerGeneral)

WPF hex-grid wargame inspired by the classic SSI Panzer General series. .NET 9, Windows-only. Turn-based combat between Allied units (infantry, mechanized, tanks) on a hex map with terrain, AI opponent, and sound.

## Gameplay

- **Hex grid map** — every tile has a terrain type (`Helpers/TerrainType.cs`, `TerrainCatalog.cs`) that affects movement cost and combat modifiers.
- **Units** — infantry, mechanized, tank (`Helpers/UnitType.cs`, `UnitCatalog.cs`). Each unit has movement points, attack, defense and HP. Sprites in `Resources/*_allied.png`.
- **Turn-based** — `Helpers/TurnManager.cs` alternates between the player and the AI side.
- **AI opponent** — `Game/AiController.cs` picks moves and attacks for the enemy side.
- **Combat** — `Game/CombatResolver.cs` calculates damage from unit stats, terrain bonuses, and a random factor.
- **Sound** — `Helpers/SoundManager.cs` plays UI / combat / movement cues.

## Build & run

Requires .NET 9 SDK and Windows.

```powershell
dotnet build PanzerGeneral\PanzerGeneral.csproj -c Release
dotnet run --project PanzerGeneral\PanzerGeneral.csproj
```

Or open `PanzerGeneral.sln` in Visual Studio 2026+ / Rider.

## CI / CD

Two GitHub Actions workflows:

- **[ci.yml](.github/workflows/ci.yml)** — runs on every push to `main` and PR → `main`. Builds Debug + Release on `windows-latest` with the .NET 9 SDK to catch breakage early.
- **[release.yml](.github/workflows/release.yml)** — runs when a `v*` tag is pushed. Publishes two flavors and attaches them to an auto-created GitHub Release:
  - `PanzerGeneral-<ver>-win-x64-selfcontained.zip` — single-file `.exe` with .NET 9 runtime bundled (~70 MB, no install)
  - `PanzerGeneral-<ver>-win-x64-framework-dependent.zip` — small (~5 MB) but requires .NET 9 Desktop Runtime on the user's machine

To cut a release:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

Dependabot ([.github/dependabot.yml](.github/dependabot.yml)) opens weekly PRs for outdated NuGet packages and GitHub Actions versions every Monday.

## Project layout

```
PanzerGeneral/
  PanzerGeneral.sln
  PanzerGeneral/
    App.xaml(.cs)            WPF application entry point
    MainWindow.xaml(.cs)     Main game window
    PanzerGeneral.csproj     .NET 9 WPF SDK project (System.Windows.Extensions)
    Game/
      AiController.cs        AI side turn logic
      CombatResolver.cs      Damage calculations
    Helpers/
      HexHelper.cs           Hex-grid geometry (axial/cube coords, neighbors, distance)
      HexTile.cs             One tile on the map
      TerrainCatalog.cs      All available terrain types and their modifiers
      TerrainType.cs         Plain / forest / mountain / city / road / ...
      TextureHelper.cs       Loads PNG sprites from /Resources at runtime
      SoundManager.cs        Plays sfx
      TurnManager.cs         Player ↔ AI turn switching
      UnitCatalog.cs         All unit kinds and their base stats
      UnitType.cs            Infantry / Mechanized / Tank
      Unit.cs                Helper unit representation (cf. Models/Unit.cs)
    Models/
      Unit.cs                Game-state unit (HP, position, owner, type)
    Resources/
      infantry_allied.png    Allied infantry sprite (set CopyToOutputDirectory=Always)
      mechanized_allied.png
      tank_allied.png
```

## Notes

- Only allied sprites are committed — enemy / axis sprites need to be added to `Resources/` and registered the same way in the `.csproj` for them to ship to the output folder.
- There are two `Unit.cs` files (`Helpers/` and `Models/`). They serve different roles; if you refactor, pick one canonical type and remove the duplicate.
- `System.Windows.Extensions` is referenced for `System.Media.SoundPlayer` and friends.
