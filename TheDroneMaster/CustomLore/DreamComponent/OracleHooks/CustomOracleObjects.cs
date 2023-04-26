
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DreamComponent.OracleHooks
{
    public class CustomOraclePearlRegistry
    {
        public readonly AbstractPhysicalObject.AbstractObjectType pearlObjectType;
        public readonly DataPearl.AbstractDataPearl.DataPearlType dataPearlType;

        public CustomOraclePearlRegistry(string name) : this(new AbstractPhysicalObject.AbstractObjectType(name,true),new DataPearl.AbstractDataPearl.DataPearlType(name,true))
        {
        }

        public CustomOraclePearlRegistry(AbstractPhysicalObject.AbstractObjectType pearlObjectType,DataPearl.AbstractDataPearl.DataPearlType dataPearlType)
        {
            this.pearlObjectType = pearlObjectType;
            this.dataPearlType = dataPearlType;
        }

        /// <summary>
        /// 创建 CustomPearl 的实例
        /// </summary>
        /// <param name="abstractPhysicalObject"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public virtual CustomOrbitableOraclePearl RealizeDataPearl(AbstractPhysicalObject abstractPhysicalObject,World world)
        {
            return null;
        }

        public virtual CustomOrbitableOraclePearl.AbstractCustomOraclePearl GetAbstractCustomOraclePearl(World world, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID, int originRoom, int placedObjectIndex, PlacedObject.ConsumableObjectData consumableData, int color, int number)
        {
            return new CustomOrbitableOraclePearl.AbstractCustomOraclePearl(pearlObjectType, dataPearlType, world, realizedObject, pos, ID, originRoom, placedObjectIndex, consumableData, color, number);
        }

        public virtual CustomOrbitableOraclePearl.AbstractCustomOraclePearl AbstractPhysicalObjectFromString(AbstractPhysicalObject.AbstractObjectType type,World world,EntityID id,WorldCoordinate pos, string[] dataArray)
        {
            return new CustomOrbitableOraclePearl.AbstractCustomOraclePearl(pearlObjectType,dataPearlType,world,null,pos,id, int.Parse(dataArray[3]), int.Parse(dataArray[4]), null, int.Parse(dataArray[6]), int.Parse(dataArray[7]));
        }

        /// <summary>
        /// TODO : 以后再来写，哈哈
        /// </summary>
        /// <param name="self"></param>
        /// <param name="saveFile"></param>
        /// <param name="oneRandomLine"></param>
        /// <param name="randomSeed"></param>
        public virtual void LoadSLPearlConversation(SLOracleBehaviorHasMark.MoonConversation self, SlugcatStats.Name saveFile, bool oneRandomLine, int randomSeed)
        {
            switch (Random.Range(0, 5))
            {
                case 0:
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You would like me to read this?"), 10));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It's still warm... this was in use recently."), 10));
                    break;
                case 1:
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("A pearl... This one is crystal clear - it was used just recently."), 10));
                    break;
                case 2:
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Would you like me to read this pearl?"), 10));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Strange... it seems to have been used not too long ago."), 10));
                    break;
                case 3:
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("This pearl has been written to just now!"), 10));
                    break;
                default:
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let's see... A pearl..."), 10));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("And this one is fresh! It was not long ago this data was written to it!"), 10));
                    break;
            }
        }
    }

    public class CustomOrbitableOraclePearl : DataPearl
    {
        public Vector2? hoverPos;

        public float orbitAngle;
        public float orbitSpeed;
        public float orbitDistance;
        public float orbitFlattenAngle;
        public float orbitFlattenFac;

        public int orbitCircle;
        public int marbleColor;
        public int marbleIndex;

        public bool lookForMarbles;

        public GlyphLabel label;
        public Oracle oracle;
        public PhysicalObject orbitObj;
        public List<CustomOrbitableOraclePearl> otherMarbles;
        public bool NotCarried => grabbedBy.Count == 0;

        public CustomOrbitableOraclePearl(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world)
        {
            otherMarbles = new List<CustomOrbitableOraclePearl>();
            orbitAngle = Random.value * 360f;
            orbitSpeed = 3f;
            orbitDistance = 50f;
            collisionLayer = 0;
            orbitFlattenAngle = Random.value * 360f;
            orbitFlattenFac = 0.5f + Random.value * 0.5f;
        }

        public override void NewRoom(Room newRoom)
        {
            base.NewRoom(newRoom);
            UpdateOtherMarbles();
        }

        public virtual void UpdateOtherMarbles()
        {
            for (int i = 0; i < otherMarbles.Count; i++)
            {
                otherMarbles[i].otherMarbles.Remove(this);
            }
            otherMarbles.Clear();
            for (int j = 0; j < room.physicalObjects[collisionLayer].Count; j++)
            {
                if (room.physicalObjects[collisionLayer][j] is CustomOrbitableOraclePearl && room.physicalObjects[collisionLayer][j] != this)
                {
                    if (!(room.physicalObjects[this.collisionLayer][j] as CustomOrbitableOraclePearl).otherMarbles.Contains(this))
                    {
                        (room.physicalObjects[collisionLayer][j] as CustomOrbitableOraclePearl).otherMarbles.Add(this);
                    }
                    if (!otherMarbles.Contains(room.physicalObjects[collisionLayer][j] as CustomOrbitableOraclePearl))
                    {
                        otherMarbles.Add(room.physicalObjects[collisionLayer][j] as CustomOrbitableOraclePearl);
                    }
                }
            }
        }

        public override void Update(bool eu)
        {
            if (!lookForMarbles)
            {
                UpdateOtherMarbles();
                lookForMarbles = true;
            }
            if (oracle != null && oracle.room != room)
            {
                oracle = null;
            }
            abstractPhysicalObject.destroyOnAbstraction = (oracle != null);
            if (label != null)
            {
                label.setPos = new Vector2?(firstChunk.pos);
                if (label.room != room)
                {
                    label.Destroy();
                }
            }
            else
            {
                label = new GlyphLabel(firstChunk.pos, GlyphLabel.RandomString(1, 1, 12842 + (abstractPhysicalObject as AbstractCustomOraclePearl).number, false));
                room.AddObject(label);
            }
            base.Update(eu);
            float num = orbitAngle;
            float num2 = orbitSpeed;
            float num3 = orbitDistance;
            float axis = orbitFlattenAngle;
            float num4 = orbitFlattenFac;
            if (room.gravity < 1f && NotCarried && oracle != null)
            {
                if (ModManager.MSC && oracle != null && oracle.marbleOrbiting)
                {
                    int listCount = 1;
                    if(CustomOracleRegister.oracleEx.TryGetValue(oracle,out var customOralceEX))
                    {
                        listCount = customOralceEX.customMarbles.Count;
                    }
                    float num5 = (float)marbleIndex / (float)listCount;
                    num = 360f * num5 + (float)oracle.behaviorTime * 0.1f;
                    Vector2 vector = new Vector2(oracle.room.PixelWidth / 2f, oracle.room.PixelHeight / 2f) + Custom.DegToVec(num) * 275f;
                    firstChunk.vel *= Custom.LerpMap(firstChunk.vel.magnitude, 1f, 6f, 0.999f, 0.9f);
                    firstChunk.vel += Vector2.ClampMagnitude(vector - firstChunk.pos, 100f) / 100f * 0.4f * (1f - room.gravity);
                }
                else
                {
                    Vector2 vector2 = base.firstChunk.pos;
                    if (orbitObj != null)
                    {
                        int num6 = 0;
                        int num7 = 0;
                        int number = abstractPhysicalObject.ID.number;
                        for (int i = 0; i < otherMarbles.Count; i++)
                        {
                            if (otherMarbles[i].orbitObj == orbitObj && otherMarbles[i].NotCarried && Custom.DistLess(otherMarbles[i].firstChunk.pos, orbitObj.firstChunk.pos, otherMarbles[i].orbitDistance * 4f) && otherMarbles[i].orbitCircle == orbitCircle)
                            {
                                num3 += otherMarbles[i].orbitDistance;
                                if (otherMarbles[i].abstractPhysicalObject.ID.number < this.abstractPhysicalObject.ID.number)
                                {
                                    num7++;
                                }
                                num6++;
                                if (otherMarbles[i].abstractPhysicalObject.ID.number < number)
                                {
                                    number = otherMarbles[i].abstractPhysicalObject.ID.number;
                                    num = otherMarbles[i].orbitAngle;
                                    num2 = otherMarbles[i].orbitSpeed;
                                    axis = otherMarbles[i].orbitFlattenAngle;
                                    num4 = otherMarbles[i].orbitFlattenFac;
                                }
                            }
                        }
                        num3 /= (1 + num6);
                        num += (num7 * (360f / (num6 + 1)));
                        Vector2 vector3 = orbitObj.firstChunk.pos;
                        if (orbitObj is Oracle && orbitObj.graphicsModule != null)
                        {
                            vector3 = (orbitObj.graphicsModule as OracleGraphics).halo.Center(1f);
                        }
                        vector2 = vector3 + Custom.FlattenVectorAlongAxis(Custom.DegToVec(num), axis, num4) * num3 * Mathf.Lerp(1f / num4, 1f, 0.5f);
                    }
                    else if (hoverPos != null)
                    {
                        vector2 = hoverPos.Value;
                    }
                    firstChunk.vel *= Custom.LerpMap(base.firstChunk.vel.magnitude, 1f, 6f, 0.999f, 0.9f);
                    firstChunk.vel += Vector2.ClampMagnitude(vector2 - firstChunk.pos, 100f) / 100f * 0.4f * (1f - this.room.gravity);
                }
            }
            orbitAngle += num2 * ((orbitCircle % 2 == 0) ? 1f : -1f);
        }

        public virtual void DataPearlApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        } 


        public class AbstractCustomOraclePearl : AbstractDataPearl
        {
            public int color;
            public int number;
            public AbstractCustomOraclePearl(
                AbstractObjectType objectType,
                DataPearlType dataPearlType,

                World world, 
                PhysicalObject realizedObject, 
                WorldCoordinate pos, 
                EntityID ID, 
                int originRoom, 
                int placedObjectIndex, 
                PlacedObject.ConsumableObjectData consumableData, 
                int color, 
                int number) : base(world, objectType, realizedObject, pos, ID, originRoom, placedObjectIndex, consumableData, dataPearlType)
            {
                this.color = color;
                this.number = number;
            }

            public override string ToString()
            {
                string baseString = base.ToString();
                baseString += string.Format(CultureInfo.InvariantCulture, "<oA>{0}<oA>{1}", color, number);
                baseString = SaveState.SetCustomData(this, baseString);

                return SaveUtils.AppendUnrecognizedStringAttrs(baseString, "<oA>", this.unrecognizedAttributes);
            }
        }
    }



    public class CustomOraclePearlHook
    {
        static bool inited = false;

        public static List<CustomOraclePearlRegistry> registries = new List<CustomOraclePearlRegistry>();
        public static Dictionary<AbstractPhysicalObject.AbstractObjectType, CustomOraclePearlRegistry> typeToRegistry = new Dictionary<AbstractPhysicalObject.AbstractObjectType, CustomOraclePearlRegistry>();

        public static void Registry(CustomOraclePearlRegistry pearlRegistry)
        {
            if (registries.Contains(pearlRegistry)) return;
            registries.Add(pearlRegistry);
            typeToRegistry.Add(pearlRegistry.pearlObjectType, pearlRegistry);

            PatchOn();
        }

        #region Hooks
        public static void PatchOn()
        {
            if (inited) return;
            On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize;
            On.DataPearl.ApplyPalette += DataPearl_ApplyPalette;
            inited = true;
        }

        private static void DataPearl_ApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig.Invoke(self, sLeaser, rCam, palette);
            if(self is CustomOrbitableOraclePearl)
            {
                (self as CustomOrbitableOraclePearl).DataPearlApplyPalette(sLeaser, rCam, palette);
            }
        }

        private static void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            orig.Invoke(self);
            if(typeToRegistry.TryGetValue(self.type,out var registry))
            {
                self.realizedObject = registry.RealizeDataPearl(self, self.world);
            }
        }

        private static AbstractPhysicalObject SaveState_AbstractPhysicalObjectFromString(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
        {
            var result = orig.Invoke(world, objString);
            AbstractPhysicalObject abstractPhysicalObject = null;
            try
            {
                string[] array = Regex.Split(objString, "<oA>");
                EntityID id = EntityID.FromString(array[0]);
                AbstractPhysicalObject.AbstractObjectType abstractObjectType = new AbstractPhysicalObject.AbstractObjectType(array[1], false);
                WorldCoordinate pos = WorldCoordinate.FromString(array[2]);

                foreach (var registry in registries)
                {
                    abstractPhysicalObject = registry.AbstractPhysicalObjectFromString(abstractObjectType, world, id, pos, array);
                    if(abstractPhysicalObject != null)
                    {
                        result = abstractPhysicalObject;
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.Log(string.Concat(new string[]
                {
                    "[EXCEPTION] AbstractPhysicalObjectFromString: ",
                    objString,
                    " -- ",
                    ex.Message,
                    " -- ",
                    ex.StackTrace
                }));
                result = null;
            }

            return result;
        }
        #endregion

        public static AbstractPhysicalObject GetAbstractCustomPearlOfType(AbstractPhysicalObject.AbstractObjectType abstractObjectType, World world, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID, int originRoom, int placedObjectIndex, PlacedObject.ConsumableObjectData consumableData, int color, int number)
        {
            if (typeToRegistry.TryGetValue(abstractObjectType, out var registry))
            {
                return registry.GetAbstractCustomOraclePearl(world, realizedObject, pos, ID, originRoom, placedObjectIndex, consumableData, color, number);
            }
            return null;
        }

        public static CustomOrbitableOraclePearl GetRealizedCustomPearlOfType(AbstractPhysicalObject.AbstractObjectType abstractObjectType,AbstractPhysicalObject abstractPhysicalObject,World world)
        {
            if(typeToRegistry.TryGetValue(abstractObjectType,out var registry))
            {
                return registry.RealizeDataPearl(abstractPhysicalObject,world);
            }
            return null;
        }
    }
}
