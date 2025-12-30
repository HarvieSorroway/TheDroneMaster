using RWCustom;
using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.GameHooks;
using static TheDroneMaster.PlayerPatchs;
using CustomDreamTx;
using static CustomDreamTx.CustomDreamRx;

namespace TheDroneMaster.DreamComponent.DreamHook
{
    public class DroneMasterDream : CustomNormalDreamTx
    {
        public static readonly DreamsState.DreamID DroneMasterDream_0 = new DreamsState.DreamID("DroneMasterDream_0", true);
        public static readonly DreamsState.DreamID DroneMasterDream_1 = new DreamsState.DreamID("DroneMasterDream_1", true);
        public static readonly DreamsState.DreamID DroneMasterDream_2 = new DreamsState.DreamID("DroneMasterDream_2", true);
        public static readonly DreamsState.DreamID DroneMasterDream_3 = new DreamsState.DreamID("DroneMasterDream_3", true);
        public static readonly DreamsState.DreamID DroneMasterDream_4 = new DreamsState.DreamID("DroneMasterDream_4", true);

        public DroneMasterDream() : base(new SlugcatStats.Name(Plugin.DroneMasterName))
        {
        }

        public override void DecideDreamID(
            SaveState saveState,
            string currentRegion,
            string denPosition,
            ref int cyclesSinceLastDream,
            ref int cyclesSinceLastFamilyDream,
            ref int cyclesSinceLastGuideDream,
            ref int inGWOrSHCounter,
            ref DreamsState.DreamID upcomingDream,
            ref DreamsState.DreamID eventDream,
            ref bool everSleptInSB,
            ref bool everSleptInSB_S01,
            ref bool guideHasShownHimselfToPlayer,
            ref int guideThread,
            ref bool guideHasShownMoonThisRound,
            ref int familyThread)
        {
            if (dreamFinished) return;

            upcomingDream = null;
            cyclesSinceLastFamilyDream = 0;//屏蔽FamilyDream计数，防止被原本的方法干扰

            //Plugin.Log("DreamState : cycleSinceLastDream{0},FamilyThread{1}", cyclesSinceLastDream, familyThread);

            switch (familyThread)
            {
                case 0:
                    if(saveState.cycleNumber > 0 && cyclesSinceLastDream > 0)
                        upcomingDream = DroneMasterDream_0;
                    break;
                case 1:
                    if (cyclesSinceLastDream > 3)
                        upcomingDream = DroneMasterDream_1;
                    break;
                case 2:
                    if(cyclesSinceLastDream > 4)
                        upcomingDream =DroneMasterDream_2;
                    break;
                case 3:
                    if (cyclesSinceLastDream > 4)
                        upcomingDream = DroneMasterDream_3;
                    break;
                case 4:
                    if (cyclesSinceLastDream > 4)
                        upcomingDream = DroneMasterDream_4;
                    break;
            }
            if (upcomingDream != null)
            {
                familyThread++;
                cyclesSinceLastDream = 0;
            }
        }

        public override BuildDreamWorldParams GetBuildDreamWorldParams()
        {
            if (activateDreamID == DroneMasterDream_0 ||
               activateDreamID == DroneMasterDream_1 ||
               activateDreamID == DroneMasterDream_3)
            {
                return new BuildDreamWorldParams()
                {
                    firstRoom = "DMD_AI",
                    singleRoomWorld = false,

                    playAs = DMEnums.SlugStateName.DroneMaster
                };
            }
            else if (activateDreamID == DroneMasterDream_2)
            {
                return new BuildDreamWorldParams()
                {
                    firstRoom = "DMD_LAB01",
                    singleRoomWorld = false,

                    playAs = DMEnums.SlugStateName.DroneMaster,
                    overridePlayerPos = new IntVector2 { x = 7, y = 54 },
                };
            }
            else if (activateDreamID == DroneMasterDream_4)
            {
                return new BuildDreamWorldParams()
                {
                    firstRoom = "DMD_ROOF",
                    singleRoomWorld = false,

                    playAs = DMEnums.SlugStateName.DroneMaster,
                    overridePlayerPos = new IntVector2{ x = 707,y = 8},
                };
            }
            else
            {
                return null;
            }
        }
    }
}
