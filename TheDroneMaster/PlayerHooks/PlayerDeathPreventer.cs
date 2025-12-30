using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public class PlayerDeathPreventer : PlayerModule.PlayerModuleUtil
    {
        public PlayerModule module;
        int acceptableDamageCount = 2;
        public int DeathPreventCounter;

        public int AcceptableDamageCount
        {
            get => acceptableDamageCount;
            set
            {
                acceptableDamageCount = value;
                module.SyncAcceptableDamage(value);
            }
        }

        public PlayerDeathPreventer(PlayerModule module)
        {
            this.module = module;
            if(module.stateOverride != null)
                acceptableDamageCount = Mathf.Min(2,module.stateOverride.overrideHealth);
        }

        public override void Update(Player player)
        {
            if (DeathPreventCounter > 0) DeathPreventCounter--;
        }


        public bool canTakeDownThisDamage(Player player,string callFrom)
        {
            if (Plugin.SkinOnly)
                return false;

            bool result = false;
            bool deathExplosion = AcceptableDamageCount >= 0;
            Plugin.Log("" + acceptableDamageCount.ToString() + deathExplosion.ToString() + result.ToString() + DeathPreventCounter.ToString());

            if (AcceptableDamageCount > 0) result = true;

            if (DeathPreventCounter > 0 && AcceptableDamageCount > -1)
            {
                result = true;
            }
            else AcceptableDamageCount--;

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

            if (player.room != null && player.room.waterObject != null && player.room.waterObject.WaterIsLethal) 
                result = false;

            if (player.drown >= 1f)
                result = false;

            if (player.rainDeath >= 1f)
                result = false;

            DeathPreventCounter = 5;

            if (deathExplosion && module is DroneMasterModule)
            {
                (module as DroneMasterModule).portGraphics.DeathShock("Death Preventer");
            }

            if (!result)
            {
                if(module is DroneMasterModule)
                    (module as DroneMasterModule).port.ClearOutAllDrones();
                AcceptableDamageCount = -1;
            }
            Plugin.Log(acceptableDamageCount.ToString () + deathExplosion.ToString() + result.ToString() + DeathPreventCounter.ToString());
            return result;
        }
    }
}
