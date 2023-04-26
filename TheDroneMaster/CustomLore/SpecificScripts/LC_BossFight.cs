using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.CustomLore.SpecificScripts
{
    public class LC_BossFight : UpdatableAndDeletable
    {
        public LC_BossFight(Room room)
        { 
            base.room = room;
        }
    }
}
