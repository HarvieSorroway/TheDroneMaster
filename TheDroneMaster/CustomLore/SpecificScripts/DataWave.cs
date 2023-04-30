using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.CustomLore.SpecificScripts
{
    public class DataWave : CosmeticSprite
    {
        Vector2 centerPos;
        Color color;
        float twistRad;
        float preTwistRad;
        float waveSpeed;
        int life;
        int currentLife;

        float strength = 1f;
        float waveRad;

        FSprite wave;
        public DataWave(Room room,Vector2 centerPos,float twistRad,float preTwistRad,int life,float waveSpeed) 
        {
            base.room = room;
            this.centerPos = centerPos;
            this.twistRad = twistRad;
            this.preTwistRad = preTwistRad;
            this.waveSpeed = waveSpeed;
            this.life = life;

            color = LaserDroneGraphics.defaultLaserColor;
            color.a = 0.5f;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new CustomFSprite("pixel") { shader = rCam.room.game.rainWorld.Shaders["DataWave"], alpha = 1f,isVisible = true };
            wave = sLeaser.sprites[0];
            for (int i = 0;i < 4; i++)
            {
                (sLeaser.sprites[0] as CustomFSprite).verticeColors[i] = Color.white;
            }
            
            AddToContainer(sLeaser,rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if(newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("HUD");
            }
            newContatiner.AddChild(sLeaser.sprites[0]);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            currentLife++;
            
            strength = Mathf.Clamp01(1f - (float)(currentLife / life) * 1.4f);
            waveRad += waveSpeed * strength;

            if (currentLife > life)
            {
                Destroy();
                return;
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            wave.isVisible = false;
            wave.RemoveFromContainer();
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            if (slatedForDeletetion)
                sLeaser.sprites[0].isVisible = false;

            try
            {
                (sLeaser.sprites[0] as CustomFSprite).MoveVertice(0, Vector2.zero);
                (sLeaser.sprites[0] as CustomFSprite).MoveVertice(1, new Vector2(0f, Screen.height));
                (sLeaser.sprites[0] as CustomFSprite).MoveVertice(2, new Vector2(Screen.width, Screen.height));
                (sLeaser.sprites[0] as CustomFSprite).MoveVertice(3, new Vector2(Screen.width, 0f));

                Plugin.Log("normal update{0},{1}", currentLife, sLeaser.sprites[0]._renderLayer._material.GetFloat("waveRad"));

                //}
                sLeaser.sprites[0]._renderLayer._material.SetVector("centerPosOnScreen", centerPos - camPos);
                sLeaser.sprites[0]._renderLayer._material.SetFloat("waveRad", waveRad);
                sLeaser.sprites[0]._renderLayer._material.SetFloat("waveStrength", strength);
                sLeaser.sprites[0]._renderLayer._material.SetFloat("twistRad", twistRad);
                sLeaser.sprites[0]._renderLayer._material.SetFloat("preTwistRad", preTwistRad);
                sLeaser.sprites[0]._renderLayer._material.SetColor("waveCol", color);
            }
            catch (Exception e)
            {
                Plugin.Log("Error update {0},{1}", sLeaser.sprites[0], sLeaser.sprites[0]._renderLayer);
            }
        }
    }
}
