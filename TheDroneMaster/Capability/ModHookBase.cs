using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using IL.MoreSlugcats;

namespace TheDroneMaster.Capability
{
    public class ModHookBase
    {
        static bool applied = false;
        public static List<ModHookBase> modHookBases = new List<ModHookBase>();

        public string _assembly;
        public string _namespace;
        public string _id;
        public ModHookBase(string _assembly,string _namespace,string _id)
        {
            this._assembly = _assembly;
            this._namespace = _namespace;
            this._id = _id;
        }

        public static void ApplyHooks()
        {
            if (applied)
                return;

            modHookBases.Add(new AimHelperHook());


            foreach(var modhook in modHookBases)
            {
                try
                {
                    bool hookOn = false;
                    foreach(var mod in ModManager.ActiveMods)
                    {
                        if(mod.id == modhook._id)
                        {
                            modhook.Apply();
                            hookOn = true;
                        }
                    }
                    if(!hookOn)
                        Plugin.Log($"{modhook._id} hook dont have active mod");
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }
            applied = true;
        }

        public virtual void Apply()
        {
        }

        public Type GetClass(string className)
        {
            return Type.GetType($"{_namespace}.{className},{_assembly}", true);
        }

        public T GetStaticFieldValue<T>(Type type,string memberName,BindingFlags bindingFlags)
        {
            return (T)type.GetField(memberName, bindingFlags).GetValue(null);
        }
    }

    public class AimHelperHook : ModHookBase
    {
        public AimHelperHook() : base("AimHelper", "AimHelper", "AwriLynn.AimHelper")
        {
        }

        public override void Apply()
        {
            base.Apply();

            var aimhelperClass = GetClass("AimHelper");
            var blockList = GetStaticFieldValue<List<CreatureTemplate.Type>>(aimhelperClass, "creatures_blocklisted", BindingFlags.Static | BindingFlags.Public);
            blockList.Add(LaserDroneCritob.LaserDrone);
        }
    }
}
