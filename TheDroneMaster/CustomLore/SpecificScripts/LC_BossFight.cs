using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.CustomLore.SpecificScripts
{
    public class RoomSpecificScriptPatch
    {
        public static void PatchOn()
        {
            On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;
        }

        private static void RoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig.Invoke(room);
            if(room.abstractRoom.name == "LC_FINAL" && room.game.IsStorySession && room.game.GetStorySession.saveState.saveStateNumber == new SlugcatStats.Name(Plugin.ID))
            {
                room.AddObject(new LC_BossFight(room));
            }
        }
    }

    public class LC_BossFight : UpdatableAndDeletable
    {
        public Player player;

        public bool triggeredBoss;
        public LC_BossFight(Room room)
        { 
            base.room = room;
            Plugin.Log("Add DroneMaster boss fight!");
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            AbstractCreature firstAlivePlayer = room.game.FirstAlivePlayer;
            if (firstAlivePlayer == null)
            {
                return;
            }

            if (DeathPersistentSaveDataPatch.GetUnitOfType<ScannedCreatureSaveUnit>().KingScanned)
            {
                Destroy();
                return;
            }


            if (player == null && room.game.Players.Count > 0 && firstAlivePlayer.realizedCreature != null && firstAlivePlayer.realizedCreature.room == room)
            {
                player = (firstAlivePlayer.realizedCreature as Player);
            }

            if (player != null && player.abstractCreature.Room == room.abstractRoom && player.room != null)
            {
                if (player.room.game.cameras[0] != null && player.room.game.cameras[0].currentCameraPosition == 0 && !player.sceneFlag)
                {
                    TriggerBossFight();
                }
            }
            else
            {
                player = null;
            }
        }

        public void TriggerBossFight()
        {
            if (!triggeredBoss)
            {
                Plugin.Log("Trigger DroneMaster boss fight!");
                //生成酋长尸体
                triggeredBoss = true;
                //player.sceneFlag = true;
                room.TriggerCombatArena();
                WorldCoordinate pos = new WorldCoordinate(room.abstractRoom.index, 122, 7, -1);
                AbstractCreature abstractCreature = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing), null, pos, room.game.GetNewID());
                abstractCreature.ID.setAltSeed(8875);
                abstractCreature.Die();
                abstractCreature.ignoreCycle = true;
                room.abstractRoom.AddEntity(abstractCreature);
                abstractCreature.RealizeInRoom();

                for(int i = 0; i< 15; i++)
                {
                    WorldCoordinate position = new WorldCoordinate(room.abstractRoom.index, 122 - i, 7, -1);
                    abstractCreature = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite), null, position, room.game.GetNewID());
                    abstractCreature.ignoreCycle = triggeredBoss;
                    room.abstractRoom.AddEntity(abstractCreature);
                    abstractCreature.RealizeInRoom();
                }
            }
        }
    }
}
