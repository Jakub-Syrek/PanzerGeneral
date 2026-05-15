using PanzerGeneral;
using System.Windows.Media;

namespace PanzerGeneral
{
    public static class TerrainCatalog
    {
        public static HexTile CreateTile(int column, int row, TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plain => new HexTile
                {
                    Column = column,
                    Row = row,
                    Terrain = terrain,
                    MoveCost = 1,
                    DefenseBonus = 0,
                    BlocksMovement = false,
                    BlocksLineOfSight = false,
                    IsObjective = false
                },

                TerrainType.City => new HexTile
                {
                    Column = column,
                    Row = row,
                    Terrain = terrain,
                    MoveCost = 1,
                    DefenseBonus = 3,
                    BlocksMovement = false,
                    BlocksLineOfSight = false,
                    IsObjective = true
                },

                TerrainType.Hills => new HexTile
                {
                    Column = column,
                    Row = row,
                    Terrain = terrain,
                    MoveCost = 2,
                    DefenseBonus = 2,
                    BlocksMovement = false,
                    BlocksLineOfSight = false,
                    IsObjective = false
                },

                TerrainType.Mountains => new HexTile
                {
                    Column = column,
                    Row = row,
                    Terrain = terrain,
                    MoveCost = 3,
                    DefenseBonus = 4,
                    BlocksMovement = false,
                    BlocksLineOfSight = true,
                    IsObjective = false
                },

                TerrainType.River => new HexTile
                {
                    Column = column,
                    Row = row,
                    Terrain = terrain,
                    MoveCost = 3,
                    DefenseBonus = -1,
                    BlocksMovement = false,
                    BlocksLineOfSight = false,
                    IsObjective = false
                },

                TerrainType.Water => new HexTile
                {
                    Column = column,
                    Row = row,
                    Terrain = terrain,
                    MoveCost = 99,
                    DefenseBonus = 0,
                    BlocksMovement = true,
                    BlocksLineOfSight = false,
                    IsObjective = false
                },

                _ => throw new ArgumentOutOfRangeException(nameof(terrain))
            };
        }

        public static int GetMoveCost(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plain => 1,
                TerrainType.City => 1,
                TerrainType.Hills => 2,
                TerrainType.Mountains => 3,
                TerrainType.River => 3,
                TerrainType.Water => 99,
                TerrainType.Forest => 2,
                TerrainType.Road => 1,
                TerrainType.Bridge => 1,
                TerrainType.Ruins => 2,
                TerrainType.Farm => 1,
                TerrainType.Swamp => 3,
                _ => 99
            };
        }

        public static int GetDefenseBonus(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plain => 0,
                TerrainType.City => 3,
                TerrainType.Hills => 2,
                TerrainType.Mountains => 4,
                TerrainType.River => -1,
                TerrainType.Water => 0,
                TerrainType.Forest => 2,
                TerrainType.Road => 0,
                TerrainType.Bridge => 0,
                TerrainType.Ruins => 1,
                TerrainType.Farm => 0,
                TerrainType.Swamp => 1,
                _ => 0
            };
        }

        public static bool BlocksMovement(TerrainType terrain)
        {
            return terrain == TerrainType.Water || terrain == TerrainType.Mountains;
        }

        public static bool BlocksLineOfSight(TerrainType terrain)
        {
            return terrain == TerrainType.Mountains;
        }

        public static bool IsObjective(TerrainType terrain)
        {
            return terrain == TerrainType.City;
        }

        public static Brush GetBrush(TerrainType terrain)
        {
            return TextureHelper.GetTextureBrush(terrain);
        }
    }
}