using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheDroneMaster.CreatureScanner;
using static TheDroneMaster.PlayerPatchs;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DreamComponent.GameHook
{
    static public class SessionHook
    {
        static public void PatchOn()
        {
            On.RoomSpecificScript.AddRoomSpecificScript += MSCRoomSpecificScript_AddRoomSpecificScript;
        }

        private static void MSCRoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig(room);
            //TODO : 结局判断
            if(room.abstractRoom.name == "SI_A07" && room.world.game.session.characterStats.name.value == "thedronemaster")
            {
                room.AddObject(new DroneMasterEnding(room));
            }

        }
    }

    public class DroneMasterEnding : UpdatableAndDeletable
    {
        public DroneMasterEnding(Room room)
        {
            this.room= room;
            var player = room.game.FirstAlivePlayer;
            if(player.realizedCreature != null 
                && Plugin.OwnLaserDrone.TryGet(player.realizedCreature as Player, out bool ownLaserDrone) 
                && ownLaserDrone
                && PlayerPatchs.modules.TryGetValue(focusedPlayer, out module))
            {
                focusedPlayer = player.realizedCreature as Player;
                //TODO : 获取扫描的生物
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            endingCounter++;
            if(curSender == null || curSender.slatedForDeletetion)
            {
                curSender = new CreatureEndingSender(creatureQueue.Dequeue(),room,Vector2.zero);
                room.AddObject(curSender);
            }
                
        }
        Player focusedPlayer;
        PlayerModule module;
        Queue<CreatureTemplate.Type> creatureQueue;
        CreatureEndingSender curSender = null;
        int endingCounter = 0;

        public class CreatureEndingSender : CosmeticSprite, IOwnProjectedCircles
        {
            //Type也是临时用
            public CreatureEndingSender(CreatureTemplate.Type type,Room room,Vector2 pos)
            {
                this.type = type;
                this.room = room;
                this.pos = pos;
                var state = Random.state;
                Random.InitState(114514 + type.index);
 
                //Just for Test
                bool[] allBools = new bool[] { Random.value<0.5,  Random.value < 0.5, Random.value < 0.5, Random.value < 0.5, Random.value < 0.5,Random.value < 0.5,false };
                for(int i=0;i<allBools.Length;i++)
                {
                    messages[i] = new TagMessage(type, allBools[i],ref spriteCount, i);
                    compresseds[i] = new TagCompressed(ref spriteCount, i);
                    overlays[i] = new TagOverlay(messages[i], compresseds[i], ref spriteCount,i);
                    allParts.Enqueue(messages[i]);
                    allParts.Enqueue(overlays[i]);
                    allParts.Enqueue(compresseds[i]);
                }
                Random.state = state;
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (allParts.Count != 0)
                {
                    allParts.First().Update();
                    if (allParts.First().NeedDelete)
                        allParts.Dequeue();
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[spriteCount];
                foreach(var i in messages)
                    i.InitiateSprites(sLeaser, rCam);

                foreach (var i in overlays)
                    i.InitiateSprites(sLeaser, rCam);


                foreach (var i in compresseds)
                    i.InitiateSprites(sLeaser, rCam);

                AddToContainer(sLeaser, rCam, null);
                
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                if (allParts.Count != 0)
                    allParts.First().DrawSprites(sLeaser, rCam, timeStacker, camPos, pos);
            }


            public Vector2 CircleCenter(int index, float timeStacker)
            {
                throw new NotImplementedException();
            }

            public Room HostingCircleFromRoom()
            {
                throw new NotImplementedException();
            }

            public bool CanHostCircle()
            {
                throw new NotImplementedException();
            }

 
            int curIndex = 0;
            int spriteCount = 0;
            CreatureTemplate.Type type;
            TagMessage[] messages = new TagMessage[7];
            TagOverlay[] overlays = new TagOverlay[7];
            TagCompressed[] compresseds = new TagCompressed[7];
            Queue<SenderPart> allParts = new Queue<SenderPart>();
        }

        public class SenderPart
        {
            public int totalSprites;
            public int startSprite;
            public int counter;
            public int filterIndex;
            public readonly int yBias = 17;

            bool isInit = false;

            public SenderPart(int startSprite, int filterIndex, Color? color)
            {
                this.startSprite = startSprite;
                this.filterIndex = filterIndex;

                if (color.HasValue)
                    this.color = color.Value;
            }

            public void SetSpriteOffest(ref int start)
            {
                start += totalSprites;
                isInit = true;
            }

            public virtual bool NeedDelete { get { return false; } }

            public Color color = Color.cyan;

            public virtual void Update()
            {
                if (!isInit)
                    throw new Exception("Forget to set offest");
                counter++;

            }

            public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
            }
            public  virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 pos)
            {
            }
        }

        public class TagCompressed : SenderPart
        {
            public TagCompressed(ref int startSprite, int filterIndex, Color? color = null) : base(startSprite, filterIndex, color)
            {
                totalSprites = 1;
                SetSpriteOffest(ref startSprite);
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[startSprite] = new FSprite("BigGlyph" + Random.Range(0, 12));
                sLeaser.sprites[startSprite].isVisible = false;
                sLeaser.sprites[startSprite].color = color;
                sLeaser.sprites[startSprite].SetAnchor(Vector2.zero);
            }

            public void ShowGlyph(RoomCamera.SpriteLeaser sLeaser,Vector2 pos,Vector2 camPos)
            {
                sLeaser.sprites[startSprite].isVisible = true;
               
                sLeaser.sprites[startSprite].x = pos.x - camPos.x;
                sLeaser.sprites[startSprite].y = pos.y - filterIndex*yBias - camPos.y;
            }

            public override void Update()
            {
                base.Update();
            }

            public override bool NeedDelete => counter > 40;

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 pos)
            {
                
            }

        }

        public class TagOverlay : SenderPart
        {

            public TagMessage message;
            public TagCompressed compressed;
            public int pixelPreCounter = 2;

            public float bigWidth=15;
            public float bigHeight=20;

            int endCount = 110;
            float width;
            float height;

            public override bool NeedDelete { get => counter > endCount; }
            public TagOverlay(TagMessage message, TagCompressed compressed , ref int startSprite, int filterIndex, Color? color = null) : base(startSprite,filterIndex,  color)
            {
                this.message = message;
                this.compressed = compressed;

                totalSprites = 5;

                width = (message.glyphs.Length+1) * 10;

                if (color.HasValue)
                    this.color = color.Value;

                height = 10f;

                SetSpriteOffest(ref startSprite);
            }


            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = 0; i < totalSprites - 1; i++)
                {
                    sLeaser.sprites[startSprite + i] = new FSprite("pixel");
                    //sLeaser.sprites[startSprite + i].isVisible = false;
                }
                var sprite = new CustomFSprite("pixel");
                sLeaser.sprites[startSprite + totalSprites - 1] = sprite;
                sLeaser.sprites[startSprite + totalSprites - 1].isVisible = false;

                sLeaser.sprites[startSprite].anchorX = 0.5f;
                sLeaser.sprites[startSprite].anchorY = 0f;

                sLeaser.sprites[startSprite + 1].anchorX = 0f;
                sLeaser.sprites[startSprite + 1].anchorY = 0.5f;

                sLeaser.sprites[startSprite + 2].anchorX = 0.5f;
                sLeaser.sprites[startSprite + 2].anchorY = 1f;

                sLeaser.sprites[startSprite + 3].anchorX = 1f;
                sLeaser.sprites[startSprite + 3].anchorY = 0.5f;

                for (int i = 0; i < totalSprites; i++)
                {
                    sLeaser.sprites[startSprite + i].color = color;
                }
                for(int i=0;i<4;i++)
                 sprite.verticeColors[i] = color;
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 pos)
            {
               
                Vector2 starPos = pos + filterIndex * Vector2.down * yBias;

                if (counter < 40)
                {
                    var now = Mathf.SmoothStep(0, 1, Mathf.Pow((counter / (float)40), 2f)) * (width + height +1);
                    sLeaser.sprites[startSprite].SetPosition(starPos - camPos);
                    sLeaser.sprites[startSprite + 1].SetPosition(starPos - camPos + new Vector2(-0.5f, height));
                    sLeaser.sprites[startSprite + 2].SetPosition(starPos - camPos + new Vector2(width, height));
                    sLeaser.sprites[startSprite + 3].SetPosition(starPos - camPos + new Vector2(width+0.5f, 0));

                    sLeaser.sprites[startSprite].height = Mathf.Clamp(now, 0, height);
                    sLeaser.sprites[startSprite + 1].width = Mathf.Clamp(now - (height), 0, width+1);
                    sLeaser.sprites[startSprite + 2].height = Mathf.Clamp(now, 0, height);
                    sLeaser.sprites[startSprite + 3].width = Mathf.Clamp(now - height, 0, width+1);

                    sLeaser.sprites[startSprite + 1].height = sLeaser.sprites[startSprite ].width = Mathf.Lerp(1,2,now / (width + height));
                    sLeaser.sprites[startSprite + 3].height = sLeaser.sprites[startSprite + 2].width = Mathf.Lerp(1, 2, now / (width + height));
                }
                else if(counter == 40)
                {
                    sLeaser.sprites[startSprite + totalSprites - 1].isVisible = true;
                }
                else if(counter < 60)
                {
                    var sprite = sLeaser.sprites[startSprite + totalSprites - 1] as CustomFSprite;
                    var lerpTimer = Mathf.SmoothStep(0,1,(counter - 40)/20f);
       
                    sprite.MoveVertice(0, starPos - camPos + new Vector2(width * (1 - lerpTimer), 0));
                    sprite.MoveVertice(1, starPos - camPos + new Vector2(width * (1 - lerpTimer), height));
                    sprite.MoveVertice(2, starPos - camPos + new Vector2(width,  height));
                    sprite.MoveVertice(3, starPos - camPos + new Vector2(width, 0));
                }
                else if(counter == 60)
                {
                    for (int i = 0; i < totalSprites - 1; i++)
                        sLeaser.sprites[startSprite + i].isVisible = false;
                    message.HiddenAll(sLeaser);
                }
                else if(counter < 90)
                {
                    var sprite = sLeaser.sprites[startSprite + totalSprites - 1] as CustomFSprite;
                    var heightTimer = Mathf.SmoothStep(0, 1, (counter - 60) / 30f);
                    var widthTimer = Mathf.SmoothStep(0, 1, (counter - 70) / 20f);
                    var tmpWidth = Mathf.Lerp(width, bigWidth, widthTimer);
                    var tmpHeight = Mathf.Lerp(height,bigHeight, heightTimer);
                    sprite.MoveVertice(0, starPos - camPos + new Vector2(0, 0));
                    sprite.MoveVertice(1, starPos - camPos + new Vector2(0, tmpHeight));
                    sprite.MoveVertice(2, starPos - camPos + new Vector2(tmpWidth, tmpHeight));
                    sprite.MoveVertice(3, starPos - camPos + new Vector2(tmpWidth, 0));

                }
                else if(counter == 90)
                {
                    compressed.ShowGlyph(sLeaser,pos,camPos);
                }
                else if(counter < 100)
                {
                    var sprite = sLeaser.sprites[startSprite + totalSprites - 1] as CustomFSprite;
                    var lerpTimer = Mathf.SmoothStep(0, 1, (counter - 90) / 10f);
        
                    sprite.MoveVertice(0, starPos - camPos + new Vector2(0, bigHeight * lerpTimer));
                    sprite.MoveVertice(1, starPos - camPos + new Vector2(0, bigHeight));
                    sprite.MoveVertice(2, starPos - camPos + new Vector2(bigWidth, bigHeight));
                    sprite.MoveVertice(3, starPos - camPos + new Vector2(bigWidth, bigHeight * lerpTimer));
                }
                else if (counter == 100)
                {
                    for (int i = 0; i < totalSprites - 1; i++)
                        sLeaser.sprites[startSprite + i].isVisible = false;
                    sLeaser.sprites[startSprite + totalSprites - 1].isVisible = false;
                }
                
            }
        }

        public class TagMessage : SenderPart
        {
            public GlyphRepresent[] glyphs;

            public bool check;
            public int afterShowCounter = -1;
            public override bool NeedDelete { get => afterShowCounter > 10f; }

            public int charTime = 3;

            //Type也是临时用
            public TagMessage(CreatureTemplate.Type type,  bool check,ref int startSprite, int filterIndex,Color? color = null) : base(startSprite, filterIndex, color)
            {
                this.check = check;
                totalSprites = 1;

                if(color != null)
                    this.color = color.Value;   

                var state = Random.state;
                Random.InitState(114514 + type.Index);

                glyphs = new GlyphRepresent[Random.Range(5, 15)];
                int x = 1; //第一个是选择框
                int y = 0;
                for (int i = 0; i < glyphs.Length; i++)
                {
                    if (Random.value < 0.2f && i > 0) glyphs[i] = new GlyphRepresent("", true, false, ref x, ref y);
                    else
                    {
                        glyphs[i] = new GlyphRepresent("TinyGlyph" + Random.Range(0, 14).ToString(), false, false, ref x, ref y);
                        totalSprites++;
                    }
                }
                Random.state = state;

                SetSpriteOffest(ref startSprite);
            }

            public void HiddenAll(RoomCamera.SpriteLeaser sLeaser)
            {
                for (int i = 0; i < totalSprites; i++)
                    sLeaser.sprites[i + startSprite].isVisible = false;
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                
                sLeaser.sprites[startSprite] = new FSprite(check ? Plugin.trueRectName : Plugin.falseRectName, true) { color = color, scale = 0.5f, isVisible = false };
                int index = 1;
                sLeaser.sprites[startSprite].SetAnchor(Vector2.zero);
                for (int i = 0; i < glyphs.Length; i++)
                {
                    var glyph = glyphs[i];
                    if (glyph.isSpaceOrChangeLine) continue;
                    sLeaser.sprites[startSprite + index] = new FSprite(glyph.spriteName, true) { color = color ,isVisible =false};
                    sLeaser.sprites[startSprite + index].SetAnchor(Vector2.zero);
                    index++;
                }
           
            }

            public override void Update()
            {
                base.Update();
                if (afterShowCounter != -1)
                    afterShowCounter++;
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 pos)
            {

                Vector2 starPos = pos + filterIndex * Vector2.down * yBias;
                sLeaser.sprites[startSprite].SetPosition(starPos - camPos);
                sLeaser.sprites[startSprite].isVisible = true;
                int index = 1 + startSprite;
                foreach (var represent in glyphs)
                {
                    if (represent.isSpaceOrChangeLine) continue;
                    sLeaser.sprites[index].SetPosition(starPos.x + (represent.indexX * 10f) - rCam.pos.x, starPos.y - represent.indexY * 10f - rCam.pos.y);
                    sLeaser.sprites[index].isVisible = ((index - startSprite) * charTime) - 2 < counter;
                    if ((index * charTime) - 2 < counter && ((index - startSprite) * charTime) > counter && Random.value<0.2f)
                        sLeaser.sprites[index].element = Futile.atlasManager.GetElementWithName("TinyGlyph" + Random.Range(0, 14).ToString());
                    else
                        sLeaser.sprites[index].element = Futile.atlasManager.GetElementWithName(represent.spriteName);
                    index++;
                }

                if ((index - startSprite) * charTime < counter && afterShowCounter == -1)
                {
                    afterShowCounter = 0;
                }
            }
        }
    }

}


