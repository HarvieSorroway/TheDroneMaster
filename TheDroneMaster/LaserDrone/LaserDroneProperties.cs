using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Fisobs.Properties;

namespace TheDroneMaster
{
    sealed class LaserDroneProperties : ItemProperties
    {
        private readonly WeakReference<LaserDrone> drone = new WeakReference<LaserDrone>(null);

        public LaserDroneProperties(LaserDrone drone)
        {
            this.drone.SetTarget(drone);
        }

        public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
        {
            grabability = Player.ObjectGrabability.TwoHands;
        }
    }
}
