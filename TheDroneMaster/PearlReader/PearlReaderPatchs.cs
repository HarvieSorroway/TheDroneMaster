using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster
{
    public static class PearlReaderPatchs //try to learn something in PearlLexicon :P
    {
        public static bool SkipIntro = false;
        public static void Patch()
        {
            Hook rainWorldGame_get_GetStoryGameSession_Hook = new Hook(typeof(RainWorldGame).GetProperty("GetStorySession", propFlags).GetGetMethod(), typeof(PearlReaderPatchs).GetMethod("RainWorldGame_get_GetStorySession", methodFlags));
            On.SLOracleBehaviorHasMark.MoonConversation.PearlIntro += MoonConversation_PearlIntro;
        }

        private static void MoonConversation_PearlIntro(On.SLOracleBehaviorHasMark.MoonConversation.orig_PearlIntro orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            if (SkipIntro) return;
            orig.Invoke(self);
        }

        //to fix exception in MoonConversation.ctor
        public static StoryGameSession RainWorldGame_get_GetStorySession(orig_RainWorldGame_GetStorySession orig, RainWorldGame self)
        {
            StoryGameSession result = orig.Invoke(self);
            if (self.session == null || !(self.session is StoryGameSession))
            {
                result = PortPearlReader.GetUninit<StoryGameSession>();
            }
            return result;
        }
        public delegate StoryGameSession orig_RainWorldGame_GetStorySession(RainWorldGame self);
        static BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
        static BindingFlags methodFlags = BindingFlags.Static | BindingFlags.Public;

    }
}
