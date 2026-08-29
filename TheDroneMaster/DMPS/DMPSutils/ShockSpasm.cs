using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSutils
{
    internal class ShockSpasm : CosmeticSprite
    {
        PhysicalObject owner;
        BodyChunk bindChunk;
        Vector2 setPos;

        FSprite sprite;
        int shockSprite;

        int life, initlife;
        float scale, energy;

        public ShockSpasm(Room room, Vector2 pos, int life, float scale) : this(room, life, scale)
        {
            this.lastPos = this.pos = this.setPos = pos;
        }

        public ShockSpasm(Room room, PhysicalObject owner, BodyChunk bodyChunk, int life, float scale, float energy) : this(room, life, scale)
        {
            this.owner = owner;
            this.bindChunk = bodyChunk;
            this.lastPos = this.pos = this.setPos = bindChunk.pos;
            this.energy = energy;
        }

        ShockSpasm(Room room, int life, float scale)
        {
            this.room = room;
            this.initlife = this.life = life;
            this.scale = scale;
            shockSprite = Random.Range(0, 4);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            if (bindChunk != null && bindChunk.owner.room == room && !bindChunk.owner.slatedForDeletetion)
            {
                setPos = bindChunk.pos;
                if(bindChunk.owner is Creature c)
                {
                    c.Violence(owner?.firstChunk, Vector2.zero, bindChunk, null, Creature.DamageType.Electric, energy * 0.5f / initlife, energy * (1 - life/(float)initlife));
                }
            }
            pos = setPos;

            if (life > 0)
            {
                life--;
                if (life == 0)
                    Destroy();
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[3]
            {
                new FSprite($"DMPS_ShockEffect_Blr_{shockSprite}")
                {
                    color = StaticColors.Menu.pink,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"]
                },
                new FSprite($"DMPS_ShockEffect_{shockSprite}")
                {
                    color = StaticColors.Menu.pink,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"]
                },
                new FSprite($"DMPS_ShockEffect_{shockSprite}")
                {
                    color = Color.white,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"]
                },
            };

            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("HUD");

            base.AddToContainer(sLeaser, rCam, newContatiner);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            var smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            bool nextFlash = Random.value < 0.5f;
            if(nextFlash && !sLeaser.sprites[0].isVisible)
            {
                shockSprite = Random.Range(0, 4);
                sLeaser.sprites[0].SetElementByName($"DMPS_ShockEffect_Blr_{shockSprite}");
                sLeaser.sprites[1].SetElementByName($"DMPS_ShockEffect_{shockSprite}");
                sLeaser.sprites[2].SetElementByName($"DMPS_ShockEffect_{shockSprite}");

                sLeaser.sprites[2].rotation = sLeaser.sprites[1].rotation = sLeaser.sprites[0].rotation = Random.Range(-180f, 180f);
            }
            sLeaser.sprites[2].isVisible = sLeaser.sprites[1].isVisible = sLeaser.sprites[0].isVisible = nextFlash;
            sLeaser.sprites[2].scale = sLeaser.sprites[1].scale = sLeaser.sprites[0].scale = scale;

            sLeaser.sprites[0].SetPosition(smoothPos - camPos);
            sLeaser.sprites[1].SetPosition(smoothPos - camPos);
            sLeaser.sprites[2].SetPosition(smoothPos - camPos);

            float alpha = life / (float)initlife;
            sLeaser.sprites[0].alpha = sLeaser.sprites[1].alpha = alpha;
            sLeaser.sprites[2].alpha = Mathf.InverseLerp(0, 3, energy) * alpha;
        }

        public override void Destroy()
        {
            base.Destroy();
        }
    }
}
