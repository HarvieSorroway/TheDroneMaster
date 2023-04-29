using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using Random = UnityEngine.Random;


namespace TheDroneMaster
{
    public class EnemyCreator
    {
        public static string header = "ENEMYCREATOR";

        public static readonly int creatureLimit = 200;

        public bool created = true;
        public PlayerPatchs.PlayerModule module;

        public int genWaitCounter = 1000;

        public Region lastRegion;
        public EnemyCreator(PlayerPatchs.PlayerModule module)
        {
            this.module = module;
        }

        public static CreatureTemplate.Type GetUperAndBetterType(CreatureTemplate.Type origType)
        {
            CreatureTemplate.Type result = null;

            if (origType == CreatureTemplate.Type.SmallCentipede)
            {
                if (Random.value < 0.9f) result = CreatureTemplate.Type.Centipede;
                else result = CreatureTemplate.Type.RedCentipede;
            }
            else if (origType == CreatureTemplate.Type.Centipede)
            {
                if (Random.value < 0.4f) result = CreatureTemplate.Type.Centipede;
                else result = CreatureTemplate.Type.RedCentipede;
            }
            else if (origType == CreatureTemplate.Type.RedCentipede) result = CreatureTemplate.Type.RedCentipede;
            else if (origType == CreatureTemplate.Type.RedLizard) result = origType;
            else if (origType == CreatureTemplate.Type.GreenLizard) result = MoreSlugcatsEnums.CreatureTemplateType.SpitLizard;
            else if (origType == CreatureTemplate.Type.PinkLizard || origType == CreatureTemplate.Type.BlueLizard) result = CreatureTemplate.Type.CyanLizard;
            else if (origType == CreatureTemplate.Type.CyanLizard) result = CreatureTemplate.Type.CyanLizard;
            else if (origType == CreatureTemplate.Type.WhiteLizard) result = CreatureTemplate.Type.WhiteLizard;
            else if (origType == CreatureTemplate.Type.Salamander)
            {
                if (Random.value < 0.5f) result = origType;
                else result = MoreSlugcatsEnums.CreatureTemplateType.EelLizard;
            }
            else if (StaticWorld.GetCreatureTemplate(origType).ancestor != null && StaticWorld.GetCreatureTemplate(origType).ancestor.type == CreatureTemplate.Type.LizardTemplate)
            {
                if (Random.value < 0.3f) result = MoreSlugcatsEnums.CreatureTemplateType.TrainLizard;
                else result = CreatureTemplate.Type.RedLizard;
            }
            else if (origType == CreatureTemplate.Type.Scavenger) result = MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite;
            else if (origType == CreatureTemplate.Type.BigSpider) result = CreatureTemplate.Type.SpitterSpider;
            else if (origType == CreatureTemplate.Type.Vulture)
            {
                if (Random.value < 0.3f) result = MoreSlugcatsEnums.CreatureTemplateType.MirosVulture;
                else result = CreatureTemplate.Type.KingVulture;
            }
            else if (origType == CreatureTemplate.Type.KingVulture) result = origType;
            else if (origType == MoreSlugcatsEnums.CreatureTemplateType.MirosVulture) result = origType;
            else if (origType == CreatureTemplate.Type.MirosBird) result = origType;
            else if (origType == CreatureTemplate.Type.BrotherLongLegs) result = CreatureTemplate.Type.DaddyLongLegs;
            else if (origType == CreatureTemplate.Type.DaddyLongLegs) result = MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs;
            else if (origType == CreatureTemplate.Type.SmallNeedleWorm) result = CreatureTemplate.Type.BigNeedleWorm;
            else if (origType == CreatureTemplate.Type.DropBug)
            {
                if (Random.value < 0.2f) result = MoreSlugcatsEnums.CreatureTemplateType.StowawayBug;
                else result = origType;
            }
            else if (origType == CreatureTemplate.Type.EggBug) result = MoreSlugcatsEnums.CreatureTemplateType.FireBug;

            return result;
        }

        public static bool IgnoreThisType(CreatureTemplate.Type type)
        {
            return (type == CreatureTemplate.Type.Spider) || 
                (type == CreatureTemplate.Type.Leech) || 
                (type == CreatureTemplate.Type.SeaLeech) ||
                (type == CreatureTemplate.Type.Overseer) || 
                (type == CreatureTemplate.Type.Fly);
        }

