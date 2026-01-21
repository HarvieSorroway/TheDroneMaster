using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.DMPSSkillTree
{
    internal class SkillNodeLoader
    {
        public static Dictionary<string, SkillNode> loadedSkillNodes = new Dictionary<string, SkillNode>();

        public static void Load()
        {
            string basePath = AssetManager.ResolveDirectory("skilltree/skills");
            loadedSkillNodes.Clear();
            Plugin.LoggerLog($"start loading skill nodes : {basePath}");
            foreach (var file in Directory.EnumerateFiles(basePath, "*.json", SearchOption.AllDirectories))
            {
                Plugin.LoggerLog($"Search path for skillnode : {file}");
                if (DMPSSkillTreeHelper.TryDeserializeSkillNode(File.ReadAllText(file), out var node))
                {
                    Plugin.LoggerLog($"Load skill of id : {node.skillID}");
                    loadedSkillNodes.Add(node.skillID, node);
                }
            }
   
        }

        static void _LoadRecusive(string path)
        {
            foreach(var file in Directory.EnumerateFiles(path))
            {
                if(DMPSSkillTreeHelper.TryDeserializeSkillNode(File.ReadAllText(path), out var node))
                {
                    loadedSkillNodes.Add(node.skillID, node);
                }
            }

            foreach(var dir in Directory.EnumerateDirectories(path))
            {
                _LoadRecusive(dir);
            }
        }
    }
}
