using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public class PlayerDeathPreventer
    {
        public PlayerPatchs.PlayerModule module;
        int acceptableDamageCount = 2;
        public int DeathPreventCounter;

        public int AcceptableDamageCount
        {
            get => acceptableDamageCount;
            set
            {
                acceptableDamageCount = value;
                module.metalGills.acceptableDamage = value;
            }
        }

        public PlayerDeathPreventer(PlayerPatchs.PlayerModule module)
        {
            this.module = module;
        }

        public void Update()
        {
            if (DeathPreventCounter > 0) DeathPreventCounter--;
        }


        public bool canTakeDownThisDamage(Player player,string callFrom)
        {
   
            bool result = false;
            bool deathExplosion = AcceptableDamageCount >= 0;
            Plugin.Log("" + acceptableDamageCount.ToString() + deathExplosion.ToString() + result.ToString() + DeathPreventCounter.ToString());

            if (AcceptableDamageCount > 0) result = true;

            try
            {
                if (player.grabbedBy != null)
                {
                    for (int i = player.grabbedBy.Count - 1; i >= 0; i--)
                    {
                        if (i > player.grabbedBy.Count - 1) continue;
                        if (player.grabbedBy[i].grabber is TentaclePlant || player.grabbedBy[i].grabber is PoleMimic)
                        {
                            result = false;
                            deathExplosion = false;

                            Plugin.Log("Grab by tentaclePlant or PoleMimic");
                            break;
                        }
                    }
                }
            }
            catch
            {
            }

            for (int i = CreaturePatchs.wormGrassTracesr.Count - 1; i >= 0; i--)
            {
                if (CreaturePatchs.wormGrassTracesr[i].Creature == null) continue;
                if (CreaturePatchs.wormGrassTracesr[i].Creature.worms == null || CreaturePatchs.wormGrassTracesr[i].Creature.worms.Count == 0) continue;
                foreach(var worm in CreaturePatchs.wormGrassTracesr[i].Creature.worms)
                {
                    if(worm.attachedChunk != null && worm.attachedChunk.owner == player)
                    {
                        result = false;
                        //deathExplosion = false;
                    }
                }
            }

            for (int i = CreaturePatchs.bulbTracers.Count - 1; i >= 0; i--)
            {
                if (CreaturePatchs.bulbTracers[i].Creature == null) continue;
                if (CreaturePatchs.bulbTracers[i].Creature.eatChunk == null) continue;
                if(CreaturePatchs.bulbTracers[i].Creature.eatChunk.owner == player)
                {
                    result = false;
                }
            }


            if (DeathPreventCounter > 0)
            {
                result = true;
            }
            else AcceptableDamageCount--;
            DeathPreventCounter = 5;

            if (deathExplosion)
            {
                module.portGraphics.DeathShock("Death Preventer");
            }

            if (!result)
            {
                module.port.ClearOutAllDrones();
            }
            Plugin.Log(acceptableDamageCount.ToString () + deathExplosion.ToString() + result.ToString() + DeathPreventCounter.ToString());
            return result;
        }
    }
}
