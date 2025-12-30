using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSDrone
{
    internal class DMPSDroneAI : ArtificialIntelligence
    {
        //live params
        public WorldCoordinate ownerCoord;
        public bool ownerInShortcut;
        public AbstractCreature target;

        public DMPSDrone drone;

        public Behaviour currentBehaviour;

        //hoverBias
        public IntVector2 hoverBias;
        int changeHoverBiasCD = 0;

        public DMPSDroneAI(AbstractCreature creature, World world) : base(creature, world)
        {
            (creature.realizedCreature as DMPSDrone).AI = this;
            drone = creature.realizedCreature as DMPSDrone;

            AddModule(new DronePather(this, world, creature));
            //AddModule(new Tracker(this, 10, 10, 600, 0.5f, 5, 5, 10));
        }

        public override void Update()
        {
            base.Update();
            if (drone.room == null)
                return;
            if (!drone.InAnimation)
                Act();
        }

        public void Act()
        {
            DecideBehaivour();
            if (currentBehaviour == Behaviour.Follow || currentBehaviour == Behaviour.FollowAndAttack)
                FollowUpdate();
            else if (currentBehaviour == Behaviour.Attack)
                AttackUpdate();
        }

        void DecideBehaivour()
        {
            if (ownerCoord.room != drone.coord.room)
            {
                if (target != null && drone.coord.room == target.pos.room)
                    ChangeBehaviour(Behaviour.FollowAndAttack);
                else
                    ChangeBehaviour(Behaviour.Follow);
            }
            else
            {
                if(target != null)
                {
                    ChangeBehaviour(Behaviour.Attack);
                }
                else
                    ChangeBehaviour(Behaviour.Follow);
            }
        }

        void ChangeBehaviour(Behaviour newBehaviour)
        {
            if(currentBehaviour == newBehaviour) 
                return;
            currentBehaviour = newBehaviour;
            idealAttackPos = drone.abstractCreature.pos.Tile;
        }
        void FollowUpdate()
        {
            if (changeHoverBiasCD == 0)
            {
                hoverBias = new IntVector2(Random.Range(-3, 3), Random.Range(-3, 3));
                changeHoverBiasCD = Random.Range(160, 480);
            }
            else
                changeHoverBiasCD--;

            if (drone.room.GetTilePosition(drone.firstChunk.pos) != ownerCoord.Tile + new IntVector2(0, 2))
            {
                pathFinder.SetDestination(ownerCoord + (ownerInShortcut ? new IntVector2(0, 0) : hoverBias + new IntVector2(0, 2)));
            }
        }

        #region Attack
        IntVector2 idealAttackPos;
        void AttackUpdate()
        {
            if (target.realizedCreature == null || target.realizedCreature.room != drone.room)
                return;

            IntVector2 newAttackPos;
            float currentHighestScore = AttackPosScore(idealAttackPos);
            for(int i = 0;i < 50; i++)
            {
                newAttackPos = new IntVector2(Random.Range(0, drone.room.Width), Random.Range(0, drone.room.Height));
                float newScore = AttackPosScore(newAttackPos);
                if(newScore > currentHighestScore)
                {
                    currentHighestScore = newScore;
                    idealAttackPos = newAttackPos;
                }
            }

            if (drone.room.GetTilePosition(drone.firstChunk.pos) != idealAttackPos)
            {
                pathFinder.SetDestination(drone.room.GetWorldCoordinate(idealAttackPos));
            }
        }

        float AttackPosScore(IntVector2 pos)
        {
            if (drone.room.GetTile(pos).Solid || !pathFinder.CoordinateReachable(drone.room.GetWorldCoordinate(pos)))
                return float.MinValue;

            float score = 0f;
            Vector2 middle = drone.room.MiddleOfTile(pos);

            foreach(var bodyChunk in target.realizedCreature.bodyChunks)
            {
                if (!drone.room.VisualContact(middle, bodyChunk.pos))
                    score -= 10000f;
            }
            float distance = Vector2.Distance(target.realizedCreature.mainBodyChunk.pos, middle);
            score += distance;
            score -= Mathf.Pow(Mathf.Max(0f, distance - 100f), 1.5f);

            if (drone.room.aimap.getAItile(pos).narrowSpace)
                score -= 20f;

            return score;
        }

        #endregion
        public enum Behaviour
        {
            Follow,
            FollowAndAttack,
            Attack,
            Stay,
            Hover,
        }
    }
}
