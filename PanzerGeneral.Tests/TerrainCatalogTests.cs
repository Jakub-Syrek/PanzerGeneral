using NUnit.Framework;
using PanzerGeneral;

namespace PanzerGeneral.Tests;

/// <summary>
/// Tests for terrain definitions exposed by <see cref="TerrainCatalog"/>.
/// </summary>
[TestFixture]
public class TerrainCatalogTests
{
    [Test]
    public void CreateTile_City_IsObjectiveWithDefenseBonus()
    {
        var tile = TerrainCatalog.CreateTile(2, 3, TerrainType.City);

        Assert.Multiple(() =>
        {
            Assert.That(tile.Column, Is.EqualTo(2));
            Assert.That(tile.Row, Is.EqualTo(3));
            Assert.That(tile.Terrain, Is.EqualTo(TerrainType.City));
            Assert.That(tile.IsObjective, Is.True);
            Assert.That(tile.DefenseBonus, Is.EqualTo(3));
            Assert.That(tile.BlocksMovement, Is.False);
        });
    }

    [Test]
    public void CreateTile_Water_BlocksMovementWithSentinelCost()
    {
        var tile = TerrainCatalog.CreateTile(0, 0, TerrainType.Water);

        Assert.That(tile.BlocksMovement, Is.True);
        Assert.That(tile.MoveCost, Is.EqualTo(99));
    }

    [Test]
    public void GetMoveCost_OrdersTerrainByExpectedCosts()
    {
        int plain = TerrainCatalog.GetMoveCost(TerrainType.Plain);
        int hills = TerrainCatalog.GetMoveCost(TerrainType.Hills);
        int mountains = TerrainCatalog.GetMoveCost(TerrainType.Mountains);

        Assert.That(plain, Is.LessThan(hills));
        Assert.That(hills, Is.LessThan(mountains));
        Assert.That(TerrainCatalog.GetMoveCost(TerrainType.Road), Is.EqualTo(1));
        Assert.That(TerrainCatalog.GetMoveCost(TerrainType.Swamp), Is.EqualTo(3));
    }

    [Test]
    public void GetDefenseBonus_MountainsBeatHillsBeatPlain()
    {
        int plain = TerrainCatalog.GetDefenseBonus(TerrainType.Plain);
        int hills = TerrainCatalog.GetDefenseBonus(TerrainType.Hills);
        int mountains = TerrainCatalog.GetDefenseBonus(TerrainType.Mountains);

        Assert.That(plain, Is.EqualTo(0));
        Assert.That(hills, Is.GreaterThan(plain));
        Assert.That(mountains, Is.GreaterThan(hills));
        Assert.That(TerrainCatalog.GetDefenseBonus(TerrainType.River), Is.LessThan(0));
    }

    [Test]
    public void BlocksMovement_OnlyWaterAndMountainsAreImpassable()
    {
        Assert.That(TerrainCatalog.BlocksMovement(TerrainType.Water), Is.True);
        Assert.That(TerrainCatalog.BlocksMovement(TerrainType.Mountains), Is.True);
        Assert.That(TerrainCatalog.BlocksMovement(TerrainType.Plain), Is.False);
        Assert.That(TerrainCatalog.BlocksMovement(TerrainType.Forest), Is.False);
    }

    [Test]
    public void IsObjective_OnlyCityCountsAsObjective()
    {
        Assert.That(TerrainCatalog.IsObjective(TerrainType.City), Is.True);
        Assert.That(TerrainCatalog.IsObjective(TerrainType.Plain), Is.False);
        Assert.That(TerrainCatalog.IsObjective(TerrainType.Hills), Is.False);
    }
}
