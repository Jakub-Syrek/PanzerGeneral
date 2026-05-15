using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace PanzerGeneral
{
    public static class UnitCatalog
    {
        private static readonly Dictionary<string, BitmapImage> _imageCache = new();

        public static BitmapImage GetCachedImage(UnitType type, int ownerId)
        {
            var path = GetImage(type, ownerId);
            if (_imageCache.TryGetValue(path, out var cached))
                return cached;

            var bitmap = new BitmapImage(new Uri(path, UriKind.Absolute));
            bitmap.Freeze();
            _imageCache[path] = bitmap;
            return bitmap;
        }

        public static string GetImage(UnitType type, int ownerId)
        {
            return type switch
            {
                UnitType.Infantry => "pack://siteoforigin:,,,/Resources/infantry_allied.png",
                UnitType.Mechanized => "pack://siteoforigin:,,,/Resources/mechanized_allied.png",
                UnitType.Tank => "pack://siteoforigin:,,,/Resources/tank_allied.png",
                _ => string.Empty
            };
        }

        public static Unit Create(UnitType type, int ownerId, int column, int row)
        {
            bool facingLeft = ownerId == 2; // gracz 2 jedzie w lewo

            return type switch
            {
                UnitType.Infantry => new Unit
                {
                    Name = "Piechota",
                    Type = type,
                    OwnerId = ownerId,
                    Column = column,
                    Row = row,
                    Attack = 3,
                    Defense = 4,
                    MaxMovePoints = 2,
                    CurrentMovePoints = 2,
                    MaxHP = 10,
                    HP = 10,
                    FacingLeft = facingLeft
                },

                UnitType.Mechanized => new Unit
                {
                    Name = "Zmechanizowana",
                    Type = type,
                    OwnerId = ownerId,
                    Column = column,
                    Row = row,
                    Attack = 5,
                    Defense = 5,
                    MaxMovePoints = 4,
                    CurrentMovePoints = 4,
                    MaxHP = 12,
                    HP = 12,
                    FacingLeft = facingLeft
                },

                UnitType.Tank => new Unit
                {
                    Name = "Czołgi",
                    Type = type,
                    OwnerId = ownerId,
                    Column = column,
                    Row = row,
                    Attack = 7,
                    Defense = 6,
                    MaxMovePoints = 5,
                    CurrentMovePoints = 5,
                    MaxHP = 15,
                    HP = 15,
                    FacingLeft = facingLeft
                },

                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static string GetSymbol(UnitType type)
        {
            return type switch
            {
                UnitType.Infantry => "P",
                UnitType.Mechanized => "Z",
                UnitType.Tank => "T",
                _ => "?"
            };
        }
    }
}