        public void Update()
        {
            if(module.playerRef.TryGetTarget(out var player))
            {
                if (player.room == null) return;
                if(player.room.world.region == null)
                {
                    lastRegion = null;
                    return;
                }

                if (genWaitCounter > 0 && player.room.world.abstractRooms.Length > 0 && player.room.world.abstractRooms[0].world == player.room.world) genWaitCounter--;
                if (!created && genWaitCounter == 0)
                {
                    Plugin.Log("Spawn more enemies");
                    World world = player.abstractCreature.world;

                    int totalCreatureInRegin = 0;
                    List<AbstractCreature> abstractCreaturesToAdd = new List<AbstractCreature>();
                    Dictionary<AbstractCreature, AbstractRoom> cretToRoom = new Dictionary<AbstractCreature, AbstractRoom>();
                    foreach (var abRoom in world.abstractRooms)
                    {
                        if (!abRoom.shelter && !abRoom.gate)
                        {
                            if (abRoom.entities.Count > 0)
                            {
                                AbstractWorldEntity[] entityCopy = new AbstractWorldEntity[abRoom.entities.Count];
                                abRoom.entities.CopyTo(entityCopy);
                                foreach (var entity in entityCopy)
                                {
                                    if (totalCreatureInRegin > creatureLimit) break;
                                    if (entity is AbstractCreature)
                                    {
                                        if (IgnoreThisType((entity as AbstractCreature).creatureTemplate.type)) continue;
                                        totalCreatureInRegin++;
                                        //Plugin.Log("GetAbstractCreature in " + abRoom.name + " : " + entity.ToString());
                                        var newCreature = SpawnUperCreature(entity as AbstractCreature);

                                        if (newCreature != null)
                                        {
                                            abstractCreaturesToAdd.Add(newCreature);
                                            cretToRoom.Add(newCreature, abRoom);
                                            totalCreatureInRegin++;
                                        }
                                    }
                                }
                            }
                            if (abRoom.entitiesInDens.Count > 0)
                            {
                                AbstractWorldEntity[] entityCopy = new AbstractWorldEntity[abRoom.entitiesInDens.Count];
                                abRoom.entitiesInDens.CopyTo(entityCopy);
                                foreach (var entity in entityCopy)
                                {
                                    if (totalCreatureInRegin > creatureLimit) break;
                                    if (entity is AbstractCreature)
                                    {
                                        if (IgnoreThisType((entity as AbstractCreature).creatureTemplate.type)) continue;
                                        totalCreatureInRegin++;
                                        //Plugin.Log("GetAbstractCreature in den of " + abRoom.name + " : " + entity.ToString());
                                        var newCreature = SpawnUperCreature(entity as AbstractCreature);

                                        if (newCreature != null)
                                        {
                                            abstractCreaturesToAdd.Add(newCreature);
                                            cretToRoom.Add(newCreature, abRoom);
                                            totalCreatureInRegin++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (abstractCreaturesToAdd.Count > 0)
                    {
                        foreach (var creature in abstractCreaturesToAdd)
                        {
                            Plugin.Log("Spawn new enemy of type:" + creature.creatureTemplate.type.ToString() + " in room:" + cretToRoom[creature].name);
                            
                            AbstractRoom abRoom = cretToRoom[creature];
                            abRoom.AddEntity(creature);
                            if (abRoom.realizedRoom != null)
                            {
                                creature.RealizeInRoom();
                            }
                        }
                    }
                    created = true;
                }

                if (player.room.world.region != lastRegion && !(DeathPersistentSaveDataPatch.GetUnitOfHeader(header) as EnemyCreatorSaveUnit).isThisRegionSpawnOrNot(player.room.world.region))
                {
                    lastRegion = player.room.world.region;
                    (DeathPersistentSaveDataPatch.GetUnitOfHeader(header) as EnemyCreatorSaveUnit).SpawnEnemyInNewRegion(lastRegion);
                    Plugin.Log("Spawn Enemies in new Region of name:" + lastRegion.name);
                    created = false;
                    genWaitCounter = 200;
                }
            }
        }

        public AbstractCreature SpawnUperCreature(AbstractCreature origCreature)
        {
            AbstractRoom abRoom = origCreature.Room;
            World world = abRoom.world;
            CreatureTemplate.Type type = EnemyCreator.GetUperAndBetterType(origCreature.creatureTemplate.type);
            if (type == null) return null;

            WorldCoordinate pos = origCreature.pos;
            AbstractCreature abstractCreature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, pos, world.game.GetNewID());
            return abstractCreature;
        }
    }
}
