using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.DMPSDrone
{
    internal class AbstractDMPSDrone : AbstractCreature
    {
        public bool addByPort;
        public AbstractDMPSDrone(World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID) : base(world, creatureTemplate, realizedCreature, pos, ID)
        {
        }

        public override void Realize()
        {
            if (!addByPort)
            {
                Destroy();
                return;
            }
            base.Realize();
        }

        public override void Abstractize(WorldCoordinate coord)
        {
            base.Abstractize(coord);
        }
    }
}
