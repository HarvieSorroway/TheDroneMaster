using CustomSaveTx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.DMPSSave
{
    internal class SkillTreeSave : DeathPersistentSaveDataTx
    {
        public override string header => "DMPSSKILLTREESAVE";
        HashSet<string> enabledSkills = new HashSet<string>();
        public SkillTreeSave(SlugcatStats.Name name) : base(name)
        {
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            if (string.IsNullOrEmpty(data))
                return;

            enabledSkills = JsonConvert.DeserializeObject<HashSet<string>>(data);
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied | saveAsIfPlayerQuit) return origSaveData;
            if (enabledSkills.Count == 0) return "";

            return JsonConvert.SerializeObject(enabledSkills);
        }

        public bool CheckSkill(string id)
        {
            return enabledSkills.Contains(id);
        }
    }
}
