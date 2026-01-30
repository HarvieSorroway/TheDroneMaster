using DMPS.PlayerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.PlayerHooks
{
    internal class JetJump : PlayerModule.PlayerModuleUtil
    {
        static int maxJetCounter = 10;

        int jetRemain;
        int inJetCounter;

        public Action IntoJetAction, ExitJetAction;

        public override void Update(Player player)
        {
            base.Update(player);

            if (!PlayerPatchs.TryGetModule<DMPSModule>(player, out var m))
                return;

            if(inJetCounter == 0)//into jet mode
            {
                if (jetRemain > 0)
                {
                    if (player.input[0].jmp && !player.input[1].jmp && 
                        player.canJump <= 0 && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam &&
                        m.bioReactor.TrySpendEnergy(3f))
                    {
                        inJetCounter = maxJetCounter;
                        IntoJetAction?.Invoke();
                        jetRemain--;
                    }
                }
                else
                {
                    if (player.bodyChunks[0].contactPoint.y < 0 || player.bodyChunks[1].contactPoint.y < 0)
                        jetRemain++;
                }
            }
            else//jet mode
            {
                if(inJetCounter == maxJetCounter)
                {
                    foreach (var chunk in player.bodyChunks)
                    {
                        if (chunk.vel.y < 0)
                            chunk.vel += Vector2.up * (-chunk.vel.y + 5f);
                        else
                            chunk.vel += Vector2.up * 8f;
                    }
                }
                else
                {
                    foreach (var chunk in player.bodyChunks)
                        chunk.vel += Vector2.up * 1.5f;
                }
               
                if (!player.input[0].jmp)
                    inJetCounter = 0;
                if(inJetCounter > 0)
                    inJetCounter--;

                if (inJetCounter == 0)
                    ExitJetAction?.Invoke();

                if (player.bodyChunks[0].contactPoint.y != 0 || player.bodyChunks[1].contactPoint.y != 0)
                {
                    inJetCounter = 0;
                    ExitJetAction?.Invoke();
                    return;
                }



                if (player.animation != DMEnums.DMPS.PlayerAnimationIndex.DMFlip)
                {
                    player.animation = DMEnums.DMPS.PlayerAnimationIndex.DMFlip;
                }
            }
        }
    }
}
