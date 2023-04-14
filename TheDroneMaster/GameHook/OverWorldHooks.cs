using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.GameHooks
{
    public class OverWorldHooks
    {
        public static void PatchOn()
        {
            On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
            On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;
        }

        private static void WorldLoader_CreatingWorld(On.WorldLoader.orig_CreatingWorld orig, WorldLoader self)
        {
            if(self.game != null && RainWorldGamePatch.modules.TryGetValue(self.game, out var gameModule))
            {
                if(gameModule.IsDroneMasterDream)
                {
                    self.world.spawners = self.spawners.ToArray();
                    List<World.Lineage> list = new List<World.Lineage>();
                    for (int i = 0; i < self.spawners.Count; i++)
                    {
                        if (self.spawners[i] is World.Lineage)
                        {
                            list.Add(self.spawners[i] as World.Lineage);
                        }
                    }
                    self.world.lineages = list.ToArray();
                    if (self.loadContext == WorldLoader.LoadingContext.FASTTRAVEL || self.loadContext == WorldLoader.LoadingContext.MAPMERGE)
                    {
                        self.world.LoadWorldForFastTravel(self.playerCharacter, self.abstractRooms, self.swarmRoomsList.ToArray(), self.sheltersList.ToArray(), self.gatesList.ToArray());
                    }
                    else
                    {
                        self.world.LoadWorld(self.playerCharacter, self.abstractRooms, self.swarmRoomsList.ToArray(), self.sheltersList.ToArray(), self.gatesList.ToArray());
                        self.creatureStats[0] = (float)self.world.NumberOfRooms;
                        self.creatureStats[1] = (float)self.world.spawners.Length;
                    }
                    self.fliesMigrationBlockages = new int[self.tempBatBlocks.Count, 2];
                    for (int j = 0; j < self.tempBatBlocks.Count; j++)
                    {
                        int num = (self.world.GetAbstractRoom(self.tempBatBlocks[j].fromRoom) == null) ? -1 : self.world.GetAbstractRoom(self.tempBatBlocks[j].fromRoom).index;
                        int num2 = (self.world.GetAbstractRoom(self.tempBatBlocks[j].destRoom) == null) ? -1 : self.world.GetAbstractRoom(self.tempBatBlocks[j].destRoom).index;
                        self.fliesMigrationBlockages[j, 0] = num;
                        self.fliesMigrationBlockages[j, 1] = num2;
                    }

                    return;
                }
            }
            orig.Invoke(self);
        }

        private static void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            if(ProcessManagerPatch.current.droneMasterDreamNumber != -1)
            {   
                string room = "DMD_AI";
                self.game.startingRoom = room;
                self.LoadWorld("DMD", self.PlayerCharacterNumber, false);
                self.FIRSTROOM = room;

                Plugin.Log("OverWorld load room");
                return;
            }
            orig.Invoke(self);
        }
    }
}
