using HUD;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DreamComponent.OracleHooks;
using TheDroneMaster.GameHooks;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace TheDroneMaster.CustomLore.SpecificScripts
{
    public class RoomSpecificScriptPatch
    {
        public static void PatchOn()
        {
            On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;
        }

        private static void RoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig.Invoke(room);
            Plugin.Log("{0},{1},{2}", room.game.IsStorySession, room.game.GetStorySession.saveState.saveStateNumber, room.abstractRoom.name);
            if (room.game.IsStorySession && room.game.GetStorySession.saveState.saveStateNumber == new SlugcatStats.Name(Plugin.DroneMasterName))
            {
                if (room.abstractRoom.name == "LC_FINAL")
                {
                    room.AddObject(new LC_BossFight(room));
                }
                //TODO : 结局判断
                if (room.abstractRoom.name == "SI_A07" && DeathPersistentSaveDataPatch.GetUnitOfType<ScannedCreatureSaveUnit>().KingScanned)
                {
                    Plugin.Log("Try Play Endding");
                    room.AddObject(new DroneMasterEnding(room));
                }
                if(room.abstractRoom.name == "DMD_LAB01")
                {
                    room.AddObject(new DMD_LAB01Lore(room));
                }
                if(room.abstractRoom.name == "DMD_ROOF")
                {
                    room.AddObject(new DMD_ROOFLore(room));
                }
            }
            
        }
    }

    public class LC_BossFight : UpdatableAndDeletable
    {
        public Player player;

        public bool triggeredBoss;
        public LC_BossFight(Room room)
        { 
            base.room = room;
            Plugin.Log("Add DroneMaster boss fight!");
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            AbstractCreature firstAlivePlayer = room.game.FirstAlivePlayer;
            if (firstAlivePlayer == null)
            {
                return;
            }

            if (DeathPersistentSaveDataPatch.GetUnitOfType<ScannedCreatureSaveUnit>().KingScanned)
            {
                Destroy();
                return;
            }


            if (player == null && room.game.Players.Count > 0 && firstAlivePlayer.realizedCreature != null && firstAlivePlayer.realizedCreature.room == room)
            {
                player = (firstAlivePlayer.realizedCreature as Player);
            }

            if (player != null && player.abstractCreature.Room == room.abstractRoom && player.room != null)
            {
                if (player.room.game.cameras[0] != null && player.room.game.cameras[0].currentCameraPosition == 0 && !player.sceneFlag)
                {
                    TriggerBossFight();
                }
            }
            else
            {
                player = null;
            }
        }

        public void TriggerBossFight()
        {
            if (!triggeredBoss)
            {
                Plugin.Log("Trigger DroneMaster boss fight!");
                //生成酋长尸体
                triggeredBoss = true;
                //player.sceneFlag = true;
                room.TriggerCombatArena();
                WorldCoordinate pos = new WorldCoordinate(room.abstractRoom.index, 122, 7, -1);
                AbstractCreature abstractCreature = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing), null, pos, room.game.GetNewID());
                abstractCreature.ID.setAltSeed(8875);
                abstractCreature.Die();
                abstractCreature.ignoreCycle = true;
                room.abstractRoom.AddEntity(abstractCreature);
                abstractCreature.RealizeInRoom();

                for(int i = 0; i< 15; i++)
                {
                    WorldCoordinate position = new WorldCoordinate(room.abstractRoom.index, 122 - i, 7, -1);
                    abstractCreature = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite), null, position, room.game.GetNewID());
                    abstractCreature.ignoreCycle = triggeredBoss;
                    room.abstractRoom.AddEntity(abstractCreature);
                    abstractCreature.RealizeInRoom();
                }
            }
        }
    }

    public class DMD_LAB01Lore : RoomLore
    {
        public DMD_LAB01Lore(Room room) : base(room, 200) 
        {
        }

        public override void AddTextEvents(DialogBox dialogBox)
        {
            events.Add(new TextEvent(this, ".  .  .", dialogBox, 0));
            events.Add(new TextEvent(this, "", dialogBox, 40));
            events.Add(new TextEvent(this, "Can you hear me little creature?", dialogBox, 0, SoundID.SL_AI_Talk_1));
            events.Add(new TextEvent(this, "You're now some distance from my room, <LINE>this is a perfect opportunity to test the functionality of your backpack!", dialogBox, 0, SoundID.SL_AI_Talk_2));
            events.Add(new TextEvent(this, "Please wait a moment for me to start the testing process. . .", dialogBox, 0, SoundID.SL_AI_Talk_3));
            events.Add(new TextEvent(this, "", dialogBox, 80));
            events.Add(new TextEvent(this, "Biomass reactor, check.<LINE>Drone port, check.<LINE>Scanning equipment, check.<LINE>Communication equipment, check<LINE>.  .  .", dialogBox, 0, SoundID.SL_AI_Talk_4));
            events.Add(new TextEvent(this, "Everything looks normal, how about trying to scan a creature next?", dialogBox, 0, SoundID.SL_AI_Talk_5));

            base.AddTextEvents(dialogBox);
        }
    }

    public class DMD_ROOFLore : RoomLore
    {
        public DMD_ROOFLore(Room room) : base(room, 240)
        {
        }

        public override void AddTextEvents(DialogBox dialogBox)
        {
            events.Add(new TextEvent(this, "This is your first time to the wall little creature, do you like the view there?", dialogBox, 0, SoundID.SL_AI_Talk_3));
            events.Add(new TextEvent(this, "You may be wondering what those buildings are.<LINE>They once belonged to my creators, but now they are long gone.", dialogBox, 0, SoundID.SL_AI_Talk_5));
            events.Add(new TextEvent(this, "Although you are about to travel far, I can still guide you in my precinct, so go down the left side next.", dialogBox, 0, SoundID.SL_AI_Talk_1));
            events.Add(new TextEvent(this, "Don't forget to watch your step when climbing those coolers", dialogBox, 0, SoundID.SL_AI_Talk_4));

            base.AddTextEvents(dialogBox);
        }
    }

    public class RoomLore : UpdatableAndDeletable
    {
        public int age;
        public int preWaitCounter;
        public List<TextEvent> events = new List<TextEvent>();
        public Player player;

        public bool inited = false;
        public RoomLore(Room room,int preWaitCounter)
        {
            base.room = room;
            this.preWaitCounter = preWaitCounter;
        }

        public void SetUp()
        {
            if (room.game.cameras == null || room.game.cameras[0].hud == null)
            {
                Plugin.Log("Set up failure : {0},{1}", room.game.cameras, room.game.cameras[0].hud);
                return;
            }

            for (int i = 0; i < room.game.cameras.Length; i++)
            {
                if (room.game.cameras[i].hud != null && room.game.cameras[i].followAbstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat && room.game.cameras[i].followAbstractCreature.Room == room.abstractRoom)
                {
                    if (room.game.cameras[i].hud.dialogBox == null)
                    {
                        room.game.cameras[i].hud.InitDialogBox();
                    }
                }
            }

           
            var dialogBox = room.game.cameras[0].hud.dialogBox;
            AddTextEvents(dialogBox);

            Plugin.Log("text events inited! {0}",events.Count);
        }

        public virtual void AddTextEvents(DialogBox dialogBox)
        {
            inited = true;
        }

        public override void Update(bool eu)
        {
            if (slatedForDeletetion)
                return;
            base.Update(eu);

            if (!inited)
            {
                SetUp();
                return;
            }

            AbstractCreature firstAlivePlayer = room.game.FirstAlivePlayer;
            if (firstAlivePlayer == null)
            {
                return;
            }
            if (player == null && room.game.Players.Count > 0 && firstAlivePlayer.realizedCreature != null && firstAlivePlayer.realizedCreature.room == room)
            {
                player = (firstAlivePlayer.realizedCreature as Player);
            }

            age++;
            if(age > preWaitCounter)
            {
                if(events.Count == 0)
                {
                    Destroy();
                    return;
                }
                var current = events[0];
                current.Update();
                if(current.IsOver)
                    events.RemoveAt(0);
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            DreamComponent.DreamHook.CustomDreamHook.currentActivateDream.EndDream(room.game);
        }

        public class TextEvent
        {
            public RoomLore owner;

            public string text;
            public DialogBox dialogBox;
            public int age;
            public int initWait;
            public int extraLinger;
            public bool activated = false;

            public SoundID partWithSound;

            public bool IsOver
            {
                get
                {
                    if (age < initWait)
                        return false;
                    return dialogBox.CurrentMessage == null;
                }
            }

            public TextEvent(RoomLore owner,string text, DialogBox dialogBox,int initWait, SoundID partWithSound = null,int extraLinger = 40,bool translate = true)
            {
                this.owner = owner;
                this.text = translate ? owner.room.game.rainWorld.inGameTranslator.Translate(text) : text;
                this.dialogBox = dialogBox;
                this.initWait = initWait;
                this.partWithSound = partWithSound;
                this.extraLinger = extraLinger;
            }

            public void Activate()
            {
                activated = true;
                if(text != string.Empty)
                    dialogBox.NewMessage(text, extraLinger);
                if (partWithSound != null)
                    owner.room.PlaySound(partWithSound, owner.player.mainBodyChunk, false, 0.8f, MIFOracleRegistry.MIFTalkPitch);

                Plugin.Log("New message {0}", text);
            }

            public void Update()
            {
                if (!activated && age >= initWait)
                    Activate();
                age++;
            }
        }
    }
}
