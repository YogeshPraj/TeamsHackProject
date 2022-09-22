using System;
using System.Collections.Generic;
using System.Drawing;

namespace TeamsHack
{
    public static class Areas
    {
        private static Area area;
        public static List<Area> areas = new List<Area>();
        public static List<Area> GetAreas()
        {
            return areas;
        }

        public static void SetArea(Rectangle desktopBounds)
        {
            area = Area.FromRectangle(desktopBounds);
            area.Z = -5;
            area.IsShared = true;
        }


    }
}
