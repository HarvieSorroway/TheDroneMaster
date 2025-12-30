using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TheDroneMaster.DMPS.SkillTree
{
    internal static class SkillTreeNodeJsonHelper
    {
        // 序列化配置：枚举序列化为字符串，支持Unity Vector2，处理空值
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            // 枚举序列化为字符串（而非数字），提高可读性
            Converters = { new StringEnumConverter() },
            // 处理Unity Vector2类型（默认会序列化x和y属性）
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore, // 忽略空值字段
            Formatting = Newtonsoft.Json.Formatting.Indented // 格式化输出（带缩进）
        };

        /// <summary>
        /// 将SkillTreeNode序列化为JSON字符串
        /// </summary>
        /// <param name="node">要序列化的技能树节点</param>
        /// <returns>JSON字符串</returns>
        public static string Serialize(SkillTreeNode node)
        {
            try
            {
                return JsonConvert.SerializeObject(node, _serializerSettings);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("SkillTreeNode序列化失败", ex);
            }
        }

        /// <summary>
        /// 将JSON字符串反序列化为SkillTreeNode
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>反序列化后的技能树节点</returns>
        public static SkillTreeNode Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
               Plugin.Log("SkillTreeNode JSON字符串不能为空");
            try
            {
                return JsonConvert.DeserializeObject<SkillTreeNode>(json, _serializerSettings);
            }
            catch (Exception ex)
            {
                Plugin.Log($"SkillTreeNode反序列化失败:{ex}");
            }
            return default;
        }
    }
}
