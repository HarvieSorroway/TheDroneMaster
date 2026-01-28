using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.DMPSSkillTree
{
    internal static class RenderNodeLoader
    {
        public static List<SkillTreeRenderNode> renderNodes = new List<SkillTreeRenderNode>();
        public static Dictionary<string, SkillTreeRenderNode> idMapper = new Dictionary<string, SkillTreeRenderNode>();

        public static void Load()
        {
            renderNodes.Clear();
            idMapper.Clear();

            List<string> indexPathLst = new List<string>
            {
                AssetManager.ResolveFilePath("skilltree/index.json"),
                AssetManager.ResolveFilePath("skilltree/index_droneupg.json"),
                AssetManager.ResolveFilePath("skilltree/index_droneportupg.json"),
                AssetManager.ResolveFilePath("skilltree/index_reactorupg.json"),
                AssetManager.ResolveFilePath("skilltree/index_torsoupg.json"),
            };


            foreach(var indexPath in indexPathLst)
            {
                if (!File.Exists(indexPath))
                    Plugin.Log($"SkillTree index 文件不存在: {indexPath}");


                string str = string.Empty;
                try
                {
                    str = File.ReadAllText(indexPath);
                }
                catch (Exception ex)
                {
                    Plugin.Log($"读取 SkillTree index 文件失败: {ex}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(str))
                {
                    Plugin.Log("SkillTree index 文件为空或仅包含空白字符");
                    continue;
                }

                try
                {
                    var nodes = JsonConvert.DeserializeObject<List<SkillTreeRenderNode>>(str);
                    if (nodes == null)
                    {
                        Plugin.Log("SkillTree index JSON 反序列化结果为 null");
                        continue;
                    }

                    Plugin.Log($"已加载 SkillTreeRenderNode 数量: {nodes.Count}");
                    renderNodes.AddRange(nodes);
                }
                catch (Exception ex)
                {
                    Plugin.Log($"SkillTree index JSON 解析失败: {ex}");
                }
            }

            foreach(var node in renderNodes)
                idMapper.Add(node.renderNodeIDInfo, node);
        }
    }
}
