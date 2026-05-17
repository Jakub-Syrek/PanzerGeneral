using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PanzerGeneral;
using PanzerGeneral.Game;

namespace PanzerGeneral.Tests;

/// <summary>
/// Tests for <see cref="AiController"/> action planning and the cube-distance helper.
/// </summary>
[TestFixture]
public class AiControllerTests
{
    /// <summary>
    /// Builds a small rectangular plain map for AI planning tests.
    /// </summary>
    private static Dictionary<(int col, int row), HexTile> BuildPlainMap(int width, int height)
    {
        var map = new Dictionary<(int col, int row), HexTile>();
        for (int c = 0; c < width; c++)
        {
            for (int r = 0; r < height; r++)
            {
                map[(c, r)] = TerrainCatalog.CreateTile(c, r, TerrainType.Plain);
            }
        }
        return map;
    }

    [Test]
    public void HexDist_SameHex_IsZero()
    {
        Assert.That(AiController.HexDist(2, 2, 2, 2), Is.EqualTo(0));
    }

    [Test]
    public void HexDist_NeighbouringHexes_IsOne()
    {
        // (1,0) and (1,1) are adjacent on an odd-r offset grid.
        Assert.That(AiController.HexDist(1, 0, 1, 1), Is.EqualTo(1));
        // (0,0) and (2,0) are two hexes apart along the same row.
        Assert.That(AiController.HexDist(0, 0, 2, 0), Is.EqualTo(2));
    }

    [Test]
    public void PlanTurn_AdjacentEnemy_ResultsInAttackWithoutMoving()
    {
        var map = BuildPlainMap(6, 6);
        var aiUnit = UnitCatalog.Create(UnitType.Tank, ownerId: 2, 2, 2);
        var enemy = UnitCatalog.Create(UnitType.Infantry, ownerId: 1, 3, 2);
        var units = new List<Unit> { aiUnit, enemy };

        var ai = new AiController(map, units);
        var actions = ai.PlanTurn(playerId: 2);

        Assert.That(actions, Has.Count.EqualTo(1));
        var action = actions[0];
        Assert.Multiple(() =>
        {
            Assert.That(action.Unit, Is.SameAs(aiUnit));
            Assert.That(action.MoveTo, Is.Null, "should attack from its current hex");
            Assert.That(action.Attack, Is.SameAs(enemy));
        });
    }

    [Test]
    public void PlanTurn_NoEnemiesOnMap_ProducesNoMoveOrAttack()
    {
        var map = BuildPlainMap(4, 4);
        var aiUnit = UnitCatalog.Create(UnitType.Infantry, ownerId: 2, 1, 1);
        var ai = new AiController(map, new List<Unit> { aiUnit });

        var actions = ai.PlanTurn(playerId: 2);

        Assert.That(actions, Has.Count.EqualTo(1));
        Assert.That(actions[0].MoveTo, Is.Null);
        Assert.That(actions[0].Attack, Is.Null);
    }

    [Test]
    public void PlanTurn_DistantEnemy_AdvancesToReachableHex()
    {
        var map = BuildPlainMap(8, 4);
        var aiUnit = UnitCatalog.Create(UnitType.Tank, ownerId: 2, 0, 1);
        var enemy = UnitCatalog.Create(UnitType.Infantry, ownerId: 1, 7, 1);
        var ai = new AiController(map, new List<Unit> { aiUnit, enemy });

        var action = ai.PlanTurn(playerId: 2).Single();

        Assert.That(action.MoveTo, Is.Not.Null, "AI must move when no enemy is adjacent");
        var start = (col: 0, row: 1);
        Assert.That(action.MoveTo!.Value, Is.Not.EqualTo(start));
        double before = AiController.HexDist(start.col, start.row, enemy.Column, enemy.Row);
        double after = AiController.HexDist(action.MoveTo!.Value.col, action.MoveTo!.Value.row,
            enemy.Column, enemy.Row);
        Assert.That(after, Is.LessThan(before), "the chosen hex should close the distance");
    }
}
