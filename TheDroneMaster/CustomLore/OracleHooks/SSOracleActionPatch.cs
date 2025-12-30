using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using RWCustom;
using CustomSaveTx;

namespace TheDroneMaster
{
    public static class SSOracleActionPatch
    {
        public static void Patch()
        {
            On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
            On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;

            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        }

        private static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            orig.Invoke(self);
            int extralingerfactor = self.owner.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ? 1 : 0;

            if(self.id == DroneMasterEnums.Pebbles_DroneMaster_FirstMeet)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(".  .  ."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("A little animal, on the floor of my chamber, is this reaching you?"), extralingerfactor * 80));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It is obvious that you are not a product of natural evolution. I'm wondering who your creator are."), extralingerfactor * 80));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Sorry, I don't have any idea of the blueprints of your body,<LINE>and my communication array is already offline so I can't announce your existence to anyone else."), extralingerfactor * 120));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Due to the scan of parts of your structure, you have the ability to read and store information.<LINE>Your creator seems to want you to be able to gather all kinds of information across as far as possible."), extralingerfactor * 160));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("From the video sent back by my overseers, it seems that you will inflict violence on the scavengers."), extralingerfactor * 80));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Well, I would recommend you to go to my top city and say hello to the neighbors there."), extralingerfactor * 80));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Although a purposed organism similar to yours managed to kill their chief not long ago,<LINE>it looks like they continue to destroy my top structure."), extralingerfactor * 120));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Given that the purpose of your journey is to gather information, maybe then we can all get what we want."), extralingerfactor * 40));
                self.events.Add(new Conversation.WaitEvent(self, 40));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I have to get back to work, and I hope you gather the ideal information."), extralingerfactor * 80));
            }
            else if(self.id == DroneMasterEnums.Pebbles_DroneMaster_AfterMet)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh, you're back, little creature."), extralingerfactor * 40));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("How was your journey?"), extralingerfactor * 40)); 
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(".  .  ."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Apparently I can't get any useful information from your hollow eyes."), extralingerfactor * 60));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("If you can keep quiet, I may allow you to stop for a little longer."), extralingerfactor * 60));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("But now I have to keep working."), extralingerfactor * 40));
            }
            else if(self.id == DroneMasterEnums.Pebbles_DroneMaster_ExplainPackage)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh you're back again, little creature."), extralingerfactor * 60));
                self.events.Add(new Conversation.WaitEvent(self, 40));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Judging by your current state, I think you've found what you were looking for."), extralingerfactor * 80));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Your pack seems to want to send some data, and given that the only structure<LINE>around that can send data is the communications array, I would recommend you head there."), extralingerfactor * 120));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Although I won't be able to use it from here, you might be able to take your chances."), extralingerfactor * 80));
                self.events.Add(new Conversation.WaitEvent(self, 50));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Good luck then, I'll have to get back to my work."), extralingerfactor * 60));
            }
            else if(self.id == DroneMasterEnums.Pebbles_DroneMaster_ExplainPackageFirstMeet)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(".  .  ."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("A little animal, on the floor of my chamber, is this reaching you?"), extralingerfactor * 80));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It is obvious that you are not a product of natural evolution. I'm wondering who your creator are."), extralingerfactor * 80));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Sorry, I don't have any idea of the blueprints of your body,<LINE>and my communication array is already offline so I can't announce your existence to anyone else."), extralingerfactor * 120));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Due to the scan of parts of your structure, you have the ability to read and store information.<LINE>Your creator seems to want you to be able to gather all kinds of information across as far as possible."), extralingerfactor * 160));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Judging by your current state, I think you've found what you were looking for."), extralingerfactor * 80));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Your pack seems to want to send some data, and given that the only structure<LINE>around that can send data is the communications array, I would recommend you head there."), extralingerfactor * 120));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Although I won't be able to use it from here, you might be able to take your chances."), extralingerfactor * 80));
                self.events.Add(new Conversation.WaitEvent(self, 50));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Good luck then, I'll have to get back to my work."), extralingerfactor * 60));
            }
        }

        private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {
            PlayerModule module = null;
            foreach(var player in self.oracle.room.game.Players)
            {
                Player realizePlayer = player.realizedCreature as Player;
                if(realizePlayer != null && PlayerPatchs.modules.TryGetValue(realizePlayer, out module))
                {
                    break;
                }
            }

            if(module != null)
            {
                if (self.action != DroneMasterEnums.MeetDroneMaster)
                {
                    if (self.timeSinceSeenPlayer < 0)
                    {
                        self.timeSinceSeenPlayer = 0;
                    }

                    self.SlugcatEnterRoomReaction();
                    self.NewAction(DroneMasterEnums.MeetDroneMaster);
                }
            }
            else
            {
                orig.Invoke(self);
            }
        }

        private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
        {
            if(nextAction == DroneMasterEnums.MeetDroneMaster)
            {
                if (self.currSubBehavior.ID == DroneMasterEnums.Meet_DroneMaster) return;

                self.inActionCounter = 0;
                self.action = nextAction;

                SSOracleBehavior.SubBehavior subBehavior = null;
                for (int i = 0; i < self.allSubBehaviors.Count; i++)
                {
                    if (self.allSubBehaviors[i].ID == DroneMasterEnums.Meet_DroneMaster)
                    {
                        subBehavior = self.allSubBehaviors[i];
                        break;
                    }
                }

                if(subBehavior == null)subBehavior = new SSOracleMeetDroneMaster(self);
                self.allSubBehaviors.Add(subBehavior);

                subBehavior.Activate(self.action, nextAction);
                self.currSubBehavior.Deactivate();
                self.currSubBehavior = subBehavior;
            }
            else
            {
                orig.Invoke(self, nextAction);
            }
        }


        public class SSOracleMeetDroneMaster : SSOracleBehavior.ConversationBehavior
        {
            public int noMessageCount = 0;
            public bool initMessage = false;
            public PlayerModule Module => PlayerPatchs.modules.TryGetValue(player, out var module) ? module : null;

            public SSOracleMeetDroneMaster(SSOracleBehavior owner) : base(owner, DroneMasterEnums.Meet_DroneMaster, DroneMasterEnums.Pebbles_DroneMaster_FirstMeet)
            {
            }

            public override void Update()
            {
                if (player == null) return;

                if(!initMessage && !dialogBox.ShowingAMessage && dialogBox.messages.Count == 0)
                {
                    var scannedSaveUnit = DeathPersistentSaveDataRx.GetTreatmentOfType<ScannedCreatureSaveUnit>();
                    var pebbleConvSaveUnit = DeathPersistentSaveDataRx.GetTreatmentOfType<SSConversationStateSaveUnit>();

                    if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0)
                    {
                        if (scannedSaveUnit.KingScanned && !pebbleConvSaveUnit.explainPackage)
                        {
                            pebbleConvSaveUnit.explainPackage = true;
                            owner.InitateConversation(DroneMasterEnums.Pebbles_DroneMaster_ExplainPackageFirstMeet, this);
                        }
                        else
                            owner.InitateConversation(DroneMasterEnums.Pebbles_DroneMaster_FirstMeet, this);
                    }
                    else
                    {
                        if(scannedSaveUnit.KingScanned && !pebbleConvSaveUnit.explainPackage)
                        {
                            pebbleConvSaveUnit.explainPackage = true;
                            owner.InitateConversation(DroneMasterEnums.Pebbles_DroneMaster_ExplainPackage, this);
                        }
                        else
                            owner.InitateConversation(DroneMasterEnums.Pebbles_DroneMaster_AfterMet, this);
                    }

                    oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
                    initMessage = true;
                }
                
                if(!dialogBox.ShowingAMessage && dialogBox.messages.Count == 0 && initMessage)
                {
                    noMessageCount++;
                }

                if(noMessageCount > 40)
                {
                    owner.getToWorking = 1f;
                }
            }
        }
    }
}
