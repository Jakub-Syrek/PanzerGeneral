using System;
using System.Windows;
using System.Windows.Media;

namespace PanzerGeneral
{
    public static class HexHelper
    {
        public static Point HexToPixel(int q, int r, double size)
        {
            double x = size * Math.Sqrt(3) * (q + r / 2.0);
            double y = size * 1.5 * r;

            return new Point(x, y);
        }

        public static PointCollection GetHexPoints(Point center, double size)
        {
            var points = new PointCollection();

            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 180 * (60 * i - 30);

                points.Add(new Point(
                    center.X + size * Math.Cos(angle),
                    center.Y + size * Math.Sin(angle)
                ));
            }

            return points;
        }
    }
}
