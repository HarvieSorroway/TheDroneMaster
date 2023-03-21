using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using RWCustom;


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
            if(self.id == DroneMasterEnums.Pebbles_DroneMaster_FirstMeet)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(".  .  ."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("A little animal, on the floor of my chamber, is this reaching you?"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It is obvious that you are not a product of natural evolution. I'm wondering who your creator are."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Sorry, I don't have any idea of the blueprints of your body,<LINE>and my communication array is already offline so I can't announce your existence to anyone else."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Due to the scan of parts of your structure, you have the ability to read and store information.<LINE>Your creator seems to want you to be able to gather all kinds of information across as far as possible."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("From the video sent back by my overseers, it seems that you will inflict violence on the scavengers."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Well, I would recommend you to go to my top city and say hello to the neighbors there."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Although a purposed organism similar to yours managed to kill their chief not long ago,<LINE>it looks like they continue to destroy my top structure."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Given that the purpose of your journey is to gather information, maybe then we can all get what we want."), 0));
                self.events.Add(new Conversation.WaitEvent(self, 40));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I have to get back to work, and I hope you gather the ideal information."), 0));
            }
            else if(self.id == DroneMasterEnums.Pebbles_DroneMaster_AfterMet)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh, you're back, little creature."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("How was your journey?"), 0)); 
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(".  .  ."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Apparently I can't get any useful information from your hollow eyes."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("If you can keep quiet, I may allow you to stop for a little longer."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("But now I have to keep working."), 0));
            }
        }

        private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {
            PlayerPatchs.PlayerModule module = null;
            foreach(var player in self.oracle.room.game.Players)
            {
                Player realizePlayer = player.realizedCreature as Player;
                if(realizePlayer != null && PlayerPatchs.modules.TryGetValue(realizePlayer,out module) && module.ownDrones)
                {
                    break;
                }
            }

            if(module != null && module.ownDrones)
            {
                if (self.action != DroneMasterEnums.MeetDroneMaster)
                {
                    if (self.timeSinceSeenPlayer < 0)
                    {
                        self.timeSinceSeenPlayer = 0;
                    }

                    self.SlugcatEnterRoomReaction();

                    if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                    {
                        self.NewAction(DroneMasterEnums.MeetDroneMaster);
                    }
                    else
                    {
                        self.afterGiveMarkAction = DroneMasterEnums.MeetDroneMaster;
                        self.NewAction(SSOracleBehavior.Action.General_GiveMark);
                    }
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
            public PlayerPatchs.PlayerModule Module => PlayerPatchs.modules.TryGetValue(player, out var module) ? module : null;

            public SSOracleMeetDroneMaster(SSOracleBehavior owner) : base(owner, DroneMasterEnums.Meet_DroneMaster, DroneMasterEnums.Pebbles_DroneMaster_FirstMeet)
            {
            }

            public override void Update()
            {
                if (player == null) return;

                if(!initMessage && !dialogBox.ShowingAMessage && dialogBox.messages.Count == 0)
                {
                    if (oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0)
                    {
                        owner.InitateConversation(DroneMasterEnums.Pebbles_DroneMaster_FirstMeet, this);
                    }
                    else
                    {
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
