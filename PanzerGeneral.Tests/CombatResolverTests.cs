using NUnit.Framework;
using PanzerGeneral;
using PanzerGeneral.Game;

namespace PanzerGeneral.Tests;

/// <summary>
/// Tests for the deterministic combat damage formula in <see cref="CombatResolver"/>.
/// </summary>
[TestFixture]
public class CombatResolverTests
{
    /// <summary>
    /// Builds a unit with the supplied attack, defense, and HP for use in combat tests.
    /// </summary>
    private static Unit MakeUnit(int attack, int defense, int hp)
    {
        return new Unit
        {
            Attack = attack,
            Defense = defense,
            HP = hp,
            MaxHP = hp
        };
    }

    /// <summary>
    /// Builds a tile with the supplied terrain defense bonus.
    /// </summary>
    private static HexTile MakeTile(int defenseBonus)
    {
        return new HexTile { DefenseBonus = defenseBonus };
    }

    [Test]
    public void Resolve_AttackerOvercomesDefense_AppliesAttackMinusDefenseDamage()
    {
        var attacker = MakeUnit(attack: 7, defense: 0, hp: 10);
        var defender = MakeUnit(attack: 0, defense: 2, hp: 10);
        var tile = MakeTile(defenseBonus: 1);

        bool killed = CombatResolver.Resolve(attacker, defender, tile);

        Assert.That(killed, Is.False);
        Assert.That(defender.HP, Is.EqualTo(6), "10 HP - max(1, 7 - (2 + 1)) = 6");
    }

    [Test]
    public void Resolve_DefenseExceedsAttack_StillDealsOneDamage()
    {
        var attacker = MakeUnit(attack: 2, defense: 0, hp: 10);
        var defender = MakeUnit(attack: 0, defense: 5, hp: 10);
        var tile = MakeTile(defenseBonus: 3);

        bool killed = CombatResolver.Resolve(attacker, defender, tile);

        Assert.That(killed, Is.False);
        Assert.That(defender.HP, Is.EqualTo(9), "minimum damage is 1");
    }

    [Test]
    public void Resolve_HpDropsToZeroOrBelow_ReturnsTrue()
    {
        var attacker = MakeUnit(attack: 9, defense: 0, hp: 10);
        var defender = MakeUnit(attack: 0, defense: 1, hp: 5);
        var tile = MakeTile(defenseBonus: 0);

        bool killed = CombatResolver.Resolve(attacker, defender, tile);

        Assert.That(killed, Is.True);
        Assert.That(defender.HP, Is.LessThanOrEqualTo(0));
    }

    [Test]
    public void Resolve_NegativeTerrainBonus_IncreasesDamageTaken()
    {
        var attacker = MakeUnit(attack: 3, defense: 0, hp: 10);
        var defender = MakeUnit(attack: 0, defense: 2, hp: 10);
        var river = MakeTile(defenseBonus: -1);

        CombatResolver.Resolve(attacker, defender, river);

        Assert.That(defender.HP, Is.EqualTo(8), "river penalty turns 2 defense into 1");
    }
}
