using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using TheDroneMaster.CustomLore.SpecificScripts;
using UnityEngine;
using static TheDroneMaster.CreatureScanner;
using static TheDroneMaster.CustomLore.SpecificScripts.Simple3DObject;
using static TheDroneMaster.PlayerPatchs;
using Random = UnityEngine.Random;

namespace TheDroneMaster.GameHooks
{
    static public class SessionHook
    {
        static public void Patch()
        {
            On.RoomCamera.Update += RoomCamera_Update;
            On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
        }

        private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
        {
            if (ghost > 0)
                self.ghostMode = origGhost;
            orig(self);
        }

        private static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            origGhost = self.ghostMode;
            if (ghost > 0)
                self.ghostMode = ghost;
            orig(self);
        }

        public static float ghost = -1;
        public static float origGhost = -1;
    }

    public class DroneMasterEnding : UpdatableAndDeletable, IDrawable
    {
        public readonly int RECT_COUNT = 4;
   
        public DroneMasterEnding(Room room)
        {
            this.room= room;
            creatureQueue = new Queue<CreatureTemplate.Type>();
            var player = room.game.Players[0];
            centerPos = new Vector2(470f, 560f);
            senderList = new List<CreatureEndingSender>();

            rects = new Mesh3D[RECT_COUNT];  
            renderers = new Mesh3DRenderer[RECT_COUNT];
            rotVels = new Vector3[RECT_COUNT];
            stdScales = new float[RECT_COUNT];
            scales = new float[RECT_COUNT];

            positionedSoundEmitter = new PositionedSoundEmitter(centerPos, 1f, 1f);
            room.PlaySound(DroneMasterEnums.DataHumming, positionedSoundEmitter, true, 1f, 1f, false);

            if (player.realizedCreature != null 
                && Plugin.OwnLaserDrone.TryGet(player.realizedCreature as Player, out bool ownLaserDrone) 
                && ownLaserDrone
                && modules.TryGetValue(player.realizedCreature as Player, out _))
            {

                focusedPlayer = player.realizedCreature as Player;
                totalSprites += glphyCount = Random.Range(10,20);
                glphys = new GlyphPoint[glphyCount];
                for (int i = 0; i < glphyCount; i++)
                {
                    glphys[i] = new GlyphPoint();
                    glphys[i].pos = centerPos + Custom.RNV() * Random.value * rad;
                    glphys[i].vel = Custom.RNV() * Random.Range(0.5f,1.0f) * 30;
                    glphys[i].life = Random.Range(80, 200);

                }

                Mesh3D.TriangleFacet[] rect = new Mesh3D.TriangleFacet[2]
                {
                    new Mesh3D.TriangleFacet(0,1,2),
                    new Mesh3D.TriangleFacet(0,2,3)
                };


                for (int i = 0; i < RECT_COUNT; i++)
                {
                    Mesh3D mesh = rects[i] = new Mesh3D();
                    mesh.SetFacet(rect);
                    mesh.rotation = new Vector3(Random.value * 180, Random.value * 180, Random.value * 180);
                    mesh.SetVertice(0, Vector3.left + Vector3.down);
                    mesh.SetVertice(1, Vector3.right + Vector3.down);
                    mesh.SetVertice(2, Vector3.right + Vector3.up);
                    mesh.SetVertice(3, Vector3.left + Vector3.up);
                    stdScales[i] = i * 15 + 30;
                    FoolSetScale(mesh, 0);

                    renderers[i] = new Mesh3DFacetRenderer(mesh, totalSprites, "pixel", false);
                    renderers[i].shader = "LineMask";
                    renderers[i].SetUV(0, new Vector2(0, 0));
                    renderers[i].SetUV(1, new Vector2(1, 0));
                    renderers[i].SetUV(2, new Vector2(1, 1));
                    renderers[i].SetUV(3, new Vector2(0, 1));
                    renderers[i].SetVerticeColor(color, true);
                    renderers[i].SetVerticeColor(color, false);
                    totalSprites += renderers[i].totalSprites;

                    rotVels[i] = Custom.RNV() * 180 * Mathf.Lerp(0.4f, 1, Random.value);
                }


                //TODO : 获取扫描的生物
                var scannedCreatures = DeathPersistentSaveDataPatch.GetUnitOfType<ScannedCreatureSaveUnit>().scanedTypes;
                scannedCreatures.Remove(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing);
                scannedCreatures.Add(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing);

                foreach(var type in scannedCreatures)
                {
                    creatureQueue.Enqueue(type);
                }

                //creatureQueue.Enqueue(CreatureTemplate.Type.BigEel);
                //creatureQueue.Enqueue(CreatureTemplate.Type.BlueLizard);
                //creatureQueue.Enqueue(CreatureTemplate.Type.CicadaA);
                //creatureQueue.Enqueue(CreatureTemplate.Type.MirosBird);
                //creatureQueue.Enqueue(CreatureTemplate.Type.Salamander);
                //creatureQueue.Enqueue(CreatureTemplate.Type.Scavenger);
                //creatureQueue.Enqueue(CreatureTemplate.Type.Slugcat);
                //creatureQueue.Enqueue(CreatureTemplate.Type.BigNeedleWorm);
                room.AddObject(light = new LightSource(centerPos, false, color, this));
                light.rad = 500f;
                init = true;
            }
        }

        public void FoolSetScale(Mesh3D mesh,float scale)
        {
            scales[rects.IndexOf(mesh)] = scale;
            for(int i=0;i<mesh.origVertices.Count;i++)
                mesh.animatedVertice[i] = scale * mesh.origVertices[i];
            
        }

        public override void Update(bool eu)
        {
            if (!init && !slatedForDeletetion)
            {
                Plugin.Log("DMEnding : Create Ending Failed");
                slatedForDeletetion = true;
            }
            if (slatedForDeletetion)
            {
                return;
            }

            base.Update(eu);
            endingCounter++;

            //3d render
            for(int i = 0; i < rects.Length; i++)
            {
                rects[i].rotation += rotVels[i] * 0.01f;
                renderers[i].Update();
            }

            //glphy
            for (int i = 0; i < glphyCount; i++)
            {
                glphys[i].pos += glphys[i].vel / 40f;
                glphys[i].life--;
                if (!Custom.DistLess(glphys[i].pos, centerPos, rad) || glphys[i].life == 0)
                {
                    glphys[i].pos = centerPos + Custom.RNV() * Random.value * rad;
                    glphys[i].vel = Custom.RNV() * Random.Range(0.5f, 1.0f) * 30;
                    glphys[i].life = Random.Range(80, 200);
                }
            }

            //core init process
            if(endingCounter <= 120)
            {
                room.game.cameras[0].hardLevelGfxOffset.y = Mathf.SmoothStep(0, 100, endingCounter / 60f);
                for (int i = 0; i < rects.Length; i++)
                    FoolSetScale(rects[i], Custom.LerpBackEaseOut(0, stdScales[i], endingCounter / 100f));

                room.game.cameras[0].paletteBlend += 1 / 100f;

                if(room.game.cameras[0].paletteBlend >= 1 && room.game.cameras[0].paletteB != 10)
                {
                    room.game.cameras[0].paletteBlend = 0f;
                    room.game.cameras[0].ChangeBothPalettes(room.game.cameras[0].paletteB, 10, room.game.cameras[0].paletteBlend);
                }

                room.game.cameras[0].ApplyFade();
                //SessionHook.ghost = endingCounter / 120f;
            }
            //creature display process
            else if (endingCounter > 120 && CurSenders < curMaxSenders && (--nextSenderCd) < 0 && creatureQueue.Count !=0)
            {
                var pos = CalNewPos();
                if (pos != null)
                {
                    nextSenderCd = Random.Range(120, 180);
                    curMaxSenders = Random.Range(4, 6);
                    var a = new CreatureEndingSender(this, creatureQueue.Dequeue(), room, pos.Value, color + new Color(Random.value / 10f, Random.value / 10f, Random.value / 10f));
                    senderList.Add(a);
                    room.AddObject(a);
                }
                else
                    Plugin.Log("DMEnding : Try Find new pos failed");
            }
            //send process
            else if(creatureQueue.Count == 0 && CurSenders ==0)
            {
                if (sender == null)
                {
                    endingCounter = 10000;
                    room.AddObject(sender = new EndingMessageSender(this));
                }
                room.game.cameras[0].hardLevelGfxOffset.y = Mathf.Lerp(room.game.cameras[0].hardLevelGfxOffset.y, 300, 0.02f);
            }
            if (endingCounter == 10200)
            {
                room.game.manager.rainWorld.progression.SaveProgressionAndDeathPersistentDataOfCurrentState(false,false);
                room.game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Credits,5f);
            }

                
        }

        public Vector2? CalNewPos()
        {
            Vector2 Pos;
            int tryCount = 0;
            do
            {
                float xOffest = Random.value * 0.4f;
                float yOffest = (0.4f - xOffest) * 0.2f / 0.4f;
                Pos = new Vector2((((0.1f + xOffest) * Mathf.Sign(Random.value - 0.5f) + 0.5f)) * room.PixelWidth, (0.1f + yOffest + Random.value * (0.8f - yOffest)) * room.PixelHeight) + room.game.cameras[0].hardLevelGfxOffset;
                tryCount++;
                if (tryCount > 40)
                    return null;
            }
            while (!senderList.All(i => !Custom.DistLess(i.pos, Pos, i.stdRad * 3.5f)));

            return Pos;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[totalSprites];
            for(int i=0;i< glphyCount;i++)
            {
                sLeaser.sprites[i] = new FSprite("TinyGlyph" + Random.Range(0, 14).ToString());
                sLeaser.sprites[i].scale = Random.Range(0.7f, 1f) * 2f;
                sLeaser.sprites[i].color = color;
                sLeaser.sprites[i].alpha = 0;
                sLeaser.sprites[i].shader = room.game.rainWorld.Shaders["Hologram"];
            }
            foreach(var render in renderers)
            {
                render.InitSprites(sLeaser, rCam);
            }
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for(int i=0;i<renderers.Length;i++)
            {
                var render = renderers[i];
                render.DrawSprites(sLeaser, rCam, timeStacker, camPos, centerPos);
                for(int j=0;j<render.totalSprites;j++)
                {
                    sLeaser.sprites[j + render.startIndex]._renderLayer?._material?.SetFloat("_Width", Mathf.Lerp(1f,0.05f,scales[i] / stdScales[i]));
                }
            }
            for(int i=0;i<glphyCount;i++)
            {
                sLeaser.sprites[i].x = glphys[i].pos.x - camPos.x;
                sLeaser.sprites[i].y = glphys[i].pos.y - camPos.y;
                sLeaser.sprites[i].alpha = Mathf.Min(Mathf.Clamp01((endingCounter - 40 - i * 5f) / 20f), (rad - Custom.Dist(glphys[i].pos, centerPos)) / rad * 2.5f, Mathf.Clamp01(glphys[i].life/20f));

                if(Random.value<0.05f)
                    sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName("TinyGlyph" + Random.Range(0, 14).ToString());
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if(newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Midground");
            foreach(var sprite in sLeaser.sprites)
            {
                sprite.RemoveFromContainer();
                newContatiner.AddChild(sprite);
            }
        }

        public override void Destroy()
        {
            positionedSoundEmitter.Destroy();
            base.Destroy();
        }

        Player focusedPlayer;


        int CurSenders { get { return senderList.Count; } }
        int curMaxSenders = 5;
        int nextSenderCd = 0;
        int totalSprites = 0;
        int endingCounter = 0;
        bool init = false;
        float rad = 200f;

        int glphyCount;
        GlyphPoint[] glphys;

        Queue<CreatureTemplate.Type> creatureQueue;
        public List<CreatureEndingSender> senderList;

        Mesh3D[] rects;
        Mesh3DRenderer[] renderers;
        Vector3[] rotVels;
        float[] stdScales;
        float[] scales;
        LightSource light;

        EndingMessageSender sender;

        PositionedSoundEmitter positionedSoundEmitter;

        public Color color = LaserDroneGraphics.defaultLaserColor;
        public Vector2 centerPos;

        public class GlyphPoint
        {
            public Vector2 pos;
            public Vector2 vel;
            public int life;
        }

        public class EndingMessageSender : CosmeticSprite
        {
            public const int RECT_COUNT = 4;

            public void FoolSetScale(Mesh3D mesh, float scale)
            {
                scales[rects.IndexOf(mesh)] = scale;
                for (int i = 0; i < mesh.origVertices.Count; i++)
                    mesh.animatedVertice[i] = scale * mesh.origVertices[i];

            }
            public EndingMessageSender(DroneMasterEnding owner)
            {
                this.owner = owner;
                rects = new Mesh3D[RECT_COUNT];
                renderers = new Mesh3DRenderer[RECT_COUNT];
                stdScales = new float[RECT_COUNT];
                scales = new float[RECT_COUNT];
                totalSprites = 1;
                color = owner.color;
                origPos = new Vector2[sideSprites];
                Mesh3D.TriangleFacet[] rect = new Mesh3D.TriangleFacet[2]
                {
                    new Mesh3D.TriangleFacet(0,1,2),
                    new Mesh3D.TriangleFacet(0,2,3)
                };


                for (int i = 0; i < RECT_COUNT; i++)
                {
                    Mesh3D mesh = rects[i] = new Mesh3D();
                    mesh.SetFacet(rect);
                    mesh.rotation = new Vector3(20, 0, 0);
                    mesh.SetVertice(0, Vector3.left + Vector3.down);
                    mesh.SetVertice(1, Vector3.right + Vector3.down);
                    mesh.SetVertice(2, Vector3.right + Vector3.up);
                    mesh.SetVertice(3, Vector3.left + Vector3.up);
                    stdScales[i] = i * -6 + 100;
                    FoolSetScale(mesh, 0);

                    renderers[i] = new Mesh3DFacetRenderer(mesh, totalSprites, "pixel", false);
                    renderers[i].shader = "LineMask";
                    renderers[i].SetUV(0, new Vector2(0, 0));
                    renderers[i].SetUV(1, new Vector2(1, 0));
                    renderers[i].SetUV(2, new Vector2(1, 1));
                    renderers[i].SetUV(3, new Vector2(0, 1));
                    renderers[i].SetVerticeColor(color, true);
                    renderers[i].SetVerticeColor(color, false);
                    totalSprites += renderers[i].totalSprites;
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[totalSprites];
                sLeaser.sprites[0] = new FSprite("Futile_White");
                sLeaser.sprites[0].shader = room.game.rainWorld.Shaders["Hologram"];
                sLeaser.sprites[0].color = color;
                sLeaser.sprites[0].anchorY = 0;


                foreach (var render in renderers)
                    render.InitSprites(sLeaser, rCam);

                AddToContainer(sLeaser,rCam,null);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {

                for (int i = 0; i < rects.Length; i++)
                {
                    for (int j = 0; j < renderers[i].totalSprites;j++)
                       sLeaser.sprites[j]._renderLayer?._material?.SetFloat("_Width", Mathf.Lerp(0.6f,0.05f,Timer(i)));
                    renderers[i].DrawSprites(sLeaser, rCam, timeStacker, camPos,owner.centerPos + Vector2.up * (35 + 25 *i));
                }
                sLeaser.sprites[0].SetPosition(owner.centerPos- camPos);
                sLeaser.sprites[0].height = Mathf.Clamp01(((counter-10)/ 25f))* 700f;
                sLeaser.sprites[0].width = Mathf.Lerp(40,20,(counter / 20f));

 

            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                counter++;
                for(int i=0; i<rects.Length;i++)
                {

                    rects[i].rotation = Vector3.Lerp(new Vector3(50, 0, 0), new Vector3(60, 90, 0), Mathf.Pow(Timer(i),2));
                    FoolSetScale(rects[i], Timer(i) * stdScales[i]);
                    renderers[i].Update();
                }
            }

            float Timer(int i)
            {
                return Mathf.Clamp01((counter - i * 5) / 15f);
            }
            Mesh3D[] rects;
            Mesh3DRenderer[] renderers;
            float[] stdScales;
            float[] scales;
            Vector2[] origPos;

            DroneMasterEnding owner;

            int totalSprites;
            Color color;
            int counter;

            int sideSprites;
        }



        public class CreatureEndingSender : CosmeticSprite, IOwnProjectedCircles
        {

            //Type也是临时用
            public CreatureEndingSender(DroneMasterEnding owner, CreatureTemplate.Type type,Room room,Vector2 pos, Color color)
            {
                this.type = type;
                this.room = room;
                this.pos = pos;
                var state = Random.state;
                this.owner = owner;
                this.color = color;
                offestDir = new Vector2(Random.Range(0.3f,1f),Random.Range(-1f,1f)).normalized;
                Random.InitState(114514 + type.index);

                vel = Custom.RNV() * Random.value *3;
                icon = new FSprite(CreatureSymbol.SpriteNameOfCreature(new CreatureSymbol.IconSymbolData(type, AbstractPhysicalObject.AbstractObjectType.Creature, 0)), true) { isVisible = false, alpha = 1f };
                icon.color = color;
                icon.shader = room.game.rainWorld.Shaders["Hologram"];

                stdRad *= Mathf.Clamp(StaticWorld.GetCreatureTemplate(type).bodySize, 0.75f, 1.25f);

                //这里想办法传一下东西就行
                bool[] allBools = new bool[] { Random.value<0.5,  Random.value < 0.2, Random.value < 0.9, Random.value < 0.5, Random.value < 0.5,Random.value < 0.5,false };

                compressed = new TagCompressed(this,type.index,allBools.Length, ref spriteCount, color);
                castLine = new TagCastLine(ref spriteCount, color,room,pos,pos,0);

                for (int i=0;i<allBools.Length;i++)
                {
                    messages[i] = new TagMessage(room,allBools[i],ref spriteCount, i, color);
                    overlays[i] = new TagOverlay(messages[i], compressed, ref spriteCount,i, color);
                    allParts.Enqueue(messages[i]);
                    allParts.Enqueue(overlays[i]);
                }
                

                room.AddObject(creatureCircle = new ScanProjectedCircle(room, this, 1, 0f, color, null /*ownerCircle*/));

                Random.state = state;
            }

            public override void Update(bool eu)
            {
                if (slatedForDeletetion)
                    return;
                base.Update(eu);

                count++;
                //二部分
                if (isMoveState)
                {
                    castLine.fromPos = pos;
                    castLine.toPos = owner.centerPos;
                    castLine.rad = 50f;
                    compressed.Update();

                    if (creatureCircle.circleThickness >= 0.99f)
                        icon.isVisible = false;

                    if (creatureCircle.rad<0.01f)
                    {
                        if (owner != null)
                            owner.senderList.Remove(this);
                        Plugin.Log("DM Ending : current sender count" + owner.CurSenders.ToString() + "max sender count" + owner.curMaxSenders);
                        slatedForDeletetion = true;

                        room.AddObject(new DataWave(room, owner.centerPos, 300f, 30f, 80, 30f));
                    }

                    if (count < 140)
                    {
                        vel = Vector2.zero;
                        creatureCircle.circleThickness = 0.07f;
                        creatureCircle.getToRad = stdRad * 1.0f;
                        pos = Vector2.Lerp(pos, owner.centerPos, 0.02f);

                        if (Custom.DistLess(pos, owner.centerPos, 25f))
                            count = 140;
                    }
                    else if(count > 140)
                    {
                        creatureCircle.circleThickness = 1f;
                        creatureCircle.getToRad = 0;
                    }

                }
                //一部分
                else
                {
                    vel *= 0.98f;
                    castLine.fromPos = owner.focusedPlayer.mainBodyChunk.pos;
                    castLine.toPos = pos;
                    castLine.rad = creatureCircle.rad/2;

                    if (creatureCircle.rad < stdRad - 0.05f && count < 100)
                    {
                        creatureCircle.getToRad = stdRad;
                        creatureCircle.circleThickness = 1f;
                    }
                    else if (creatureCircle.rad < stdRad * 1.5f - 0.05f && count < 100)
                    {
                        creatureCircle.circleThickness = 0.1f;
                        creatureCircle.getToRad = stdRad * 1.5f;

                    }
                    else if (count >= 120)
                    {
                        if (allParts.Count != 0)
                        {
                            allParts.First().Update();
                            if (allParts.First().NeedDelete)
                                allParts.Dequeue();
                        }
                        else
                        {
                            count = 0;
                            isMoveState = true;
                        }
                        compressed.Update();

                        creatureCircle.circleThickness = 0.1f;
                        creatureCircle.getToRad = stdRad * 1.5f;
                    }
                }
                castLine.Update();
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[spriteCount+1];
                sLeaser.sprites[spriteCount] = icon;
                foreach (var i in messages)
                    i.InitiateSprites(sLeaser, rCam);

                foreach (var i in overlays)
                    i.InitiateSprites(sLeaser, rCam);


                compressed.InitiateSprites(sLeaser, rCam);
                castLine.InitiateSprites(sLeaser, rCam);

                AddToContainer(sLeaser, rCam, null);
                
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                if (slatedForDeletetion)
                {
                    if (room != null)
                    {
                        sLeaser.CleanSpritesAndRemove();
                        room.RemoveObject(this);
                    }
                    return;
                }

                if(count == 30)
                    icon.isVisible = true;
                icon.SetPosition(pos - camPos);

                if (count >= 120)
                {
                    if (allParts.Count != 0)
                        allParts.First().DrawSprites(sLeaser, rCam, timeStacker, camPos, pos + OffestPos);
                }
                compressed.DrawSprites(sLeaser, rCam, timeStacker, camPos, pos + OffestPos);
                castLine.DrawSprites(sLeaser, rCam, timeStacker, camPos, pos);
            }

            Vector2 IOwnProjectedCircles.CircleCenter(int index, float timeStacker)
            {
                return pos;
            }

            Room IOwnProjectedCircles.HostingCircleFromRoom()
            {
                return room;
            }

            bool IOwnProjectedCircles.CanHostCircle()
            {
                return true;
            }

            int count = 0;
            int spriteCount = 0;
            bool isMoveState = false;
            CreatureTemplate.Type type;
            Vector2 offestDir;

            TagMessage[] messages = new TagMessage[7];
            TagOverlay[] overlays = new TagOverlay[7];
            TagCompressed compressed;
            TagCastLine castLine;


            Queue<SenderPart> allParts = new Queue<SenderPart>();
            DroneMasterEnding owner;

            ScanProjectedCircle creatureCircle;

            public Vector2 OffestPos { get 
                    {
                        if (creatureCircle != null)
                            return offestDir * (creatureCircle.Rad(0) * 1.5f);
                        else
                            return offestDir * stdRad * 1.5f;
                    } }

            public float stdRad = 40f;
            public static readonly float glphyScale = 1.5f;

            public float CurRad(float timeStacker)
            {
                return creatureCircle.Rad(timeStacker);
            }

            FSprite icon;
            public Color color;
        }

        public class SenderPart
        {
            public int totalSprites;
            public int startSprite;
            public int counter;
            public int filterIndex;
            public readonly int yBias = /*17*/0;

            public Room room;

            bool isInit = false;

            public SenderPart(int startSprite, int filterIndex, Color? color, Room room)
            {
                this.startSprite = startSprite;
                this.filterIndex = filterIndex;

                if (color.HasValue)
                    this.color = color.Value;
                this.room = room;
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

        public class TagCastLine : SenderPart
        {
            public Vector2 fromPos;
            Vector2[] fPos;
            public Vector2 toPos;
            public float rad = 0;
            public bool HiddenAll;
            public TagCastLine(ref int startSprite, Color? color, Room room, Vector2 fromPos,Vector2 toPos, float rad) : base(startSprite, 0, color, room)
            {
                this.fromPos = fromPos;
                this.toPos = toPos;
                this.rad = rad;
                fPos = new Vector2[3];
                totalSprites = 3;
                SetSpriteOffest(ref startSprite);
            }

            public override void Update()
            {
                base.Update();
                for(int i=0; i<totalSprites; i++)
                    if(Random.value < 0.25f)
                        fPos[i] = toPos + (Custom.RNV() * rad * Mathf.Pow(Random.value, 2f) / 2f);
                
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                base.InitiateSprites(sLeaser, rCam);
                for(int i=0;i<totalSprites;i++)
                {
                    sLeaser.sprites[i + startSprite] = new FSprite("pixel");
                    sLeaser.sprites[i + startSprite].color = color;
                    sLeaser.sprites[i + startSprite].SetAnchor(Vector2.zero);
                    sLeaser.sprites[i + startSprite].width = 1.5f;
                }
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 pos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos, pos);
                if (HiddenAll)
                {
                    for (int i = 0; i < totalSprites; i++)
                        sLeaser.sprites[i + startSprite].isVisible = false;
                    return;
                }
                for (int i = 0; i < totalSprites; i++)
                {
                    if (Random.value < 0.25f)
                    {
                        sLeaser.sprites[i + startSprite].isVisible = false;
                        continue;
                    }
                    sLeaser.sprites[i + startSprite].isVisible = true;
                    sLeaser.sprites[i + startSprite].x = fromPos.x - camPos.x;
                    sLeaser.sprites[i + startSprite].y = fromPos.y - camPos.y;
                    sLeaser.sprites[i + startSprite].rotation = Custom.AimFromOneVectorToAnother(fromPos, fPos[i]);
                    sLeaser.sprites[i + startSprite].height = Vector2.Distance(fromPos, fPos[i]);
                }
            }
        }
        public class TagCompressed : SenderPart
        {
            public int seed = 0;
            public int[] counts;
            public float[] toDeg;


            float degVel = 20;
            float degOffest = 0;

            CreatureEndingSender owner;


            public TagCompressed(CreatureEndingSender owner, int seed, int length,ref int startSprite, Color? color = null) : base(startSprite, 0, color, owner.room)
            {
                this.seed = seed;
                this.owner = owner;
                totalSprites = length;
                counts = new int[totalSprites];
                toDeg = new float[totalSprites];
                for (int i=0;i<totalSprites;++i) 
                { 
                    counts[i] = -1;
                    toDeg[i] = i * 360 / totalSprites;
                }
                SetSpriteOffest(ref startSprite);
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = 0; i < totalSprites; i++)
                {
                    sLeaser.sprites[startSprite + i] = new FSprite("BigGlyph" + (seed + i)%13);
                    sLeaser.sprites[startSprite + i].scale = CreatureEndingSender.glphyScale;
                    sLeaser.sprites[startSprite + i].isVisible = false;
                    sLeaser.sprites[startSprite + i].color = color;
                    sLeaser.sprites[startSprite + i].shader = rCam.room.game.rainWorld.Shaders["Hologram"];
                    sLeaser.sprites[startSprite + i].SetAnchor(Vector2.zero);
                }
                    
            }

            public void ShowGlyph(RoomCamera.SpriteLeaser sLeaser,int index)
            {

                sLeaser.sprites[startSprite + index].isVisible = true;
                counts[index] = 0;

            }

            public override void Update()
            {
                base.Update();
                for(int i=0;i< totalSprites;i++)
                    if (counts[i] != -1) 
                        counts[i]++;

                degOffest += degVel / 40;


            }

            public override bool NeedDelete => false;

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 pos)
            {
                for (int i = 0; i < totalSprites; i++)
                {
                    //TODO : 改成offest vector
                    var offestCenter = (pos - owner.OffestPos);
                    var radVec = Custom.DegToVec(toDeg[i] + degOffest) * owner.CurRad(timeStacker) * 1.5f;
                    sLeaser.sprites[startSprite + i].x = Mathf.Lerp(pos.x,offestCenter.x + radVec.x,Timer(i)) - camPos.x;
                    sLeaser.sprites[startSprite + i].y = Mathf.Lerp(pos.y - i* yBias, offestCenter.y + radVec.y, Timer(i)) - camPos.y;
                    sLeaser.sprites[startSprite + i].scale = CreatureEndingSender.glphyScale * Mathf.Clamp01(owner.CurRad(timeStacker)/50f);
                    sLeaser.sprites[startSprite + i].anchorX = Timer(i) * 0.5f;
                    sLeaser.sprites[startSprite + i].anchorY = Timer(i) * 0.5f;
                    sLeaser.sprites[startSprite + i].alpha = Mathf.Clamp01(owner.CurRad(timeStacker) / 50f);
                }

            }

            float Timer(int index)
            {
                return Mathf.SmoothStep(0,1,Mathf.Pow(Mathf.Clamp01((counts[index]-10f) / 30f),0.6f));
            }
        }

        public class TagOverlay : SenderPart
        {

            public TagMessage message;
            public TagCompressed compressed;
            public int pixelPreCounter = 2;

            public float bigWidth = 15 * CreatureEndingSender.glphyScale;
            public float bigHeight= 20 * CreatureEndingSender.glphyScale;

            int endCount = 110;
            float width;
            float height;

            public override bool NeedDelete { get => counter > endCount; }
            public TagOverlay(TagMessage message, TagCompressed compressed , ref int startSprite, int filterIndex, Color? color = null) : base(startSprite,filterIndex, color, compressed.room)
            {
                this.message = message;
                this.compressed = compressed;

                totalSprites = 4+2;

                width = (message.glyphs.Length+2) * 10;

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
                for (int j = 0; j < 4; j++)
                    sprite.verticeColors[j] = color;

                sLeaser.sprites[startSprite + totalSprites - 1].shader = rCam.room.game.rainWorld.Shaders["Hologram"];
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
                    var lerpTimer = Mathf.SmoothStep(0, 1, (counter - 40) / 20f);

                    var sprite = sLeaser.sprites[startSprite + totalSprites - 1] as CustomFSprite;

                    sprite.MoveVertice(0, starPos - camPos + new Vector2(width * (1 - lerpTimer), 0));
                    sprite.MoveVertice(1, starPos - camPos + new Vector2(width * (1 - lerpTimer), height));
                    sprite.MoveVertice(2, starPos - camPos + new Vector2(width, height));
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
                   
                    var heightTimer = Mathf.SmoothStep(0, 1, (counter - 60) / 30f);
                    var widthTimer = Mathf.SmoothStep(0, 1, (counter - 70) / 20f);
                    var tmpWidth = Mathf.Lerp(width, bigWidth, widthTimer);
                    var tmpHeight = Mathf.Lerp(height,bigHeight, heightTimer);

                    var sprite = sLeaser.sprites[startSprite + totalSprites - 1] as CustomFSprite;

                    sprite.MoveVertice(0, starPos - camPos + new Vector2(0, 0));
                    sprite.MoveVertice(1, starPos - camPos + new Vector2(0, tmpHeight));
                    sprite.MoveVertice(2, starPos - camPos + new Vector2(tmpWidth, tmpHeight));
                    sprite.MoveVertice(3, starPos - camPos + new Vector2(tmpWidth, 0));

                }
                else if(counter == 90)
                {
                    compressed.ShowGlyph(sLeaser, filterIndex);
                }
                else if(counter < 100)
                {
                    var lerpTimer = Mathf.SmoothStep(0, 1, (counter - 90) / 10f);

                    var sprite = sLeaser.sprites[startSprite + totalSprites - 1] as CustomFSprite;

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
            public TagMessage(Room room,bool check,ref int startSprite, int filterIndex,Color? color = null) : base(startSprite, filterIndex, color,room)
            {
                this.check = check;
                totalSprites = 1;

                if(color != null)
                    this.color = color.Value;   

                var state = Random.state;
                Random.InitState(10000 + filterIndex);

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
                    sLeaser.sprites[index].SetPosition(starPos.x + (represent.indexX * 10f) - camPos.x, starPos.y - represent.indexY * 10f - camPos.y);
                    sLeaser.sprites[index].isVisible = ((index - startSprite) * charTime) - 1 < counter;
                    if ((index * charTime) - 1 < counter && ((index - startSprite) * charTime) >= counter)
                        sLeaser.sprites[index].element = Futile.atlasManager.GetElementWithName("TinyGlyph" + Random.Range(0, 14).ToString());
                    else
                        sLeaser.sprites[index].element = Futile.atlasManager.GetElementWithName(represent.spriteName);
                    if(((index - startSprite) * charTime)== counter)
                        room.PlaySound(SoundID.SS_AI_Text, pos);
                    index++;
                }

                if ((index - startSprite) * charTime < counter && afterShowCounter == -1)
                {
                    room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Data_Bit, pos);
                    afterShowCounter = 0;
                }
            }
        }
    }

}


