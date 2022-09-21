using System;
using System.Collections.Generic;
using System.Drawing;

namespace TeamsHack
{
    public static class Areas
    {
        private static Area area;
        public static List<Area> GetAreas()
        {
            return new List<Area>()
            {
                area
            };
        }

        public static void SetArea(Rectangle desktopBounds)
        {
            area = Area.FromRectangle(desktopBounds);
        }


    }
}
