# PanzerGeneral

Turn-based hex-grid wargame for Windows, inspired by SSI's Panzer General.

![CI](https://github.com/Jakub-Syrek/PanzerGeneral/actions/workflows/tests.yml/badge.svg)
![Release](https://img.shields.io/github/v/release/Jakub-Syrek/PanzerGeneral)
![.NET](https://img.shields.io/badge/.NET-9.0--windows-512BD4?logo=dotnet)
![License](https://img.shields.io/github/license/Jakub-Syrek/PanzerGeneral)
![Last commit](https://img.shields.io/github/last-commit/Jakub-Syrek/PanzerGeneral)

## Overview

PanzerGeneral is a Windows-only WPF game in which the player commands a small
allied force across a hex map and fights an AI opponent for control of city
objectives. The rules are intentionally compact: pure attack and defense
values, terrain modifiers, movement points, and a deployment phase before the
action begins.

## Gameplay

- **Hex map** - rendered with WPF polygons via `HexHelper`. Coordinates are
  stored in offset (column, row) form and converted to cube coordinates only
  when distance has to be computed.
- **Terrain** - twelve types (`TerrainType`) with per-tile move costs, defense
  bonuses, and line-of-sight rules (`TerrainCatalog`).
- **Units** - infantry, mechanized, tank (`UnitCatalog`), each with attack,
  defense, HP, and movement points.
- **Combat** - deterministic damage: `max(1, attack - (defense + terrainBonus))`,
  applied to the defender's HP (`CombatResolver`).
- **Turns** - `TurnManager` runs a one-turn deployment phase (5 units per
  player) and then alternates action turns, resetting movement at the start
  of each side's turn.
- **AI** - `AiController` enumerates reachable hexes with BFS over movement
  cost, scores positions on defensive terrain, objective proximity, adjacent
  enemy threat and target priority, and emits an ordered action plan.

## Architecture

```
PanzerGeneral/
  PanzerGeneral.sln
  PanzerGeneral/                Main WPF project (net9.0-windows)
    App.xaml(.cs)               Application entry point
    MainWindow.xaml(.cs)        Game window, rendering, input
    Game/
      AiController.cs           Enemy turn planner
      CombatResolver.cs         Damage calculation
    Helpers/
      HexHelper.cs              Hex geometry
      HexTile.cs                Map cell
      TerrainCatalog.cs         Terrain factory and modifiers
      TerrainType.cs            Terrain enum
      TextureHelper.cs          Sprite/texture loading
      SoundManager.cs           Sound effects
      TurnManager.cs            Phase and turn state
      Unit.cs                   Unit entity
      UnitCatalog.cs            Unit factory and stats
      UnitType.cs               Unit enum
    Resources/                  PNG sprites for allied units
  PanzerGeneral.Tests/          NUnit + NSubstitute test project
```

## Build & Run

Requires the .NET 9 SDK on Windows.

```powershell
dotnet build PanzerGeneral.sln -c Release
dotnet run --project PanzerGeneral/PanzerGeneral.csproj
```

Or open `PanzerGeneral.sln` in Visual Studio 2022/2026 or Rider.

## Testing

```powershell
dotnet test PanzerGeneral.sln -c Release
```

The test project (`PanzerGeneral.Tests`) targets the pure game-logic types -
combat, terrain, turns, AI scoring - and avoids anything bound to WPF
rendering. NUnit is the runner; NSubstitute is available for mocking.

## Versioning

This repository follows [Semantic Versioning](https://semver.org). Versions
are bumped automatically by the [version workflow](.github/workflows/version.yml)
on each push to `main`:

- a commit footer containing `BREAKING CHANGE:` -> major
- a `feat:` commit -> minor
- anything else (`fix:`, `chore:`, `docs:`, `test:`, `ci:`, `refactor:`) -> patch

The workflow writes `<Version>` into the main `.csproj`, commits
`chore(release): vX.Y.Z`, tags `vX.Y.Z`, and publishes a GitHub Release with
the framework-dependent and self-contained Windows builds attached.

See [CHANGELOG.md](CHANGELOG.md) for a human-readable history.

## License

[MIT](LICENSE) - Copyright (c) 2026 Jakub Syrek.

## Contact

- Author: Jakub Syrek - <jakubvonsyrek@gmail.com>
- Issues: <https://github.com/Jakub-Syrek/PanzerGeneral/issues>
- Security: see [SECURITY.md](SECURITY.md)
