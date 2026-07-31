using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SkillTreeEditor.Models;

public sealed class Vec2
{
    [JsonProperty("x")]
    public float X { get; set; }

    [JsonProperty("y")]
    public float Y { get; set; }

    public Vec2() { }
    public Vec2(float x, float y) { X = x; Y = y; }

    public override string ToString() => $"{X:0.##}, {Y:0.##}";
}

/// <summary>
/// 对应 TheDroneMaster/mod/skilltree/index*.json 里的单条记录。
/// 这里不强绑定 enum，避免未来扩展/拼写变化导致解析失败。
/// </summary>
public sealed class SkillTreeRenderNode
{
    public string renderNodeIDInfo { get; set; } = "";

    public string? bindSkillNodeInfo { get; set; }

    public List<Vec2>? posInfo { get; set; }

    public int layer { get; set; }

    public float scaleInfo { get; set; } = 1f;

    public string typeInfo { get; set; } = ""; // StaticNode / BasicNode / SubBasicNode / NodeGroup / LineNode

    public string? iconSprite { get; set; }

    public List<string>? subRenderNodeInfo { get; set; }

    public List<SkillTreeRenderNodeExtCondition>? extConditions { get; set; }

    // editor only
    [JsonIgnore]
    public string? __sourceIndexFile { get; set; }
}

public sealed class SkillTreeRenderNodeExtCondition
{
    public string type { get; set; } = "";          // Pre / Show / Hide
    public string boolType { get; set; } = "";      // And / Or / NotAnd / NotOr
    public string conditionType { get; set; } = ""; // SkillNode / Item ...
    public string info { get; set; } = "";
}

public sealed class SkillNode
{
    public string skillID { get; set; } = "";
    public float cost { get; set; }

    public List<SkillNodeConditionInfo>? conditions { get; set; }

    public Dictionary<string, SkillNodeDescriptionInfo>? descriptionInfos { get; set; }

    // editor only
    [JsonIgnore]
    public string? __sourceSkillFile { get; set; }
}

public sealed class SkillNodeDescriptionInfo
{
    public string? name { get; set; }
    public string? description { get; set; }
}

public sealed class SkillNodeConditionInfo
{
    public string type { get; set; } = "";      // SkillNode / Item ...
    public string boolType { get; set; } = "";  // And / Or / NotAnd / NotOr
    public string info { get; set; } = "";      // skillID or itemID
}

public sealed class SkillTreeProject
{
    public string SkillTreeFolder { get; set; } = "";

    // index file -> nodes loaded from this file
    public Dictionary<string, List<SkillTreeRenderNode>> IndexFileToNodes { get; } = new(StringComparer.OrdinalIgnoreCase);

    // skillID -> skill node
    public Dictionary<string, SkillNode> SkillIdToNode { get; } = new(StringComparer.OrdinalIgnoreCase);
}
