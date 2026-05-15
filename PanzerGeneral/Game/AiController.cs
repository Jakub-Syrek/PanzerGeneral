using System;
using System.Collections.Generic;
using System.Linq;

namespace PanzerGeneral.Game
{
    public class AiController
    {
        private readonly Dictionary<(int col, int row), HexTile> _map;
        private readonly List<Unit> _units;

        private static readonly (int dc, int dr)[] EvenNeighbors =
            { (-1, 0), (1, 0), (-1, -1), (0, -1), (-1, 1), (0, 1) };
        private static readonly (int dc, int dr)[] OddNeighbors =
            { (-1, 0), (1, 0), (0, -1), (1, -1), (0, 1), (1, 1) };

        public record AiAction(Unit Unit, (int col, int row)? MoveTo, Unit? Attack);

        public AiController(Dictionary<(int col, int row), HexTile> map, List<Unit> units)
        {
            _map = map;
            _units = units;
        }

        // Returns ordered list of actions for all AI units this turn
        public List<AiAction> PlanTurn(int playerId)
        {
            int enemyId = playerId == 1 ? 2 : 1;
            var objectives = _map.Values.Where(t => t.IsObjective).ToList();

            return _units
                .Where(u => u.OwnerId == playerId && !u.HasMoved)
                .OrderByDescending(u => u.Attack) // tanks move first
                .Select(u => PlanUnit(u, enemyId, objectives))
                .ToList();
        }

        private AiAction PlanUnit(Unit unit, int enemyId, List<HexTile> objectives)
        {
            var enemies = _units.Where(u => u.OwnerId == enemyId).ToList();
            if (!enemies.Any()) return new AiAction(unit, null, null);

            // Attack adjacent enemy without moving (pick weakest / most damaged)
            var immediateTarget = GetAdjacentEnemiesAt(unit.Column, unit.Row, enemyId)
                .OrderBy(PriorityScore)
                .FirstOrDefault();
            if (immediateTarget != null)
                return new AiAction(unit, null, immediateTarget);

            // Find best reachable hex
            var reachable = GetReachableHexes(unit, enemyId);
            if (!reachable.Any()) return new AiAction(unit, null, null);

            var bestPos = reachable
                .OrderByDescending(pos => ScorePosition(pos, enemies, objectives, enemyId))
                .First();

            // Check if we can attack after moving there
            var attackAfterMove = GetAdjacentEnemiesAt(bestPos.col, bestPos.row, enemyId)
                .OrderBy(PriorityScore)
                .FirstOrDefault();

            return new AiAction(unit, bestPos, attackAfterMove);
        }

        // Lower = higher priority to attack (almost dead enemies first, then weakly-defended)
        private static double PriorityScore(Unit enemy) =>
            enemy.HP * 0.6 + enemy.Defense * 0.4;

        private double ScorePosition((int col, int row) pos, List<Unit> enemies,
            List<HexTile> objectives, int enemyId)
        {
            if (!_map.TryGetValue(pos, out var tile)) return double.MinValue;

            double score = 0;

            // Prefer defensive terrain
            score += tile.DefenseBonus * 7;

            // Very high bonus for capturing the objective
            if (tile.IsObjective) score += 100;

            // Bonus per adjacent attackable enemy
            var adjacentEnemies = GetAdjacentEnemiesAt(pos.col, pos.row, enemyId);
            foreach (var e in adjacentEnemies)
            {
                score += 35;
                score += (20.0 - e.HP) * 1.5; // Extra bonus for almost-dead enemies
            }

            // Penalise being outnumbered
            if (adjacentEnemies.Count > 1) score -= adjacentEnemies.Count * 8;

            // Advance toward nearest enemy
            double nearestEnemy = enemies.Min(e => HexDist(pos.col, pos.row, e.Column, e.Row));
            score -= nearestEnemy * 3;

            // Advance toward objectives
            if (objectives.Any())
                score -= objectives.Min(o => HexDist(pos.col, pos.row, o.Column, o.Row)) * 5;

            return score;
        }

        private List<Unit> GetAdjacentEnemiesAt(int col, int row, int enemyId)
        {
            var nb = NeighborSet(col, row);
            return _units.Where(u => u.OwnerId == enemyId && nb.Contains((u.Column, u.Row))).ToList();
        }

        private HashSet<(int col, int row)> GetReachableHexes(Unit unit, int enemyId)
        {
            var reachable = new HashSet<(int, int)>();
            var best = new Dictionary<(int, int), int> { [(unit.Column, unit.Row)] = unit.CurrentMovePoints };
            var queue = new Queue<((int col, int row) pos, int rem)>();
            queue.Enqueue(((unit.Column, unit.Row), unit.CurrentMovePoints));

            while (queue.Count > 0)
            {
                var (pos, rem) = queue.Dequeue();
                foreach (var nb in NeighborSet(pos.col, pos.row))
                {
                    if (!_map.TryGetValue(nb, out var tile) || tile.BlocksMovement) continue;
                    // Cannot land on any occupied hex
                    if (_units.Any(u => u.Column == nb.col && u.Row == nb.row)) continue;

                    int left = rem - tile.MoveCost;
                    if (left < 0) continue;
                    if (best.TryGetValue(nb, out int prev) && prev >= left) continue;

                    best[nb] = left;
                    reachable.Add(nb);
                    queue.Enqueue((nb, left));
                }
            }
            return reachable;
        }

        private HashSet<(int col, int row)> NeighborSet(int col, int row)
        {
            var offsets = row % 2 == 0 ? EvenNeighbors : OddNeighbors;
            return offsets
                .Select(o => (col + o.dc, row + o.dr))
                .Where(p => _map.ContainsKey(p))
                .ToHashSet();
        }

        // Cube-coordinate hex distance for odd-r offset grid
        public static double HexDist(int c1, int r1, int c2, int r2)
        {
            int x1 = c1 - (r1 - (r1 & 1)) / 2, z1 = r1, y1 = -x1 - z1;
            int x2 = c2 - (r2 - (r2 & 1)) / 2, z2 = r2, y2 = -x2 - z2;
            return (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) + Math.Abs(z1 - z2)) / 2.0;
        }
    }
}
