using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    internal static class StaticColors
    {
        public static Color defaultLaserColor = new Color(1f, 0.26f, 0.45f);
        public static Color defaultDarkLaserColor = Custom.hexToColor("3C001D");

        public static class Menu
        {
            public static Color pink = Custom.hexToColor("800029");
            public static Color darkPink = Custom.hexToColor("30000E");

        }
    }
}
