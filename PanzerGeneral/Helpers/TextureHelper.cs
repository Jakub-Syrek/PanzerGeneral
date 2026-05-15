using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PanzerGeneral
{
    public static class TextureHelper
    {
        private static readonly Dictionary<TerrainType, string> TexturePaths = new()
        {
            { TerrainType.Plain, "pack://application:,,,/Assets/Textures/plain.png" },
            { TerrainType.City, "pack://application:,,,/Assets/Textures/city.png" },
            { TerrainType.Hills, "pack://application:,,,/Assets/Textures/hills.png" },
            { TerrainType.Mountains, "pack://application:,,,/Assets/Textures/mountains.png" },
            { TerrainType.River, "pack://application:,,,/Assets/Textures/river.png" },
            { TerrainType.Water, "pack://application:,,,/Assets/Textures/water.png" },
            { TerrainType.Forest, "pack://application:,,,/Assets/Textures/forest.png" }
        };

        private static readonly Dictionary<TerrainType, Brush> _cache = new();
        private static readonly HashSet<TerrainType> _missingTextures = new();

        public static Brush GetTextureBrush(TerrainType terrain)
        {
            if (_cache.TryGetValue(terrain, out var cached))
                return cached;

            Brush brush = TryLoadTexture(terrain) ?? GetFallbackBrush(terrain);
            _cache[terrain] = brush;
            return brush;
        }

        private static Brush? TryLoadTexture(TerrainType terrain)
        {
            if (_missingTextures.Contains(terrain))
                return null;

            if (!TexturePaths.TryGetValue(terrain, out var path))
            {
                _missingTextures.Add(terrain);
                return null;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                var brush = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
                brush.Freeze();
                return brush;
            }
            catch
            {
                _missingTextures.Add(terrain);
                return null;
            }
        }

        private static Brush GetFallbackBrush(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plain      => Freeze(new SolidColorBrush(Color.FromRgb(135, 165, 75))),
                TerrainType.City       => Freeze(new SolidColorBrush(Color.FromRgb(110, 110, 120))),
                TerrainType.Hills      => Freeze(new SolidColorBrush(Color.FromRgb(148, 128, 68))),
                TerrainType.Mountains  => Freeze(new SolidColorBrush(Color.FromRgb(105, 88, 72))),
                TerrainType.River      => Freeze(new SolidColorBrush(Color.FromRgb(65, 125, 185))),
                TerrainType.Water      => Freeze(new SolidColorBrush(Color.FromRgb(30, 85, 160))),
                TerrainType.Forest     => Freeze(new SolidColorBrush(Color.FromRgb(28, 78, 28))),
                TerrainType.Road       => Freeze(new SolidColorBrush(Color.FromRgb(170, 150, 105))),
                TerrainType.Bridge     => Freeze(new SolidColorBrush(Color.FromRgb(140, 120, 88))),
                TerrainType.Ruins      => Freeze(new SolidColorBrush(Color.FromRgb(105, 95, 80))),
                TerrainType.Farm       => Freeze(new SolidColorBrush(Color.FromRgb(195, 170, 75))),
                TerrainType.Swamp      => Freeze(new SolidColorBrush(Color.FromRgb(65, 88, 50))),
                _ => Brushes.Magenta
            };
        }

        private static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }
    }
}