using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public static class Fixer
    {
        //sometimes save in shelter may cause game to freeze, this may sovle the problem.
        public static void Patch()
        {
            On.RegionState.AdaptRegionStateToWorld += RegionState_AdaptRegionStateToWorld;
        }


        private static void RegionState_AdaptRegionStateToWorld(On.RegionState.orig_AdaptRegionStateToWorld orig, RegionState self, int playerShelter, int activeGate)
        {
            if (self.savedObjects == null) self.savedObjects = new List<string>();
            if (self.unrecognizedSavedObjects == null) self.unrecognizedSavedObjects = new List<string>();
            if (self.savedPopulation == null) self.savedPopulation = new List<string>();

            try
            {
                orig.Invoke(self, playerShelter, activeGate);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Plugin.Log("Region State adapt to world failed");
            }
        }

    }
}
