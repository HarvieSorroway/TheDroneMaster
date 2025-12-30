
using CustomOracleTx;
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
            if (type != pearlObjectType)
                return null;
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
}
