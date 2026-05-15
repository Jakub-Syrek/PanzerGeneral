using System;

namespace PanzerGeneral
{
    public class HexTile
    {
        public int Column { get; set; }
        public int Row { get; set; }
        public TerrainType Terrain { get; set; }
        public int MoveCost { get; set; }
        public int DefenseBonus { get; set; }
        public bool BlocksMovement { get; set; }
        public bool BlocksLineOfSight { get; set; }
        public bool IsObjective { get; set; }
    }
}