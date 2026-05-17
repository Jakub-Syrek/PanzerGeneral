using System.Linq;
using NUnit.Framework;
using PanzerGeneral;
using PanzerGeneral.Game;

namespace PanzerGeneral.Tests;

/// <summary>
/// Tests for <see cref="TurnManager"/> phase transitions and movement reset.
/// </summary>
[TestFixture]
public class TurnManagerTests
{
    [Test]
    public void Constructor_StartsAtTurnOneInDeployPhaseWithFirstPlayerActive()
    {
        var manager = new TurnManager(new[] { 1, 2 });

        Assert.Multiple(() =>
        {
            Assert.That(manager.TurnNumber, Is.EqualTo(1));
            Assert.That(manager.IsDeployPhase, Is.True);
            Assert.That(manager.CurrentPlayerId, Is.EqualTo(1));
        });
    }

    [Test]
    public void NextTurn_AfterBothPlayersPlay_LeavesDeployPhaseAndIncrementsTurn()
    {
        var manager = new TurnManager(new[] { 1, 2 });

        manager.NextTurn(); // -> player 2, still turn 1, still deploy
        Assert.That(manager.CurrentPlayerId, Is.EqualTo(2));
        Assert.That(manager.TurnNumber, Is.EqualTo(1));
        Assert.That(manager.IsDeployPhase, Is.True);

        manager.NextTurn(); // -> back to player 1, turn 2, action phase
        Assert.Multiple(() =>
        {
            Assert.That(manager.CurrentPlayerId, Is.EqualTo(1));
            Assert.That(manager.TurnNumber, Is.EqualTo(2));
            Assert.That(manager.IsDeployPhase, Is.False);
        });
    }

    [Test]
    public void CanDeployUnit_RespectsPerPlayerCapAndPhase()
    {
        var manager = new TurnManager(new[] { 1, 2 });

        for (int i = 0; i < TurnManager.MaxUnitsPerPlayer; i++)
        {
            Assert.That(manager.CanDeployUnit(1), Is.True);
            manager.RegisterDeployedUnit(1);
        }
        Assert.That(manager.CanDeployUnit(1), Is.False, "cap reached");
        Assert.That(manager.GetUnitCount(1), Is.EqualTo(TurnManager.MaxUnitsPerPlayer));

        manager.NextTurn();
        manager.NextTurn(); // leaves deploy phase
        Assert.That(manager.CanDeployUnit(2), Is.False, "deployment closed after turn 1");
    }

    [Test]
    public void StartTurn_OnlyResetsMovementForCurrentPlayer()
    {
        var manager = new TurnManager(new[] { 1, 2 });
        var ours = UnitCatalog.Create(UnitType.Tank, ownerId: 1, 0, 0);
        var theirs = UnitCatalog.Create(UnitType.Tank, ownerId: 2, 1, 1);
        ours.CurrentMovePoints = 0;
        ours.HasMoved = true;
        theirs.CurrentMovePoints = 0;
        theirs.HasMoved = true;

        manager.StartTurn(new[] { ours, theirs });

        Assert.Multiple(() =>
        {
            Assert.That(ours.CurrentMovePoints, Is.EqualTo(ours.MaxMovePoints));
            Assert.That(ours.HasMoved, Is.False);
            Assert.That(theirs.CurrentMovePoints, Is.EqualTo(0));
            Assert.That(theirs.HasMoved, Is.True);
        });
    }
}
