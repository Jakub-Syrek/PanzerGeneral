using NUnit.Framework;
using PanzerGeneral;

namespace PanzerGeneral.Tests;

/// <summary>
/// Tests for the unit factory and stat lookup in <see cref="UnitCatalog"/>.
/// </summary>
[TestFixture]
public class UnitCatalogTests
{
    [Test]
    public void Create_TankHasHighestAttackAndMovement()
    {
        var infantry = UnitCatalog.Create(UnitType.Infantry, ownerId: 1, 0, 0);
        var mech = UnitCatalog.Create(UnitType.Mechanized, ownerId: 1, 0, 0);
        var tank = UnitCatalog.Create(UnitType.Tank, ownerId: 1, 0, 0);

        Assert.That(tank.Attack, Is.GreaterThan(mech.Attack));
        Assert.That(mech.Attack, Is.GreaterThan(infantry.Attack));
        Assert.That(tank.MaxMovePoints, Is.GreaterThan(infantry.MaxMovePoints));
        Assert.That(tank.MaxHP, Is.GreaterThan(infantry.MaxHP));
    }

    [Test]
    public void Create_PlayerTwoUnitsFaceLeft()
    {
        var p1 = UnitCatalog.Create(UnitType.Tank, ownerId: 1, 0, 0);
        var p2 = UnitCatalog.Create(UnitType.Tank, ownerId: 2, 0, 0);

        Assert.That(p1.FacingLeft, Is.False);
        Assert.That(p2.FacingLeft, Is.True);
    }

    [Test]
    public void Create_NewUnitStartsAtFullHpAndFullMovement()
    {
        var unit = UnitCatalog.Create(UnitType.Mechanized, ownerId: 1, 4, 5);

        Assert.Multiple(() =>
        {
            Assert.That(unit.HP, Is.EqualTo(unit.MaxHP));
            Assert.That(unit.CurrentMovePoints, Is.EqualTo(unit.MaxMovePoints));
            Assert.That(unit.HasMoved, Is.False);
            Assert.That(unit.Column, Is.EqualTo(4));
            Assert.That(unit.Row, Is.EqualTo(5));
            Assert.That(unit.OwnerId, Is.EqualTo(1));
        });
    }

    [Test]
    public void GetSymbol_ReturnsDistinctSymbolPerUnitType()
    {
        string infantry = UnitCatalog.GetSymbol(UnitType.Infantry);
        string mech = UnitCatalog.GetSymbol(UnitType.Mechanized);
        string tank = UnitCatalog.GetSymbol(UnitType.Tank);

        Assert.That(new[] { infantry, mech, tank }, Is.Unique);
        Assert.That(infantry, Is.Not.Empty);
    }
}
