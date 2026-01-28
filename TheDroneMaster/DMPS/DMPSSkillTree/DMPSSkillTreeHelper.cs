using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSave;
using TheDroneMaster.DMPS.SkillTree;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSkillTree
{
    internal static class DMPSSkillTreeHelper
    {// 序列化配置：枚举序列化为字符串，支持Unity Vector2，处理空值
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            // 枚举序列化为字符串（而非数字），提高可读性
            Converters = { new StringEnumConverter(),new UnityVector2Converter(), new InGameTranslatorLangIDConverter() },
            // 处理Unity Vector2类型（默认会序列化x和y属性）
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore, // 忽略空值字段
            Formatting = Newtonsoft.Json.Formatting.Indented // 格式化输出（带缩进）
        };

        public static string SerializeRenderNodes(List<SkillTreeRenderNode> nodes)
        {
            if (nodes == null)
            {
                Plugin.Log("Serialize: nodes 参数为 null，返回空数组 JSON");
                return "[]";
            }

            try
            {
                return JsonConvert.SerializeObject(nodes, _serializerSettings);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("SkillTreeRenderNode 列表序列化失败", ex);
            }
        }

        public static List<SkillTreeRenderNode> DeserializeRenderNodes(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Plugin.Log("Deserialize: SkillTreeRenderNode JSON 字符串为空或仅包含空白字符，返回空列表");
                return new List<SkillTreeRenderNode>();
            }

            try
            {
                var nodes = JsonConvert.DeserializeObject<List<SkillTreeRenderNode>>(json, _serializerSettings);
                if (nodes == null)
                {
                    Plugin.Log("Deserialize: 反序列化结果为 null，返回空列表");
                    return new List<SkillTreeRenderNode>();
                }
                return nodes;
            }
            catch (Exception ex)
            {
                Plugin.Log($"SkillTreeRenderNode 反序列化失败: {ex}");
                return new List<SkillTreeRenderNode>();
            }
        }

        public static bool TryDeserializeSkillNode(string json, out SkillNode skillNode)
        {
            skillNode = default;
            if (string.IsNullOrWhiteSpace(json))
            {
                Plugin.LoggerLog("Deserialize: SkillNode JSON 字符串为空或仅包含空白字符，返回默认值");
                return false;
            }

            try
            {
                skillNode = JsonConvert.DeserializeObject<SkillNode>(json, _serializerSettings);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.LoggerLog($"SkillNode 反序列化失败: {ex}");
                return false;
            }
        }

        public static string SerializeSkillNode(SkillNode skillNode)
        {
            return JsonConvert.SerializeObject(skillNode, _serializerSettings);
        }

        public static bool CheckCondition(SkillNode.SKillNodeConditionInfo conditionInfo, DMPSBasicSave save)
        {
            bool res = false;
            if (conditionInfo.type == SkillNode.ConditionType.SkillNode)
            {
                res = save.CheckSkill(conditionInfo.info);
            }
            else if (conditionInfo.type == SkillNode.ConditionType.Item)
            {
                //todo
                res = false;
            }

            if (conditionInfo.boolType == SkillNode.ConditionBoolType.NotOr || conditionInfo.boolType == SkillNode.ConditionBoolType.NotAnd)
            {
                res = !res;
            }

            return res;
        }

        public static bool CheckAllConditions(SkillNode node, DMPSBasicSave save)
        {
            if(node.conditions == null || node.conditions.Count == 0)
                return true;

            bool res = CheckCondition(node.conditions[0], save);
            for(int i = 0;i < node.conditions.Count; i++)
            {
                if (node.conditions[i].boolType == SkillNode.ConditionBoolType.Or || node.conditions[i].boolType == SkillNode.ConditionBoolType.NotOr)
                {
                    res = res || CheckCondition(node.conditions[i], save);
                }
                else
                {
                    res = res && CheckCondition(node.conditions[i], save);
                }
            }
            return res;
        }
    }

    internal class UnityVector2Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // 支持 UnityEngine.Vector2 和 可空 Vector2
            return objectType == typeof(UnityEngine.Vector2) || objectType == typeof(UnityEngine.Vector2?);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var v = (UnityEngine.Vector2)value;

            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(v.x);
            writer.WritePropertyName("y");
            writer.WriteValue(v.y);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return default(UnityEngine.Vector2);
            }

            var jo = JObject.Load(reader);

            float x = 0f;
            float y = 0f;

            JToken token;
            if (jo.TryGetValue("x", StringComparison.OrdinalIgnoreCase, out token))
            {
                x = token.ToObject<float>();
            }
            if (jo.TryGetValue("y", StringComparison.OrdinalIgnoreCase, out token))
            {
                y = token.ToObject<float>();
            }

            return new UnityEngine.Vector2(x, y);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
    }

    internal class InGameTranslatorLangIDConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(InGameTranslator.LanguageID);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var token = JToken.Load(reader);
                // 如果是字符串，直接返回字符串；否则返回 token 的文本表示
                if (token.Type == JTokenType.String)
                    return new InGameTranslator.LanguageID(token.ToObject<string>());
                return new InGameTranslator.LanguageID(token.ToString());
            }
            catch
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }

            // 优先写出名称（例如 "Chinese"），保证可读性并与 StringEnumConverter 行为一致
            writer.WriteValue((value as InGameTranslator.LanguageID).value);
        }
    }

    //internal class SkillTreeRenderNodeConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return objectType == typeof(SkillTreeRenderNode);
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        if (reader.TokenType == JsonToken.Null)
    //        {
    //            return default(SkillTreeRenderNode);
    //        }

    //        var jo = JObject.Load(reader);

    //        SkillTreeRenderNode res = default(SkillTreeRenderNode);

    //        JToken token;
    //        if (jo.TryGetValue(nameof(SkillTreeRenderNode.renderNodeIDInfo), StringComparison.OrdinalIgnoreCase, out token))
    //            res.renderNodeIDInfo = token.ToObject<string>();

    //        if (jo.TryGetValue(nameof(SkillTreeRenderNode.bindSkillNodeInfo), StringComparison.OrdinalIgnoreCase, out token))
    //            res.bindSkillNodeInfo = token.ToObject<string>();

    //        if (jo.TryGetValue(nameof(SkillTreeRenderNode.iconSprite), StringComparison.OrdinalIgnoreCase, out token))
    //            res.iconSprite = token.ToObject<string>();

    //        if (jo.TryGetValue(nameof(SkillTreeRenderNode.scaleInfo), StringComparison.OrdinalIgnoreCase, out token))
    //            res.scaleInfo = token.ToObject<float>();

    //        if (jo.TryGetValue(nameof(SkillTreeRenderNode.typeInfo), StringComparison.OrdinalIgnoreCase, out token))
    //            res.typeInfo = token.ToObject<SkillTreeRenderType>();

    //        if (jo.TryGetValue(nameof(SkillTreeRenderNode.posInfo), StringComparison.OrdinalIgnoreCase, out token))
    //            res.posInfo = token.ToObject<Vector2[]>();

    //        if (jo.TryGetValue(nameof(SkillTreeRenderNode.subRenderNodeInfo), StringComparison.OrdinalIgnoreCase, out token))
    //            res.subRenderNodeInfo = token.ToObject<string[]>();


    //        return res;
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        if (value == null)
    //        {
    //            writer.WriteNull();
    //            return;
    //        }

    //        var v = (SkillTreeRenderNode)value;

    //        writer.WriteStartObject();
    //        writer.WritePropertyName(nameof(SkillTreeRenderNode.renderNodeIDInfo));
    //        writer.WriteValue(v.renderNodeIDInfo);

    //        writer.WritePropertyName(nameof(SkillTreeRenderNode.bindSkillNodeInfo));
    //        writer.WriteValue(v.bindSkillNodeInfo);

    //        writer.WritePropertyName(nameof(SkillTreeRenderNode.iconSprite));
    //        writer.WriteValue(v.iconSprite);

    //        writer.WritePropertyName(nameof(SkillTreeRenderNode.scaleInfo));
    //        writer.WriteValue(v.scaleInfo);

    //        writer.WritePropertyName(nameof(SkillTreeRenderNode.typeInfo));
    //        writer.WriteValue(v.typeInfo);

    //        writer.WritePropertyName(nameof(SkillTreeRenderNode.posInfo));
    //        writer.WriteValue(v.posInfo);

    //        writer.WritePropertyName(nameof(SkillTreeRenderNode.subRenderNodeInfo));
    //        writer.WriteValue(v.subRenderNodeInfo);

    //        writer.WriteEndObject();
    //    }
    //}
}
