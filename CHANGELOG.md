# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-05-17

### Added
- WPF hex-grid map with axial/offset coordinate helpers (`HexHelper`).
- Twelve terrain types (Plain, City, Hills, Mountains, River, Water, Forest,
  Road, Bridge, Ruins, Farm, Swamp) with movement costs and defense bonuses
  (`TerrainCatalog`).
- Three unit kinds - Infantry, Mechanized, Tank - with attack, defense, HP,
  and movement-point profiles (`UnitCatalog`).
- Deterministic combat resolution based on attacker attack, defender defense,
  and terrain defense bonus (`CombatResolver`).
- Turn manager with a deployment phase (5 units per player) and an action
  phase that resets movement and the `HasMoved` flag at the start of each
  turn (`TurnManager`).
- AI opponent that scores reachable hexes by defensive terrain, objective
  proximity, enemy threat, and target priority (`AiController`).
- WPF main window with hex rendering, unit sprites, and a sound manager.
- Resource pipeline copying allied unit PNGs to the output folder.
- GitHub Actions workflows for build/test and semantic-version releases.
- Dependabot configuration for weekly NuGet and Actions updates.
- MIT license, security policy, contribution templates, and this changelog.

[Unreleased]: https://github.com/Jakub-Syrek/PanzerGeneral/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Jakub-Syrek/PanzerGeneral/releases/tag/v1.0.0
