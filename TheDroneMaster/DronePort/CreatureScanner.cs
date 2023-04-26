using CoralBrain;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster
{
    public class CreatureScanner : CosmeticSprite, IOwnProjectedCircles
    {
        public static int showFilterFrameSpan = 20;
        public static float xBias = 30f;

        public Creature target;
        public PlayerPatchs.PlayerModule playerModule;
        public int scanProgress = 0;

        public ScanProjectedCircle ownerCircle;
        public ScanProjectedCircle targetCircle;

        public FSprite icon;

        public Color color;

        public bool iconVisible = true;
        public bool glyphVisible = true;

        public float ownerCircleRad = 20f;
        public float targetCircleRad;

        public List<GlyphRepresent> glyphRepresents = new List<GlyphRepresent>();
        public List<FilterTags> filterTags = new List<FilterTags>();
        public int totalGlyphCount = 0;
        public int totalSprites = 0;

        public float revalGlyphIndex = -1;
        public float framesToRevalOne = 2f;
        public int revalFilterIndex = -1;


        public int ScanCounter = 200;
        public int ShowGlyphCounter;
        public int BlinkCounter;
        public int ShowFilterCounter;

        public CreatureScanner(Creature target, Vector2 hoverPos, Room room, Color color, PlayerPatchs.PlayerModule module) : base()
        {
            this.room = room;
            this.target = target;
            this.color = color;
            playerModule = module;
            pos = hoverPos;

            targetCircleRad = target.mainBodyChunk.rad * 5f;
            ownerCircle = new ScanProjectedCircle(room, this, 0, 0f, color);
            targetCircle = new ScanProjectedCircle(room, this, 1, 0f, color, ownerCircle);

            room.AddObject(ownerCircle);
            room.AddObject(targetCircle);

            SetUpGlyph();
            SetUpFilter();

            ShowGlyphCounter = ScanCounter + (int)(totalGlyphCount * framesToRevalOne);
            BlinkCounter = ShowGlyphCounter + 200;
            ShowFilterCounter = BlinkCounter + showFilterFrameSpan * filterTags.Count;
        }

        public void SetUpFilter()
        {
            bool usingAI = false;
            bool usingRelation = false;
            bool reactToSocialEvents = false;
            bool usingfriendTracker = false;
            bool isScav = false;
            bool isScavElite = false;
            bool isScavKing = false;

            if(target.abstractCreature.abstractAI.RealAI != null)
            {
                usingAI = true;
                var ai = target.abstractCreature.abstractAI.RealAI;
                usingRelation = ai is IUseARelationshipTracker;
                reactToSocialEvents = ai is IReactToSocialEvents;
                usingfriendTracker = ai is FriendTracker.IHaveFriendTracker;
            }
            if(target is Scavenger)
            {
                isScav = true;
                Scavenger scav = target as Scavenger;
                isScavElite = scav.Elite || scav.King;
                isScavKing = scav.King;

                if (isScavKing)// force all true
                {
                    usingfriendTracker = true;
                }
            }

            bool[] allBools = new bool[] { usingAI, usingfriendTracker, usingRelation, reactToSocialEvents, isScav, isScavElite, isScavKing };

            int filterIndex = 0;
            while(filterIndex < allBools.Length)
            {
                var newFilter = new FilterTags(target, this, 10000 + filterIndex,allBools[filterIndex], totalSprites, filterIndex);
                totalSprites += newFilter.totalSprites;

                filterTags.Add(newFilter);
                filterIndex++;
            }
        }

        public void SetUpGlyph()
        {
            Plugin.Log("Set up glyph for : " + target.GetType().Name);
            int methodCount = target.GetType().GetMethods().Length - typeof(Creature).GetMethods().Length;
            methodCount *= 2;
            int fieldCount = target.GetType().GetFields().Length - typeof(Creature).GetFields().Length;

            var state = Random.state;
            Random.InitState(114514 + target.abstractCreature.creatureTemplate.type.index);

            int x = 2;
            int y = 0;

            for (int i = 0; i < methodCount + fieldCount; i++)
            {
                if (Random.value < 0.2f && i > 0) glyphRepresents.Add(new GlyphRepresent("", true, false, ref x, ref y));
                else if (Random.value < 0.02f && i > 25) glyphRepresents.Add(new GlyphRepresent("", false, true, ref x, ref y));
                else
                {
                    glyphRepresents.Add(new GlyphRepresent("TinyGlyph" + Random.Range(0, 14).ToString(), false, false, ref x, ref y));
                    totalGlyphCount++;
                }
            }

            totalSprites = 1 + totalGlyphCount;
            Random.state = state;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[totalSprites];
            icon = new FSprite(CreatureSymbol.SpriteNameOfCreature(CreatureSymbol.SymbolDataFromCreature(target.abstractCreature)), true) { isVisible = iconVisible, alpha = 1f };

            sLeaser.sprites[0] = icon;

            int index = 1;
            foreach (var represent in glyphRepresents)
            {
                if (represent.isSpaceOrChangeLine) continue;
                sLeaser.sprites[index] = new FSprite(represent.spriteName, true) { x = pos.x + represent.indexX * 10f - rCam.pos.x, y = pos.y - represent.indexY * 10f - rCam.pos.y, isVisible = false, color = color };
                index++;
            }

            foreach(var filter in filterTags)
            {
                filter.InitiateSprites(sLeaser, rCam);
            }

            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("BackgroundShortcuts"));
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = color;
            //sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["Hologram"];
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[0].SetPosition(pos - camPos);
            sLeaser.sprites[0].isVisible = iconVisible;

            int index = 1;
            foreach (var represent in glyphRepresents)
            {
                if (represent.isSpaceOrChangeLine) continue;
                sLeaser.sprites[index].SetPosition(pos.x + represent.indexX * 10f + xBias - rCam.pos.x, pos.y - represent.indexY * 10f - rCam.pos.y);
                sLeaser.sprites[index].isVisible = (glyphRepresents.IndexOf(represent) <= revalGlyphIndex) && glyphVisible;
                index++;
            }

            foreach(var filter in filterTags)
            {
                filter.DrawSprites(sLeaser, rCam, timeStacker, camPos, pos + xBias * Vector2.right, revalFilterIndex);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            scanProgress++;

            playerModule.portGraphics.Cast(pos, ownerCircle.rad);
            if (scanProgress < ScanCounter) // 等待扫描
            {
                ownerCircle.getToRad = ownerCircleRad;
                ownerCircle.circleThickness = 1f;

                targetCircle.getToRad = targetCircleRad;
                targetCircle.circleThickness = 1f;
            }
            else if (scanProgress >= ScanCounter && scanProgress < ShowGlyphCounter) // 展示信息矩阵
            {
                ownerCircle.circleThickness = 0.1f;
                ownerCircle.getToRad = ownerCircleRad * 1.5f;

                targetCircle.circleThickness = 0.1f;
                targetCircle.getToRad = targetCircleRad * 1.5f;
                revalGlyphIndex += 1f / framesToRevalOne;
                if ((int)revalGlyphIndex - revalGlyphIndex == 0) room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Data_Bit, pos);
            }
            else if (scanProgress >= ShowGlyphCounter && scanProgress < BlinkCounter) // 矩阵闪烁
            {
                ownerCircle.circleThickness = 0.1f;
                ownerCircle.getToRad = ownerCircleRad * 1.5f;

                targetCircle.circleThickness = 0.1f;
                targetCircle.getToRad = targetCircleRad * 1.5f;


                int deltaCounter = scanProgress - ShowGlyphCounter;

                if (deltaCounter % 30 == 0)
                {
                    glyphVisible = !glyphVisible;
                    room.PlaySound(SoundID.SS_AI_Text_Blink, pos);
                }
            }
            else if(scanProgress >= BlinkCounter && scanProgress < ShowFilterCounter)
            {
                ownerCircle.circleThickness = 0.1f;
                ownerCircle.getToRad = ownerCircleRad * 1.5f;

                targetCircle.circleThickness = 0.1f;
                targetCircle.getToRad = targetCircleRad * 1.5f;


                glyphVisible = false;
                int delta = scanProgress - BlinkCounter;
                if(delta % showFilterFrameSpan == 0)
                {
                    revalFilterIndex++;
                    room.PlaySound(SoundID.SS_AI_Image, pos);
                }
            }
            else if(scanProgress >= ShowFilterCounter && scanProgress < ShowFilterCounter + 100)
            {
                ownerCircle.circleThickness = 0.1f;
                ownerCircle.getToRad = ownerCircleRad;

                targetCircle.circleThickness = 0.1f;
                targetCircle.getToRad = targetCircleRad;
            }
            else if(scanProgress >= ShowFilterCounter + 100 && ownerCircle.lastRad > 0)
            {
                ownerCircle.getToRad = 0f;
                targetCircle.getToRad = 0f;

                ownerCircle.circleThickness = 1f;
                targetCircle.circleThickness = 1f;
            }
            else
            {
                ownerCircle.Destroy();
                targetCircle.Destroy();
                Destroy();
            }
        }

        public bool CanHostCircle()
        {
            return !slatedForDeletetion;
        }

        public Vector2 CircleCenter(int index, float timeStacker)
        {
            if (index == 0) return pos;
            else return target.DangerPos;
        }

        public Room HostingCircleFromRoom()
        {
            return room;
        }

        public class GlyphRepresent
        {
            public static int maxGlyphLineCount = 20;

            public string spriteName;
            public bool isSpaceOrChangeLine;

            public int indexX;
            public int indexY;
            public GlyphRepresent(string spriteName, bool isSpace, bool isChangeLine, ref int lastIndexX, ref int lastIndexY)
            {
                this.spriteName = spriteName;
                this.isSpaceOrChangeLine = isSpace | isChangeLine;

                lastIndexX++;
                indexX = lastIndexX;
                indexY = lastIndexY;

                if (isChangeLine)
                {
                    lastIndexX = 2;
                    lastIndexY++;
                }
                else if (indexX == maxGlyphLineCount)
                {
                    lastIndexX = 0;
                    lastIndexY++;
                }
            }
        }

        public class FilterTags
        {
            public static float yBias = 15f;

            public CreatureScanner scanner;
            public GlyphRepresent[] glyphs;

            public bool check;
            public int startIndex;
            public int filterIndex;

            public int totalSprites;

            public FilterTags(Creature creature, CreatureScanner scanner,int seed,bool check,int startIndex,int filterIndex)
            {
                this.scanner = scanner;
                this.check = check;
                this.startIndex = startIndex;
                this.filterIndex = filterIndex;

                totalSprites = 1;

                var state = Random.state;
                Random.InitState(seed);

                glyphs = new GlyphRepresent[Random.Range(5, 15)];
                int x = 1; //第一个是选择框
                int y = 0;
                for(int i = 0;i < glyphs.Length; i++)
                {
                    if (Random.value < 0.2f && i > 0) glyphs[i] = new GlyphRepresent("", true, false, ref x, ref y);
                    else
                    {
                        glyphs[i] = new GlyphRepresent("TinyGlyph" + Random.Range(0, 14).ToString(), false, false, ref x, ref y);
                        totalSprites++;
                    }
                }
                Random.state = state;
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[startIndex] = new FSprite(check ? Plugin.trueRectName : Plugin.falseRectName, true) { color = scanner.color,scale = 0.5f};
                int index = 1;
                for(int i = 0;i < glyphs.Length; i++)
                {
                    var glyph = glyphs[i];
                    if (glyph.isSpaceOrChangeLine) continue;
                    sLeaser.sprites[startIndex + index] = new FSprite(glyph.spriteName, true) { color = scanner.color};
                    index++;
                }
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos,Vector2 pos,int currentShowFilterIndex)
            {
                Vector2 starPos = pos + filterIndex * Vector2.down * yBias;
                sLeaser.sprites[startIndex].SetPosition(starPos - camPos);
                sLeaser.sprites[startIndex].isVisible = currentShowFilterIndex >= filterIndex;

                int index = 1 + startIndex;
                foreach (var represent in glyphs)
                {
                    if (represent.isSpaceOrChangeLine) continue;
                    sLeaser.sprites[index].SetPosition(starPos.x + represent.indexX * 10f - rCam.pos.x, starPos.y - represent.indexY * 10f - rCam.pos.y);
                    sLeaser.sprites[index].isVisible = currentShowFilterIndex >= filterIndex;
                    index++;
                }
            }
        }
    }

    public class ScanProjectedCircle : ProjectedCircle
    {
        public float circleThickness = 1f;
        public float lastThickness = 1f;
        public float smoothThickness = 1f;

        Color color;
        public ScanProjectedCircle connected;

        public ScanProjectedCircle(Room room, IOwnProjectedCircles owner, int index, float size, Color color, ScanProjectedCircle connected = null) : base(room, owner, index, size)
        {
            getToRad = size;
            baseRad = size;
            rad = 0f;
            this.color = color;
            depthOffset = 0f;

            spokes = 6;

            this.connected = connected;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            connectedCircles.Clear();
            offScreenConnections = new Vector2[0];
            if (connected != null) connectedCircles.Add(connected);
            connectionsBlink = new int[connectedCircles.Count + offScreenConnections.Length];

            base.InitiateSprites(sLeaser, rCam);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i != 0) sLeaser.sprites[i].color = color;
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[1].alpha = smoothThickness;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            updateDepth = false;
            depthOffset = 0f;

            smoothThickness = Mathf.Lerp(lastThickness, circleThickness, 0.1f);
            lastThickness = smoothThickness;
        }
    }
}
