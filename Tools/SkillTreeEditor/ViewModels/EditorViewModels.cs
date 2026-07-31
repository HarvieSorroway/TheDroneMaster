using SkillTreeEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkillTreeEditor.ViewModels;

public enum LayoutViewMode
{
    Fold = 0,
    Expand = 1,
}

public sealed class RenderNodeVM : BindableBase
{
    public SkillTreeRenderNode Model { get; }

    // 将技能树 JSON 的“游戏坐标”（中心为 0,0，y 向上）映射到 WPF Canvas（左上角为 0,0，y 向下）
    // 先用固定中心点即可，后续可做成可配置/自动适配。
    public const double CanvasCenterX = 600;
    public const double CanvasCenterY = 400;

    private double _zoom = 1.0;
    private double _panX = 0.0;
    private double _panY = 0.0;

    public RenderNodeVM(SkillTreeRenderNode model)
    {
        Model = model;
        Model.extConditions ??= new List<SkillTreeRenderNodeExtCondition>();
        foreach (var condition in Model.extConditions)
        {
            var vm = new RenderExtConditionVM(condition);
            if (condition.type.Equals("Hide", StringComparison.OrdinalIgnoreCase))
                HideExtConditions.Add(vm);
            else if (condition.type.Equals("Show", StringComparison.OrdinalIgnoreCase))
                ShowExtConditions.Add(vm);
            else
                PreExtConditions.Add(vm);
        }
    }

    public ObservableCollection<RenderExtConditionVM> PreExtConditions { get; } = new();
    public ObservableCollection<RenderExtConditionVM> ShowExtConditions { get; } = new();
    public ObservableCollection<RenderExtConditionVM> HideExtConditions { get; } = new();

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    // “游戏坐标”（与 JSON 一致，y 向上）
    public float GameX
    {
        get => GetActivePos()?.X ?? 0f;
        set
        {
            var p = GetOrCreateActivePos();
            p.X = value;
            RaisePositionChanged();
        }
    }

    public float GameY
    {
        get => GetActivePos()?.Y ?? 0f;
        set
        {
            var p = GetOrCreateActivePos();
            p.Y = value;
            RaisePositionChanged();
        }
    }

    public double CanvasX => CanvasCenterX + _panX + GameX * _zoom;
    public double CanvasY => CanvasCenterY + _panY - GameY * _zoom;

    internal LayoutViewMode ViewMode { get; set; } = LayoutViewMode.Fold;

    public void ApplyViewMode(LayoutViewMode mode)
    {
        ViewMode = mode;
        RaisePositionChanged();
    }

    public void ApplyViewport(double zoom, double panX, double panY)
    {
        _zoom = zoom;
        _panX = panX;
        _panY = panY;
        RaisePositionChanged();
    }

    private Vec2? GetActivePos()
    {
        if (Model.posInfo == null || Model.posInfo.Count == 0) return null;
        var idx = (int)ViewMode;
        if (Model.posInfo.Count == 1) idx = 0;
        if (idx < 0 || idx >= Model.posInfo.Count) idx = 0;
        return Model.posInfo[idx];
    }

    private Vec2 GetOrCreateActivePos()
    {
        Model.posInfo ??= new List<Vec2>();
        if (Model.posInfo.Count == 0) Model.posInfo.Add(new Vec2(0, 0));
        if (Model.posInfo.Count == 1 && ViewMode == LayoutViewMode.Expand) Model.posInfo.Add(new Vec2(Model.posInfo[0].X, Model.posInfo[0].Y));
        var idx = (int)ViewMode;
        if (idx < 0 || idx >= Model.posInfo.Count) idx = 0;
        return Model.posInfo[idx];
    }

