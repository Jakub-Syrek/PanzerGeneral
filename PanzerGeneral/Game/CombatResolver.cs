using PanzerGeneral;
using System;

namespace PanzerGeneral.Game
{
    public static class CombatResolver
    {
        // zwraca true jeśli obrońca zabity
        public static bool Resolve(Unit attacker, Unit defender, HexTile defenderTile)
        {
            int attackValue = attacker.Attack;
            int defenseValue = defender.Defense + defenderTile.DefenseBonus;

            int damage = Math.Max(1, attackValue - defenseValue);
            defender.HP -= damage;

            return defender.HP <= 0;
        }
    }
}