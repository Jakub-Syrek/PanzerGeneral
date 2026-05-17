# About PanzerGeneral

PanzerGeneral is a turn-based hex-grid wargame for Windows, built with WPF on
.NET 9. It draws inspiration from SSI's classic Panzer General series:
infantry, mechanized, and armored units maneuver across a hand-built map of
plains, hills, forests, rivers, mountains, and cities, trading attacks
modified by terrain and unit type.

The game ships with a deployment phase, a turn manager that alternates between
the human player and an AI opponent, and a scoring AI that weighs defensive
terrain, adjacent threats, objective control, and target priority before
committing to a move. Combat resolution is deterministic and driven by attack,
defense, and terrain defense bonuses, which keeps the core rules easy to
reason about and easy to test.

The project is open source under the MIT license. Releases are produced by an
automated semantic-versioning workflow and published as both self-contained
and framework-dependent Windows builds.
