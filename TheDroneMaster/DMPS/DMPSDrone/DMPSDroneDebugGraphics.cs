using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSDrone
{
    internal class DMPSDroneDebugGraphics
    {
        int startSprite;
        DMPSDrone Drone;
        FLabel test;
        public int totSprite = 4;

        public DMPSDroneDebugGraphics(DMPSDrone drone, int startSprite)
        {
            Drone = drone;
            this.startSprite = startSprite;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            var s = new List<FSprite>
            {
                new FSprite("pixel", true)
                {
                    scale = 10f
                },
                new FSprite("pixel", true)
                {
                    scale = 10f,
                    color = Color.red
                },
                new FSprite("pixel", true)
                {
                    color = Color.red
                },
                new FSprite("pixel", true)
                {
                    scale = 10f,
                    color = Color.yellow
                },
            };

            sLeaser.containers = new FContainer[1];
            sLeaser.containers[0] = new FContainer();

            for(int i = 0;i < s.Count; i++)
            {
                sLeaser.sprites[startSprite + i] = s[i];
            }

            test = new FLabel(Custom.GetFont(), "") { anchorY = 1f};
            sLeaser.containers[0].AddChild(test);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = rCam.ReturnFContainer("HUD");

            for(int i = 0;i < totSprite; i++)
            {
                sLeaser.sprites[startSprite + i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[startSprite + i]);
            }

            foreach(var container in sLeaser.containers)
            {
                container.RemoveFromContainer();
                newContatiner.AddChild(container);
            }

        }
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (Drone.room == null)
                return;
            Vector2 drawPos, destPos, nextConnectionPos;
            drawPos = Vector2.Lerp(Drone.firstChunk.lastPos, Drone.firstChunk.pos, timeStacker) - camPos;
            destPos = Drone.room.MiddleOfTile(Drone.AI.pathFinder.destination) - camPos;
            nextConnectionPos = (Drone.nextConnection != default ? Drone.room.MiddleOfTile(Drone.nextConnection.DestTile) - camPos : Vector2.zero);

            sLeaser.sprites[startSprite].SetPosition(drawPos);
            sLeaser.sprites[startSprite + 1].SetPosition(destPos);

            sLeaser.sprites[startSprite + 2].scaleY = Vector2.Distance(drawPos, destPos);
            sLeaser.sprites[startSprite + 2].rotation = Custom.VecToDeg((destPos - drawPos).normalized);
            sLeaser.sprites[startSprite + 2].SetPosition((drawPos + destPos) / 2f);

            test.SetPosition(drawPos + Vector2.down * 20f);
            test.text = $"-Drone {Drone.abstractCreature.ID.number}-\npather dest : {Drone.AI.pathFinder.destination.x}, {Drone.AI.pathFinder.destination.y}" +
                $"\nowner coord : {Drone.AI.ownerCoord.x}, {Drone.AI.ownerCoord.y}" +
                $"\nusing weapon : {Drone.UsingWeapon}" +
                $"\ntarget : {(Drone.AI.target != null ? Drone.AI.target.creatureTemplate.type.value : "null") }";

            if (Drone.nextConnection != default)
            {
                sLeaser.sprites[startSprite + 3].isVisible = true;
                sLeaser.sprites[startSprite + 3].SetPosition(nextConnectionPos);
            }
            else
            {
                sLeaser.sprites[startSprite + 3].isVisible = false;
            }
        }
    } 
}