    private void RaisePositionChanged()
    {
        RaisePropertyChanged(nameof(GameX));
        RaisePropertyChanged(nameof(GameY));
        RaisePropertyChanged(nameof(CanvasX));
        RaisePropertyChanged(nameof(CanvasY));
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? PositionChanged;

    public string NodeId => Model.renderNodeIDInfo;
    public string? BindSkillId
    {
        get => Model.bindSkillNodeInfo;
        set
        {
            Model.bindSkillNodeInfo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            RaisePropertyChanged(nameof(BindSkillId));
        }
    }

    public string? IconSprite
    {
        get => Model.iconSprite;
        set
        {
            Model.iconSprite = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            RaisePropertyChanged(nameof(IconSprite));
        }
    }

    public float ScaleInfo
    {
        get => Model.scaleInfo;
        set
        {
            Model.scaleInfo = value;
            RaisePropertyChanged(nameof(ScaleInfo));
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public string NodeType => Model.typeInfo;
    public int Layer
    {
        get => Model.layer;
        set
        {
            Model.layer = value;
            RaisePropertyChanged(nameof(Layer));
        }
    }

    public string DisplayLabel
    {
        get
        {
            var id = Model.renderNodeIDInfo ?? "";
            if (id.StartsWith("RenderNode.", StringComparison.OrdinalIgnoreCase))
                id = id["RenderNode.".Length..];
            return id;
        }
    }

    private ImageSource? _iconImage;
    public ImageSource? IconImage
    {
        get => _iconImage;
        set => SetProperty(ref _iconImage, value);
    }
}

public sealed class LineVM : BindableBase
{
    public string Id { get; set; } = "";

    private PointCollection _points = new();
    public PointCollection Points
    {
        get => _points;
        set => SetProperty(ref _points, value);
    }

    public Brush Stroke { get; set; } = new SolidColorBrush(Color.FromRgb(120, 120, 120));
    public double Thickness { get; set; } = 2.0;
    public double Opacity { get; set; } = 0.7;
    public bool IsVisible { get; set; } = true;
}

public sealed class TreeNodeItemVM : BindableBase
{
    public string Header { get; set; } = "";
    public SkillTreeRenderNode? Model { get; set; }
    public RenderNodeVM? CanvasNode { get; set; }
    public string? MissingRenderNodeId { get; set; }
    public bool IsMissing => Model == null && !string.IsNullOrWhiteSpace(MissingRenderNodeId);
    public TreeNodeItemVM? Parent { get; set; }
    public ObservableCollection<TreeNodeItemVM> Children { get; } = new();

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (SetProperty(ref _isVisible, value))
                RaisePropertyChanged(nameof(VisibilityIcon));
        }
    }

    public string VisibilityIcon => IsVisible ? "◉" : "○";

    private bool _isExpanded = true;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public sealed class SkillConditionVM : BindableBase
{
    public SkillNodeConditionInfo Model { get; }
    public SkillConditionVM(SkillNodeConditionInfo model) { Model = model; }

    public string Type
    {
        get => Model.type;
        set { Model.type = value ?? "SkillNode"; RaisePropertyChanged(nameof(Type)); }
    }

    public string BoolType
    {
        get => Model.boolType;
        set { Model.boolType = value ?? "And"; RaisePropertyChanged(nameof(BoolType)); }
    }

    public string Info
    {
        get => Model.info;
        set { Model.info = value ?? ""; RaisePropertyChanged(nameof(Info)); }
    }
}

public sealed class RenderExtConditionVM : BindableBase
{
    public SkillTreeRenderNodeExtCondition Model { get; }
    public RenderExtConditionVM(SkillTreeRenderNodeExtCondition model) { Model = model; }

    public string Type
    {
        get => Model.type;
        set { Model.type = value ?? "Pre"; RaisePropertyChanged(nameof(Type)); }
    }

    public string BoolType
    {
        get => Model.boolType;
        set { Model.boolType = value ?? "And"; RaisePropertyChanged(nameof(BoolType)); }
    }

    public string ConditionType
    {
        get => Model.conditionType;
        set { Model.conditionType = value ?? "SkillNode"; RaisePropertyChanged(nameof(ConditionType)); }
    }

    public string Info
    {
        get => Model.info;
        set { Model.info = value ?? ""; RaisePropertyChanged(nameof(Info)); }
    }
}

public sealed class SkillNodeVM : BindableBase
{
    public SkillNode Model { get; }
    public SkillNodeVM(SkillNode model)
    {
        Model = model;
        Model.conditions ??= new List<SkillNodeConditionInfo>();
        foreach (var condition in Model.conditions)
            Conditions.Add(new SkillConditionVM(condition));
    }

    public ObservableCollection<SkillConditionVM> Conditions { get; } = new();
    public string SkillId => Model.skillID;

    public float Cost
    {
        get => Model.cost;
        set
        {
            Model.cost = value;
            RaisePropertyChanged(nameof(Cost));
        }
    }

    public string? ChineseName
    {
        get => GetLang("Chinese")?.name;
        set { EnsureLang("Chinese").name = value; OnTextChanged(); }
    }
    public string? ChineseDescription
    {
        get => GetLang("Chinese")?.description;
        set { EnsureLang("Chinese").description = value; OnTextChanged(); }
    }

    public string? EnglishName
    {
        get => GetLang("English")?.name;
        set { EnsureLang("English").name = value; OnTextChanged(); }
    }
    public string? EnglishDescription
    {
        get => GetLang("English")?.description;
        set { EnsureLang("English").description = value; OnTextChanged(); }
    }

    private SkillNodeDescriptionInfo? GetLang(string key)
    {
        if (Model.descriptionInfos == null) return null;
        return Model.descriptionInfos.TryGetValue(key, out var v) ? v : null;
    }

    private SkillNodeDescriptionInfo EnsureLang(string key)
    {
        Model.descriptionInfos ??= new Dictionary<string, SkillNodeDescriptionInfo>(StringComparer.OrdinalIgnoreCase);
        if (!Model.descriptionInfos.TryGetValue(key, out var v))
        {
            v = new SkillNodeDescriptionInfo();
            Model.descriptionInfos[key] = v;
        }
        return v;
    }

    private void OnTextChanged()
    {
        RaisePropertyChanged(nameof(ChineseName));
        RaisePropertyChanged(nameof(ChineseDescription));
        RaisePropertyChanged(nameof(EnglishName));
        RaisePropertyChanged(nameof(EnglishDescription));
    }
}

public sealed class MainViewModel : BindableBase
{
    public ObservableCollection<RenderNodeVM> Nodes { get; } = new();
    public ObservableCollection<LineVM> Lines { get; } = new();
    public ObservableCollection<TreeNodeItemVM> TreeNodes { get; } = new();
    public ObservableCollection<string> AvailableSkillIds { get; } = new();

    private SkillConditionVM? _selectedCondition;
    public SkillConditionVM? SelectedCondition
    {
        get => _selectedCondition;
        set => SetProperty(ref _selectedCondition, value);
    }

    private RenderExtConditionVM? _selectedPreExtCondition;
    public RenderExtConditionVM? SelectedPreExtCondition
    {
        get => _selectedPreExtCondition;
        set => SetProperty(ref _selectedPreExtCondition, value);
    }

    private RenderExtConditionVM? _selectedShowExtCondition;
    public RenderExtConditionVM? SelectedShowExtCondition
    {
        get => _selectedShowExtCondition;
        set => SetProperty(ref _selectedShowExtCondition, value);
    }

    private RenderExtConditionVM? _selectedHideExtCondition;
    public RenderExtConditionVM? SelectedHideExtCondition
    {
        get => _selectedHideExtCondition;
        set => SetProperty(ref _selectedHideExtCondition, value);
    }

    private RenderNodeVM? _selectedNode;
    public RenderNodeVM? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    private SkillNodeVM? _selectedSkill;
    public SkillNodeVM? SelectedSkill
    {
        get => _selectedSkill;
        set => SetProperty(ref _selectedSkill, value);
    }

    private string _status = "未加载 skilltree";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private LayoutViewMode _viewMode = LayoutViewMode.Fold;
    public LayoutViewMode ViewMode
    {
        get => _viewMode;
        set => SetProperty(ref _viewMode, value);
    }

    private double _zoom = 1.0;
    public double Zoom
    {
        get => _zoom;
        set
        {
            if (SetProperty(ref _zoom, value))
                RaisePropertyChanged(nameof(ZoomPercentText));
        }
    }

    public string ZoomPercentText => $"{Zoom * 100:0}%";

    private double _panX;
    public double PanX
    {
        get => _panX;
        set => SetProperty(ref _panX, value);
    }

    private double _panY;
    public double PanY
    {
        get => _panY;
        set => SetProperty(ref _panY, value);
    }

    private bool _snapToGrid = true;
    public bool SnapToGrid
    {
        get => _snapToGrid;
        set => SetProperty(ref _snapToGrid, value);
    }
}
