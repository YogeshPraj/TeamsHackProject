using System;
using System.Drawing;

namespace TeamsHack
{
    public class Area : IComparable<Area>   
    {
        public IntPtr Hwnd { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsShared { get; set; }

        public static Area FromRectangle(Rectangle rectangle)
        {
            return new Area()
            {
                X = rectangle.X,
                Y = rectangle.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }

        public int CompareTo(Area other)
        {
            // assuming 0 is bottom
            return this.Z - other.Z;
        }

        internal Rectangle GetRectangle()
        {
            return new Rectangle(X, Y, Width, Height);
        }
    }
}
