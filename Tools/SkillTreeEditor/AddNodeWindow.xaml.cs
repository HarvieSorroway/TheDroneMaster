using SkillTreeEditor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SkillTreeEditor;

public sealed class AddNodeRequest
{
    public string RenderNodeId { get; set; } = "";
    public string NodeType { get; set; } = "SubBasicNode";
    public string TargetIndexFile { get; set; } = "";
    public string? BindSkillId { get; set; }
    public string? IconSprite { get; set; }
    public float ScaleInfo { get; set; } = 0.8f;
    public bool AttachToSelectedParent { get; set; } = true;
    public bool ParentIsNodeGroup { get; set; }

    public bool CreateSkillNode { get; set; } = true;
    public string SkillFolder { get; set; } = "custom";
    public float Cost { get; set; } = 10f;
    public string? ChineseName { get; set; }
    public string? ChineseDescription { get; set; }
    public string? EnglishName { get; set; }
    public string? EnglishDescription { get; set; }
}

public partial class AddNodeWindow : Window
{
    public AddNodeRequest? Result { get; private set; }

    private readonly string? _parentRenderNodeId;
    private readonly string? _parentType;
    private bool _isInternalUpdate;

    private bool IsSelectedParentGroup =>
        _parentType?.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase) == true;
    private string? _lastAutoPrefix;
    private string? _previousNodeType;

    public AddNodeWindow(IEnumerable<string> indexFiles, string? suggestedIndexFile = null, string? parentRenderNodeId = null, string? parentType = null)
    {
        InitializeComponent();

        _parentRenderNodeId = string.IsNullOrWhiteSpace(parentRenderNodeId) ? null : parentRenderNodeId.Trim();
        _parentType = string.IsNullOrWhiteSpace(parentType) ? null : parentType.Trim();

        foreach (var file in indexFiles)
            IndexFileBox.Items.Add(file);

        if (!string.IsNullOrWhiteSpace(suggestedIndexFile))
            IndexFileBox.SelectedItem = suggestedIndexFile;

        if (IndexFileBox.SelectedIndex < 0 && IndexFileBox.Items.Count > 0)
            IndexFileBox.SelectedIndex = 0;

        ApplyTypeRules();
        ApplyAutoNodeIdPrefix(forceIfEmpty: true);
    }

    private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyTypeRules();

        var type = ((TypeBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SubBasicNode").Trim();
        var wasGroup = _previousNodeType?.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase) == true;
        var isGroup = type.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase);
        var crossedGroupBoundary = _previousNodeType != null && wasGroup != isGroup;

        // 普通节点类型之间切换时保留用户输入；只有进入或离开 Group 时重建默认 ID。
        if (_previousNodeType == null)
            ApplyAutoNodeIdPrefix(forceIfEmpty: true);
        else if (crossedGroupBoundary)
            ApplyAutoNodeIdPrefix(forceIfEmpty: true, forceReplace: true);

        _previousNodeType = type;
    }

    private void RenderNodeIdBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInternalUpdate) return;
        ApplyAutoSkillIdIfNeeded();
    }

    private void ApplyTypeRules()
    {
        // XAML 初始化期间，TypeBox 的 SelectionChanged 可能早于后续输入框创建完成触发。
        // 这时直接访问 BindSkillIdBox / IconSpriteBox 等会导致打开窗口即闪退。
        if (BindSkillIdBox == null || IconSpriteBox == null || ScaleBox == null ||
            CreateSkillBox == null || SkillFolderBox == null || CostBox == null ||
            ChineseNameBox == null || ChineseDescBox == null || EnglishNameBox == null || EnglishDescBox == null)
        {
            return;
        }

        var type = ((TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SubBasicNode").Trim();

        // 默认全部可编辑
        SetSkillFieldsEnabled(true);
        BindSkillIdBox.IsEnabled = true;
        IconSpriteBox.IsEnabled = true;
        ScaleBox.IsEnabled = true;
        AttachToSelectedParentBox.IsEnabled = true;

        // Group：纯容器，不需要 skill、icon、scale
        if (type.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase))
        {
            BindSkillIdBox.IsEnabled = false;
            BindSkillIdBox.Text = "";
            IconSpriteBox.IsEnabled = false;
            IconSpriteBox.Text = "";
            ScaleBox.IsEnabled = false;
            ScaleBox.Text = "1";
            SetSkillFieldsEnabled(false);
            CreateSkillBox.IsChecked = false;
        }
        // Line：只画线，不需要 skill、icon、scale
        else if (type.Equals("LineNode", StringComparison.OrdinalIgnoreCase))
        {
            BindSkillIdBox.IsEnabled = false;
            BindSkillIdBox.Text = "";
            IconSpriteBox.IsEnabled = false;
            IconSpriteBox.Text = "";
            ScaleBox.IsEnabled = false;
            ScaleBox.Text = "1";
            SetSkillFieldsEnabled(false);
            CreateSkillBox.IsChecked = false;

            // Group 下创建的是分类展开线；其他位置创建的是总览线。
            AttachToSelectedParentBox.IsEnabled = IsSelectedParentGroup;
            AttachToSelectedParentBox.IsChecked = IsSelectedParentGroup;
        }
        // SubBasicNode：自动生成 SkillID，并默认勾选创建 Skill；SkillID 不允许手动填写
        else if (type.Equals("SubBasicNode", StringComparison.OrdinalIgnoreCase))
        {
            BindSkillIdBox.IsEnabled = false;
            CreateSkillBox.IsChecked = true;
            ApplyAutoSkillIdIfNeeded();
        }
    }

    private void ApplyAutoNodeIdPrefix(bool forceIfEmpty, bool forceReplace = false)
    {
        if (RenderNodeIdBox == null)
            return;

        if (string.IsNullOrWhiteSpace(_parentRenderNodeId))
            return;

        var type = ((TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SubBasicNode").Trim();
        var current = RenderNodeIdBox.Text;

        // 切换到 NodeGroup 时按父节点重新生成完整的 Group ID。
        // 其他类型只在输入框为空，或仍为编辑器自动填入的内容时覆盖。
        var allowOverwrite = forceReplace || (forceIfEmpty && string.IsNullOrWhiteSpace(current));
        if (!allowOverwrite && !string.IsNullOrWhiteSpace(_lastAutoPrefix) && current.StartsWith(_lastAutoPrefix, StringComparison.OrdinalIgnoreCase))
            allowOverwrite = true;

        if (!allowOverwrite) return;

        var prefix = SuggestRenderNodeIdPrefix(_parentRenderNodeId!, _parentType, type);
        if (string.IsNullOrWhiteSpace(prefix)) return;

        _isInternalUpdate = true;
        try
        {
            RenderNodeIdBox.Text = prefix;
            RenderNodeIdBox.CaretIndex = RenderNodeIdBox.Text.Length;
            _lastAutoPrefix = prefix;
        }
        finally
        {
            _isInternalUpdate = false;
        }

        ApplyAutoSkillIdIfNeeded();
    }

    private static string SuggestRenderNodeIdPrefix(string parentId, string? parentType, string newType)
    {
        var p = parentId.Trim();

        // 编辑器虚拟根：普通节点使用 RenderNode.，Group 使用 RenderNode.Group.。
        if (parentType?.Equals("VirtualRoot", StringComparison.OrdinalIgnoreCase) == true)
        {
            return newType.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase)
                ? "RenderNode.Group."
                : "RenderNode.";
        }

        // 如果父节点是 Group，则孩子通常不以 RenderNode.Group.* 开头，而是回到 RenderNode.<Category>.*
        if (p.StartsWith("RenderNode.Group.", StringComparison.OrdinalIgnoreCase))
        {
            var rest = p["RenderNode.Group.".Length..];
            if (newType.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase))
            {
                // Group 下再建 Group：给一个明显的子组前缀
                return $"RenderNode.Group.{rest}.";
            }
            return $"RenderNode.{rest}.";
        }

        // 切换为 Group 类型时，建议生成 RenderNode.Group.<parentRest>
        if (newType.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase))
        {
            if (p.StartsWith("RenderNode.", StringComparison.OrdinalIgnoreCase))
            {
                var rest = p["RenderNode.".Length..];
                return $"RenderNode.Group.{rest}";
            }
            return "RenderNode.Group.";
        }

        // 普通情况：在父节点后面追加点号
        return p.EndsWith(".", StringComparison.Ordinal) ? p : p + ".";
    }

    private void ApplyAutoSkillIdIfNeeded()
    {
        if (RenderNodeIdBox == null || BindSkillIdBox == null)
            return;

        var type = ((TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SubBasicNode").Trim();
        if (!type.Equals("SubBasicNode", StringComparison.OrdinalIgnoreCase))
            return;

        var skillId = GenerateSkillIdFromRenderNodeId(RenderNodeIdBox.Text);
        _isInternalUpdate = true;
        try
        {
            BindSkillIdBox.Text = skillId ?? "";
        }
        finally
        {
            _isInternalUpdate = false;
        }
    }

    private static string? GenerateSkillIdFromRenderNodeId(string renderNodeId)
    {
        var id = (renderNodeId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(id)) return null;

        // RenderNode.X.Y -> Skill.X.Y
        if (id.StartsWith("RenderNode.", StringComparison.OrdinalIgnoreCase))
            return "Skill." + id["RenderNode.".Length..];

        // 已经是 Skill 前缀则原样返回
        if (id.StartsWith("Skill.", StringComparison.OrdinalIgnoreCase))
            return id;

        // fallback：用户没写 RenderNode 前缀
        return "Skill." + id.TrimStart('.');
    }

    private void SetSkillFieldsEnabled(bool enabled)
    {
        if (CreateSkillBox == null || SkillFolderBox == null || CostBox == null ||
            ChineseNameBox == null || ChineseDescBox == null || EnglishNameBox == null || EnglishDescBox == null)
        {
            return;
        }

        CreateSkillBox.IsEnabled = enabled;
        SkillFolderBox.IsEnabled = enabled;
        CostBox.IsEnabled = enabled;
        ChineseNameBox.IsEnabled = enabled;
        ChineseDescBox.IsEnabled = enabled;
        EnglishNameBox.IsEnabled = enabled;
        EnglishDescBox.IsEnabled = enabled;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var renderNodeId = RenderNodeIdBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(renderNodeId))
        {
            MessageBox.Show(this, "请填写 RenderNode ID。", "新增节点", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var type = ((TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SubBasicNode").Trim();
        var indexFile = IndexFileBox.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(indexFile))
        {
            MessageBox.Show(this, "请选择要写入的 index 文件。", "新增节点", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!float.TryParse(ScaleBox.Text.Trim(), out var scale))
            scale = 0.8f;

        if (!float.TryParse(CostBox.Text.Trim(), out var cost))
            cost = 10f;

        // 某些类型不应创建/绑定 skill
        var createSkill = CreateSkillBox.IsChecked == true;
        if (type.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase) || type.Equals("LineNode", StringComparison.OrdinalIgnoreCase))
            createSkill = false;
        if (type.Equals("SubBasicNode", StringComparison.OrdinalIgnoreCase))
            createSkill = CreateSkillBox.IsChecked == true; // 默认已勾选，但仍允许用户取消

        // SubBasicNode：强制从 RenderNodeId 自动生成 SkillID（不允许手填）
        var bindSkillId = BindSkillIdBox.IsEnabled && !string.IsNullOrWhiteSpace(BindSkillIdBox.Text) ? BindSkillIdBox.Text.Trim() : null;
        if (type.Equals("SubBasicNode", StringComparison.OrdinalIgnoreCase))
            bindSkillId = GenerateSkillIdFromRenderNodeId(renderNodeId);

        Result = new AddNodeRequest
        {
            RenderNodeId = renderNodeId,
            NodeType = type,
            TargetIndexFile = indexFile,
            BindSkillId = bindSkillId,
            IconSprite = IconSpriteBox.IsEnabled && !string.IsNullOrWhiteSpace(IconSpriteBox.Text) ? IconSpriteBox.Text.Trim() : null,
            ScaleInfo = scale,
            AttachToSelectedParent = AttachToSelectedParentBox.IsChecked == true,
            ParentIsNodeGroup = IsSelectedParentGroup,
            CreateSkillNode = createSkill,
            SkillFolder = string.IsNullOrWhiteSpace(SkillFolderBox.Text) ? "custom" : SkillFolderBox.Text.Trim(),
            Cost = cost,
            ChineseName = string.IsNullOrWhiteSpace(ChineseNameBox.Text) ? null : ChineseNameBox.Text.Trim(),
            ChineseDescription = string.IsNullOrWhiteSpace(ChineseDescBox.Text) ? null : ChineseDescBox.Text.Trim(),
            EnglishName = string.IsNullOrWhiteSpace(EnglishNameBox.Text) ? null : EnglishNameBox.Text.Trim(),
            EnglishDescription = string.IsNullOrWhiteSpace(EnglishDescBox.Text) ? null : EnglishDescBox.Text.Trim(),
        };

        DialogResult = true;
        Close();
    }
}
