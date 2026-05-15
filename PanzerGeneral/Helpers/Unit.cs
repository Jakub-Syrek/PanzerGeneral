using System;

namespace PanzerGeneral
{
    public class Unit
    {
        public string Name { get; set; } = "";
        public UnitType Type { get; set; }
        public int OwnerId { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }

        public int Attack { get; set; }
        public int Defense { get; set; }
        public int MaxMovePoints { get; set; }
        public int CurrentMovePoints { get; set; }
        public int MaxHP { get; set; } = 10;
        public int HP { get; set; } = 10;
        public bool HasMoved { get; set; } = false;
        public bool FacingLeft { get; set; } = false;
    }
}