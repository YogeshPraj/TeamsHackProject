using System;
using System.Drawing;

namespace TeamsHack
{
    public class Area
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        internal static Area FromRectangle(Rectangle rectangle)
        {
            return new Area()
            {
                X = rectangle.X,
                Y = rectangle.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }

        internal Rectangle GetRectangle()
        {
            return new Rectangle(X, Y, Width, Height);
        }
    }
}
