using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSDrone.DroneWeapon;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSDrone
{
    internal class DMPSDrone : Creature, SharedPhysics.IProjectileTracer
    {
        public static float maxVelocity = 5f;

        public DMPSDroneAI AI;

        //ai pather
        public MovementConnection nextConnection;

        public Vector2 dir, lastDir, targetDir, lastVel;
        public float velocity;
        public Vector2 Vel => dir * velocity;

        public bool AtDestination => Custom.ManhattanDistance(coord, AI.pathFinder.GetDestination) < 2;
        public bool AtNextCoonection => Custom.ManhattanDistance(coord, nextConnection.destinationCoord) <= 2;

        //animation 
        DroneAnimation activeAnimation;
        public bool disableJet;
        public bool InAnimation => activeAnimation != null;
        public float wingStretch;

        //weapon
        public bool UsingWeapon => activeAnimation == null && (AI.currentBehaviour == DMPSDroneAI.Behaviour.FollowAndAttack || AI.currentBehaviour == DMPSDroneAI.Behaviour.Attack) && AI.target != null && AI.target.realizedCreature != null && AI.target.realizedCreature.room == room;
        public Vector2 weaponTargetDir, lastWeaponTargetDir;
        public BaseDroneWeapon weapon;

        public DMPSDrone(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
            bodyChunks = new BodyChunk[1];
            bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.07f);

            bodyChunkConnections = new BodyChunkConnection[0];

            airFriction = 0.999f;
            gravity = 0f;
            waterFriction = 0.9f;
            waterRetardationImmunity = 0.9f;
            GoThroughFloors = true;

            //weapon = new DefaultLaserGun();
            weapon = new LastPrismGun();

            if (abstractCreature is AbstractDMPSDrone abstractDMPSDrone)
            {
                if (!(abstractCreature as AbstractDMPSDrone).addByPort)
                {
                    Destroy();
                }
            }
            else
            {
                abstractCreature.Destroy();
                Destroy();
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if(room != null) 
                Act(); 
        }

        public void Act()
        {
            lastVel = firstChunk.vel;
            lastDir = dir;

            
            if(activeAnimation != null)
            {
                ActAnimation();
            }
            else
            {
                AI.Update();
                MovementUpdate();

            }
            weapon.Update(this);
        }

        void ActAnimation()
        {
            firstChunk.pos = activeAnimation.targetPos;
            dir = Vector3.Slerp(dir, activeAnimation.targetDir, 0.5f);
            wingStretch = activeAnimation.wingStretch;

            if (activeAnimation.slateForDeletion)
            {
                activeAnimation = null;
                return;
            }
        }

        void MovementUpdate()
        {
            if(enteringShortCut != null)
            {
                nextConnection = default;
                return;
            }

            if (!AtDestination && (nextConnection == default || AtNextCoonection))
            {
                nextConnection = (AI.pathFinder as DronePather).FollowPath(room.GetWorldCoordinate(mainBodyChunk.pos), true);
            }
            else if (AtDestination)
            {
                if (AI.pathFinder.destination.room == room.abstractRoom.index && room.shortcutData(AI.pathFinder.destination.Tile).shortCutType != ShortcutData.Type.DeadEnd && shortcutDelay == 0)
                    enteringShortCut = AI.pathFinder.destination.Tile;
                nextConnection = default;
            }


            firstChunk.vel *= 0.9f;

            dir = Vector3.Slerp(dir, targetDir, 0.15f);
            if (nextConnection != default && shortcutDelay == 0)
            {
                if(room.shortcutData(nextConnection.DestTile).shortCutType != ShortcutData.Type.DeadEnd && shortcutDelay == 0)
                {
                    enteringShortCut = nextConnection.DestTile;
                }
                else
                {
                    Vector2 delta = room.MiddleOfTile(nextConnection.destinationCoord) - firstChunk.pos;
                    firstChunk.vel += (delta).normalized /** Mathf.InverseLerp(20f, 100f, delta.magnitude)*/ * Mathf.InverseLerp(10f, 0f, firstChunk.vel.magnitude);
                    firstChunk.vel = Vector2.ClampMagnitude(firstChunk.vel, maxVelocity);

                    if (UsingWeapon)
                    {
                        UpdateWeaponTargetDir();
                        targetDir = weaponTargetDir;
                        dir = Vector3.Slerp(dir, weaponTargetDir, 0.7f);
                    }
                    else
                    {
                        Vector2 acc = firstChunk.vel - lastVel;
                        targetDir = Vector3.Slerp(firstChunk.vel.normalized, acc.normalized, acc.magnitude);
                    }
                }

            }
            else if(firstChunk.vel != Vector2.zero)
            {
                if (UsingWeapon)
                {
                    UpdateWeaponTargetDir();
                    targetDir = dir = weaponTargetDir;
                }
                else
                    targetDir = firstChunk.vel.normalized;
            }
        }

        void UpdateWeaponTargetDir()
        {
            lastWeaponTargetDir = weaponTargetDir;
            //foreach(var bodyChunk in AI.target.realizedCreature.bodyChunks)
            //{
            //    if (room.VisualContact(firstChunk.pos, bodyChunk.pos))
            //    {
            //        weaponTargetDir = (bodyChunk.pos - firstChunk.pos).normalized;
            //        return;
            //    }
            //}
            weaponTargetDir = (AI.target.realizedCreature.mainBodyChunk.pos - firstChunk.pos).normalized;
        }

        public override void SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            base.SpitOutOfShortCut(pos, newRoom, spitOutAllSticks);
            nextConnection = default;
            foreach(var dir in Custom.fourDirections)
            {
                if(!newRoom.GetTile(dir + pos).Solid)
                {
                    firstChunk.vel = (dir.ToVector2()) * 20f;
                    break;
                }
            }
        }

        public override void InitiateGraphicsModule()
        {
            graphicsModule = new DMPSDroneGraphics(this);
        }

        public void StartAnimation(DroneAnimation animation)
        {
            if(activeAnimation != null)
            {
                activeAnimation.slateForDeletion = true;
                activeAnimation = null;
            }
            this.activeAnimation = animation;
        }

        public bool HitThisObject(PhysicalObject obj)
        {
            return !(obj is Player) && !(obj is DMPSDrone) && (obj is Creature);
        }

        public bool HitThisChunk(BodyChunk chunk)
        {
            return true;
        }

        public class DroneAnimation : UpdatableAndDeletable
        {
            public bool slateForDeletion;
            protected DMPSDrone drone;

            public Vector2 targetPos;
            public Vector2 targetDir;
            public float wingStretch;

            public DroneAnimation(DMPSDrone drone)
            {
                this.drone = drone;
            }
        }
    }
}
