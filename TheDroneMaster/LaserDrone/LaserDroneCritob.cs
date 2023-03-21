using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Fisobs;
using Fisobs.Creatures;
using Fisobs.Sandbox;
using Fisobs.Properties;

namespace TheDroneMaster
{
    public class LaserDroneCritob : Critob
    {
        public static readonly CreatureTemplate.Type LaserDrone = new CreatureTemplate.Type("LaserDrone", true);
        public static readonly MultiplayerUnlocks.SandboxUnlockID LaserDroneUnlock = new MultiplayerUnlocks.SandboxUnlockID("LaserDrone", true);

        public LaserDroneCritob() : base(LaserDrone)
        {
            LoadedPerformanceCost = 100;
            SandboxPerformanceCost = new SandboxPerformanceCost(linear: 0.6f, exponential: 0.9f);
        }

        public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
        {
            return new LaserDroneAI(acrit, acrit.realizedCreature as LaserDrone);
        }

        public override Creature CreateRealizedCreature(AbstractCreature acrit)
        {
            return new LaserDrone(acrit);
        }

        public override CreatureState CreateState(AbstractCreature acrit)
        {
            return new NoHealthState(acrit);
        }

        public override CreatureTemplate CreateTemplate()
        {
            CreatureTemplate t = new CreatureFormula(this)
            {
                DefaultRelationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.25f),
                HasAI = true,
                InstantDeathDamage = 3,
                Pathing = PreBakedPathing.Ancestral(CreatureTemplate.Type.Fly),

                TileResistances = new TileResist()
                {
                    Air = new PathCost(1, PathCost.Legality.Allowed),
                    Solid = new PathCost(1f, PathCost.Legality.IllegalTile),
                    OffScreen = new PathCost(1, PathCost.Legality.Unallowed),
                },
                ConnectionResistances = new ConnectionResist()
                {
                    Standard = new PathCost(1f, PathCost.Legality.Allowed),
                    OpenDiagonal = new PathCost(0.5f, PathCost.Legality.Allowed),
                    ShortCut = new PathCost(1, PathCost.Legality.Allowed),
                    NPCTransportation = new PathCost(0, PathCost.Legality.Unallowed),
                    OffScreenMovement = new PathCost(0, PathCost.Legality.IllegalTile),
                    BetweenRooms = new PathCost(0.5f, PathCost.Legality.Allowed),
                },
                DamageResistances = new AttackResist()
                {
                    Base = 10f,
                },
                StunResistances = new AttackResist()
                {
                    Base = 10f,
                }
            }.IntoTemplate();

            t.offScreenSpeed = 1f;
            t.abstractedLaziness = 10;
            t.roamBetweenRoomsChance = 1f;
            t.bodySize = 0.5f;
            t.stowFoodInDen = false;
            t.shortcutSegments = 1;
            t.grasps = 1;
            t.visualRadius = 1600f;
            t.movementBasedVision = 1f;
            t.communityInfluence = 0f;
            t.waterRelationship = CreatureTemplate.WaterRelationship.AirAndSurface;
            t.waterPathingResistance = 2f;
            t.canFly = true;
            t.meatPoints = 0;
            t.dangerousToPlayer = 0f;

            return t;
        }

        public override void EstablishRelationships()
        {
            Relationships self = new Relationships(LaserDrone);

            foreach(var template in StaticWorld.creatureTemplates)
            {
                if (template.quantified)
                {
                    self.Ignores(template.type);
                    self.IgnoredBy(template.type);
                }
            }
            self.AttackedBy(CreatureTemplate.Type.Scavenger, 0.2f);
        }

        public override string DevtoolsMapName(AbstractCreature acrit)
        {
            return "lsd";
        }

        public override Color DevtoolsMapColor(AbstractCreature acrit)
        {
            return new Color(1f, 0.26f, 0.45f);
        }

        public override ItemProperties Properties(Creature crit)
        {
            if(crit is LaserDrone laserDrone)
            {
                return new LaserDroneProperties(laserDrone);
            }
            return null;
        }
    }
}
