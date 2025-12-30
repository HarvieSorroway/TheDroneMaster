using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster
{
    internal static class DMEnums
    {
        public static class SlugStateName
        {
            public static SlugcatStats.Name DroneMaster = new SlugcatStats.Name("thedronemaster");
            public static SlugcatStats.Name DMPS = new SlugcatStats.Name("dmps");
        }

        public static class PlayerAnimationIndex
        {
            public static Player.AnimationIndex DMFlip = new Player.AnimationIndex("DMFlip", true);
        }

        public static class CreatureTemplateType
        {
            public static CreatureTemplate.Type DMPSDrone = new CreatureTemplate.Type("DMPSDrone", true);
        }
    }
}
