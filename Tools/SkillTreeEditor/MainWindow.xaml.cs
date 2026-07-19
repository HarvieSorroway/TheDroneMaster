using Microsoft.Win32;
using Newtonsoft.Json;
using SkillTreeEditor.Models;
using SkillTreeEditor.Services;
using SkillTreeEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ellipse = System.Windows.Shapes.Ellipse;

namespace SkillTreeEditor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    private SkillTreeProject? _project;
    private string? _illustrationsFolder;

    private readonly Dictionary<string, ImageSource> _iconCache = new(StringComparer.OrdinalIgnoreCase);

    private RenderNodeVM? _dragNode;
    private Point _dragOffset;
    private TreeNodeItemVM? _selectedTreeItem;
    private bool _treeSelectionGuard;
    private readonly HashSet<string> _hiddenNodeIds = new(StringComparer.OrdinalIgnoreCase);

    private SkillTreeRenderNode? _selectedLineNode;
    private int _dragLinePointIndex = -1;

    private bool _isPanning;
    private Point _panStartMouse;
    private double _panStartX;
    private double _panStartY;

    private Point _treeDragStart;
    private TreeNodeItemVM? _treeDragSource;

    private RenderNodeVM? _undoMoveNode;
    private float _undoMoveX;
    private float _undoMoveY;
    private float _dragStartGameX;
    private float _dragStartGameY;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ViewMode))
            {
                foreach (var n in _vm.Nodes)
                    n.ApplyViewMode(_vm.ViewMode);

                // 切换布局时默认聚焦对应的 1366×768 游戏可见范围中心，并保留当前缩放。
                if (_vm.ViewMode == LayoutViewMode.Expand)
                {
                    _vm.PanX = -683.0 * _vm.Zoom;
                    _vm.PanY = 384.0 * _vm.Zoom;
                }
                else
                {
                    _vm.PanX = 0;
                    _vm.PanY = 0;
                }

                ApplyViewportToNodes();
                RebuildLines();
                RefreshLineEditor();
                RefreshSelectedNodeGameRange();
                RefreshAxes();
            }
            else if (e.PropertyName == nameof(MainViewModel.Zoom) ||
                     e.PropertyName == nameof(MainViewModel.PanX) ||
                     e.PropertyName == nameof(MainViewModel.PanY))
            {
                ApplyViewportToNodes();
                RebuildLines();
                RefreshLineEditor();
                RefreshSelectedNodeGameRange();
                RefreshAxes();
            }
        };

        RefreshAxes();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择 mod/skilltree 下任意一个 index*.json",
            Filter = "SkillTree index (*.json)|index*.json|JSON (*.json)|*.json",
            Multiselect = false,
        };

        if (dlg.ShowDialog(this) != true)
            return;

        var folder = Path.GetDirectoryName(dlg.FileName);
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            MessageBox.Show(this, "无法确定 skilltree 文件夹。", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _project = SkillTreeIO.LoadProject(folder);
            _illustrationsFolder = SkillTreeIO.TryFindIllustrationFolder(folder);
            _iconCache.Clear();

            RebuildViewModel();
            _vm.Status = $"已加载：{_project.SkillTreeFolder}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "加载失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_project == null)
        {
            MessageBox.Show(this, "尚未加载 skilltree。", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            SkillTreeIO.SaveProject(_project);
            _vm.Status = $"已保存：{_project.SkillTreeFolder}  ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RebuildViewModel()
    {
        _vm.Nodes.Clear();
        _vm.Lines.Clear();
        _vm.TreeNodes.Clear();
        _vm.AvailableSkillIds.Clear();
        _vm.SelectedCondition = null;
        _vm.SelectedPreExtCondition = null;
        _vm.SelectedShowExtCondition = null;
        _vm.SelectedHideExtCondition = null;
        _vm.SelectedNode = null;
        if (SelectedNodeGameRange != null)
            SelectedNodeGameRange.Visibility = Visibility.Collapsed;
        _vm.SelectedSkill = null;
        _selectedTreeItem = null;
        _selectedLineNode = null;
        ClearLineEditor();

        if (_project == null) return;

        foreach (var skillId in _project.SkillIdToNode.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            _vm.AvailableSkillIds.Add(skillId);

        var renderNodeMap = new Dictionary<string, RenderNodeVM>(StringComparer.OrdinalIgnoreCase);

        // build nodes
        foreach (var nodes in _project.IndexFileToNodes.Values)
        {
            foreach (var n in nodes)
            {
                if (n.typeInfo.Equals("NodeGroup", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (n.typeInfo.Equals("LineNode", StringComparison.OrdinalIgnoreCase))
                    continue;

                var vm = new RenderNodeVM(n)
                {
                    IsVisible = !_hiddenNodeIds.Contains(n.renderNodeIDInfo)
                };
                vm.ApplyViewMode(_vm.ViewMode);
                vm.ApplyViewport(_vm.Zoom, _vm.PanX, _vm.PanY);
                vm.IconImage = TryLoadIcon(vm.IconSprite);
                vm.PositionChanged += (_, _) =>
                {
                    RebuildLines();
                    if (ReferenceEquals(_vm.SelectedNode, vm))
                        RefreshSelectedNodeGameRange();
                };
                _vm.Nodes.Add(vm);
                renderNodeMap[n.renderNodeIDInfo] = vm;
            }
        }

        BuildTree(renderNodeMap);
        RebuildLines();
        RefreshAxes();
    }

    private void BuildTree(Dictionary<string, RenderNodeVM> renderNodeMap)
    {
        _vm.TreeNodes.Clear();
        if (_project == null) return;

        var allRenderNodes = _project.IndexFileToNodes.Values.SelectMany(x => x).ToList();
        var byId = allRenderNodes.ToDictionary(x => x.renderNodeIDInfo, StringComparer.OrdinalIgnoreCase);

        var childIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in allRenderNodes)
        {
            if (node.subRenderNodeInfo == null) continue;
            foreach (var childId in node.subRenderNodeInfo)
                childIds.Add(childId);
        }

        TreeNodeItemVM CreateTreeItem(SkillTreeRenderNode node)
        {
            var header = node.renderNodeIDInfo;
            if (!string.IsNullOrWhiteSpace(node.typeInfo))
                header += $"  [{node.typeInfo}]";

            var item = new TreeNodeItemVM
            {
                Header = header,
                Model = node,
                CanvasNode = renderNodeMap.TryGetValue(node.renderNodeIDInfo, out var vm) ? vm : null,
                IsVisible = !_hiddenNodeIds.Contains(node.renderNodeIDInfo)
            };

            if (node.subRenderNodeInfo != null)
            {
                foreach (var childId in node.subRenderNodeInfo)
                {
                    if (byId.TryGetValue(childId, out var child))
                    {
                        var childItem = CreateTreeItem(child);
                        childItem.Parent = item;
                        item.Children.Add(childItem);
                    }
                    else
                    {
                        item.Children.Add(new TreeNodeItemVM
                        {
                            Header = childId + "  [missing]",
                            MissingRenderNodeId = childId,
                            Parent = item
                        });
                    }
                }
            }

            return item;
        }

        var roots = allRenderNodes.Where(n => !childIds.Contains(n.renderNodeIDInfo)).ToList();

        // 编辑器专用虚拟根节点：只用于统一展示结构，不对应任何 SkillTreeRenderNode，保存时不会写入 JSON。
        var virtualRoot = new TreeNodeItemVM
        {
            Header = "RenderNode",
            Model = null,
            CanvasNode = null,
            IsExpanded = true,
            IsVisible = true
        };
        foreach (var root in roots.OrderBy(r => r.layer).ThenBy(r => r.renderNodeIDInfo))
        {
            var rootItem = CreateTreeItem(root);
            rootItem.Parent = virtualRoot;
            virtualRoot.Children.Add(rootItem);
        }
        _vm.TreeNodes.Add(virtualRoot);
    }

    private void RebuildLines()
    {
        _vm.Lines.Clear();
        if (_project == null) return;

        foreach (var nodes in _project.IndexFileToNodes.Values)
        {
            foreach (var n in nodes)
            {
                if (!n.typeInfo.Equals("LineNode", StringComparison.OrdinalIgnoreCase))
                    continue;

                var pts = new PointCollection();
                if (n.posInfo != null)
                {
                    foreach (var p in n.posInfo)
                    {
                        // line 目前在 json 里就是一串点，不分 fold/expand；直接按同一套坐标系画
                        var x = RenderNodeVM.CanvasCenterX + _vm.PanX + p.X * _vm.Zoom;
                        var y = RenderNodeVM.CanvasCenterY + _vm.PanY - p.Y * _vm.Zoom;
                        pts.Add(new Point(x, y));
                    }
                }

                _vm.Lines.Add(new LineVM
                {
                    Id = n.renderNodeIDInfo,
                    Points = pts,
                    Stroke = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                    Thickness = 2,
                    Opacity = 0.75,
                    IsVisible = !_hiddenNodeIds.Contains(n.renderNodeIDInfo)
                });
            }
        }
    }

    private void RefreshLineEditor()
    {
        ClearLineEditor();
        if (_selectedLineNode?.posInfo == null ||
            _hiddenNodeIds.Contains(_selectedLineNode.renderNodeIDInfo))
            return;

        var points = new PointCollection(_selectedLineNode.posInfo.Select(GameToCanvas));
        SelectedLineGlow.Points = points;
        SelectedLineHighlight.Points = points;
        SelectedLineGlow.Visibility = Visibility.Visible;
        SelectedLineHighlight.Visibility = Visibility.Visible;
        LineEditCanvas.Background = Brushes.Transparent;

        for (var i = 0; i < points.Count; i++)
        {
            var isStart = i == 0;
            var isEnd = i == points.Count - 1;
            var isEndpoint = isStart || isEnd;
            var size = isEndpoint ? 18d : 14d;
            var fill = isStart
                ? Color.FromRgb(48, 200, 104)
                : isEnd
                    ? Color.FromRgb(244, 78, 78)
                    : Color.FromRgb(255, 166, 48);
            var roleName = isStart ? "起点" : isEnd ? "终点" : $"折点 {i}";

            var handle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(fill),
                Stroke = Brushes.White,
                StrokeThickness = isEndpoint ? 3 : 2,
                Cursor = Cursors.SizeAll,
                Tag = i,
                ToolTip = $"{roleName}：拖动修改，右键删除"
            };
            Canvas.SetLeft(handle, points[i].X - size / 2);
            Canvas.SetTop(handle, points[i].Y - size / 2);
            handle.MouseLeftButtonDown += LinePoint_MouseLeftButtonDown;
            handle.MouseLeftButtonUp += LinePoint_MouseLeftButtonUp;
            handle.MouseMove += LinePoint_MouseMove;
            handle.MouseRightButtonDown += LinePoint_MouseRightButtonDown;
            LineEditCanvas.Children.Add(handle);

            if (isEndpoint)
            {
                var endpointLabel = new TextBlock
                {
                    Text = isStart ? "S" : "E",
                    Foreground = Brushes.White,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    IsHitTestVisible = false,
                    Tag = "LineEndpointLabel"
                };
                Canvas.SetLeft(endpointLabel, points[i].X - 3.5);
                Canvas.SetTop(endpointLabel, points[i].Y - 6.5);
                LineEditCanvas.Children.Add(endpointLabel);
            }
        }

        LineEditCanvas.MouseLeftButtonDown += LineEditCanvas_MouseLeftButtonDown;
    }

    private void ClearLineEditor()
    {
        if (LineEditCanvas == null) return;
        LineEditCanvas.MouseLeftButtonDown -= LineEditCanvas_MouseLeftButtonDown;
        for (var i = LineEditCanvas.Children.Count - 1; i >= 0; i--)
        {
            if (LineEditCanvas.Children[i] is Ellipse ||
                LineEditCanvas.Children[i] is TextBlock { Tag: "LineEndpointLabel" })
                LineEditCanvas.Children.RemoveAt(i);
        }
        if (SelectedLineGlow != null) SelectedLineGlow.Visibility = Visibility.Collapsed;
        if (SelectedLineHighlight != null) SelectedLineHighlight.Visibility = Visibility.Collapsed;
        LineEditCanvas.Background = null;
        _dragLinePointIndex = -1;
    }

    private Point GameToCanvas(Vec2 p) => new(
        RenderNodeVM.CanvasCenterX + _vm.PanX + p.X * _vm.Zoom,
        RenderNodeVM.CanvasCenterY + _vm.PanY - p.Y * _vm.Zoom);

    private Vec2 CanvasToGame(Point p) => new(
        (float)((p.X - RenderNodeVM.CanvasCenterX - _vm.PanX) / Math.Max(0.05, _vm.Zoom)),
        (float)((RenderNodeVM.CanvasCenterY + _vm.PanY - p.Y) / Math.Max(0.05, _vm.Zoom)));

    private void LineEditCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_selectedLineNode == null || e.ClickCount < 2 || e.OriginalSource is Ellipse)
            return;

        _selectedLineNode.posInfo ??= new List<Vec2>();
        var raw = CanvasToGame(e.GetPosition(LineEditCanvas));
        var point = _selectedLineNode.posInfo.Count == 0
            ? SnapGrid(raw)
            : SnapEightDirections(_selectedLineNode.posInfo[^1], raw);
        _selectedLineNode.posInfo.Add(point);
        RebuildLines();
        RefreshLineEditor();
        _vm.Status = $"已向 {_selectedLineNode.renderNodeIDInfo} 追加折点（8方向吸附）";
        e.Handled = true;
    }

    private void LinePoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Ellipse { Tag: int index } handle) return;
        _dragLinePointIndex = index;
        handle.CaptureMouse();
        e.Handled = true;
    }

    private void LinePoint_MouseMove(object sender, MouseEventArgs e)
    {
        if (_selectedLineNode?.posInfo == null || _dragLinePointIndex < 0 || e.LeftButton != MouseButtonState.Pressed)
            return;

        var raw = CanvasToGame(e.GetPosition(LineEditCanvas));
        var snapped = _dragLinePointIndex > 0
            ? SnapEightDirections(_selectedLineNode.posInfo[_dragLinePointIndex - 1], raw)
            : SnapGrid(raw);
        _selectedLineNode.posInfo[_dragLinePointIndex].X = snapped.X;
        _selectedLineNode.posInfo[_dragLinePointIndex].Y = snapped.Y;
        UpdateSelectedLineGeometry();
        if (sender is Ellipse handle)
        {
            var canvasPoint = GameToCanvas(snapped);
            Canvas.SetLeft(handle, canvasPoint.X - handle.Width / 2);
            Canvas.SetTop(handle, canvasPoint.Y - handle.Height / 2);

            var isStart = _dragLinePointIndex == 0;
            var isEnd = _dragLinePointIndex == _selectedLineNode.posInfo.Count - 1;
            if (isStart || isEnd)
            {
                var marker = isStart ? "S" : "E";
                var label = LineEditCanvas.Children.OfType<TextBlock>()
                    .FirstOrDefault(x => Equals(x.Tag, "LineEndpointLabel") && x.Text == marker);
                if (label != null)
                {
                    Canvas.SetLeft(label, canvasPoint.X - 3.5);
                    Canvas.SetTop(label, canvasPoint.Y - 6.5);
                }
            }
        }
    }

    private void UpdateSelectedLineGeometry()
    {
        if (_selectedLineNode?.posInfo == null) return;
        var points = new PointCollection(_selectedLineNode.posInfo.Select(GameToCanvas));
        SelectedLineGlow.Points = points;
        SelectedLineHighlight.Points = points;

        var line = _vm.Lines.FirstOrDefault(x => string.Equals(x.Id, _selectedLineNode.renderNodeIDInfo, StringComparison.OrdinalIgnoreCase));
        if (line != null)
            line.Points = points;
    }

    private void LinePoint_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse handle) handle.ReleaseMouseCapture();
        _dragLinePointIndex = -1;
        e.Handled = true;
    }

    private void LinePoint_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_selectedLineNode?.posInfo == null || sender is not Ellipse { Tag: int index }) return;
        if (_selectedLineNode.posInfo.Count <= 2)
        {
            MessageBox.Show(this, "LineNode 至少需要保留两个点。", "删除折点", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _selectedLineNode.posInfo.RemoveAt(index);
        RebuildLines();
        RefreshLineEditor();
        e.Handled = true;
    }

    private Vec2 SnapGrid(Vec2 p)
    {
        if (!_vm.SnapToGrid) return p;
        return new Vec2((float)(Math.Round(p.X / 10f) * 10f), (float)(Math.Round(p.Y / 10f) * 10f));
    }

    private Vec2 SnapEightDirections(Vec2 origin, Vec2 target)
    {
        var dx = target.X - origin.X;
        var dy = target.Y - origin.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001) return new Vec2(origin.X, origin.Y);

        var angle = Math.Atan2(dy, dx);
        var snappedAngle = Math.Round(angle / (Math.PI / 4.0)) * (Math.PI / 4.0);
        var result = new Vec2(
            origin.X + (float)(Math.Cos(snappedAngle) * length),
            origin.Y + (float)(Math.Sin(snappedAngle) * length));
        return SnapGrid(result);
    }

    private void WorkspaceGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 右键空白保留给画布；折点删除由控制点自己的事件处理。
    }

    private ImageSource? TryLoadIcon(string? iconSprite)
    {
        if (string.IsNullOrWhiteSpace(iconSprite)) return null;
        if (string.IsNullOrWhiteSpace(_illustrationsFolder)) return null;

        if (_iconCache.TryGetValue(iconSprite, out var cached))
            return cached;

        var file = TryResolveIconFile(_illustrationsFolder!, iconSprite);
        if (file == null)
            return null;

        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(file, UriKind.Absolute);
            bi.EndInit();
            bi.Freeze();

            _iconCache[iconSprite] = bi;
            return bi;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryResolveIconFile(string illustrationsFolder, string iconSprite)
    {
        var s = iconSprite.Trim();
        if (s.StartsWith("RenderNode.", StringComparison.OrdinalIgnoreCase))
            s = s["RenderNode.".Length..];

        // RenderNode.DroneUpg.Dmg -> rendernode_droneupg_dmg.png
        var candidate = "rendernode_" + s.Replace('.', '_').ToLowerInvariant() + ".png";
        var path = Path.Combine(illustrationsFolder, candidate);
        if (File.Exists(path)) return path;

        // fallback: 直接当作文件名（极少数情况）
        var path2 = Path.Combine(illustrationsFolder, s.ToLowerInvariant() + ".png");
        if (File.Exists(path2)) return path2;

        return null;
    }

    private void ApplyViewportToNodes()
    {
        foreach (var n in _vm.Nodes)
            n.ApplyViewport(_vm.Zoom, _vm.PanX, _vm.PanY);
    }

    private void RefreshSelectedNodeGameRange()
    {
        if (SelectedNodeGameRange == null || _vm.SelectedNode == null || !_vm.SelectedNode.IsVisible)
        {
            if (SelectedNodeGameRange != null)
                SelectedNodeGameRange.Visibility = Visibility.Collapsed;
            return;
        }

        var node = _vm.SelectedNode;
        var supported = node.NodeType.Equals("StaticNode", StringComparison.OrdinalIgnoreCase) ||
                        node.NodeType.Equals("BasicNode", StringComparison.OrdinalIgnoreCase) ||
                        node.NodeType.Equals("SubBasicNode", StringComparison.OrdinalIgnoreCase);
        if (!supported)
        {
            SelectedNodeGameRange.Visibility = Visibility.Collapsed;
            return;
        }

        // 游戏四角装饰以 48 × scaleInfo 为半径，因此正常/展开状态使用 96 的完整对角线。
        // SubBasicNode 在 Fold 状态下背景缩至 18 像素方块，旋转后对角线为 18√2。
        var gameDiagonal = node.NodeType.Equals("SubBasicNode", StringComparison.OrdinalIgnoreCase) &&
                           _vm.ViewMode == LayoutViewMode.Fold
            ? 18.0 * Math.Sqrt(2.0) * Math.Max(0.01, node.ScaleInfo)
            : 96.0 * Math.Max(0.01, node.ScaleInfo);
        var radius = gameDiagonal * _vm.Zoom / 2.0;
        var cx = node.CanvasX;
        var cy = node.CanvasY;

        SelectedNodeGameRange.Points = new PointCollection
        {
            new(cx, cy - radius),
            new(cx + radius, cy),
            new(cx, cy + radius),
            new(cx - radius, cy)
        };
        SelectedNodeGameRange.Visibility = Visibility.Visible;
    }

    private void RefreshAxes()
    {
        if (CenterVerticalLine == null || CenterHorizontalLine == null)
            return;

        var centerX = RenderNodeVM.CanvasCenterX + _vm.PanX;
        var centerY = RenderNodeVM.CanvasCenterY + _vm.PanY;

        CenterVerticalLine.X1 = centerX;
        CenterVerticalLine.X2 = centerX;

        CenterHorizontalLine.Y1 = centerY;
        CenterHorizontalLine.Y2 = centerY;

        RefreshGameViewportFrame();
    }

    private void RefreshGameViewportFrame()
    {
        if (GameViewportFrame == null)
            return;

        const double gameWidth = 1366.0;
        const double gameHeight = 768.0;
        var left = _vm.ViewMode == LayoutViewMode.Fold ? -gameWidth / 2.0 : 0.0;
        var bottom = _vm.ViewMode == LayoutViewMode.Fold ? -gameHeight / 2.0 : 0.0;
        var topLeft = GameToCanvas(new Vec2((float)left, (float)(bottom + gameHeight)));

        Canvas.SetLeft(GameViewportFrame, topLeft.X);
        Canvas.SetTop(GameViewportFrame, topLeft.Y);
        GameViewportFrame.Width = gameWidth * _vm.Zoom;
        GameViewportFrame.Height = gameHeight * _vm.Zoom;
    }

    private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not RenderNodeVM node)
            return;

        SelectNode(node);

        _dragNode = node;
        _dragStartGameX = node.GameX;
        _dragStartGameY = node.GameY;
        _dragOffset = e.GetPosition(WorkspaceGrid);
        _dragOffset.X -= node.CanvasX;
        _dragOffset.Y -= node.CanvasY;

        element.CaptureMouse();
        e.Handled = true;
    }

    private void Node_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragNode == null) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var mouse = e.GetPosition(WorkspaceGrid);
        var newCanvasLeft = mouse.X - _dragOffset.X;
        var newCanvasTop = mouse.Y - _dragOffset.Y;

        var zoom = Math.Max(0.05, _vm.Zoom);
        var newGameX = (float)((newCanvasLeft - RenderNodeVM.CanvasCenterX - _vm.PanX) / zoom);
        var newGameY = (float)((RenderNodeVM.CanvasCenterY + _vm.PanY - newCanvasTop) / zoom);

        if (_vm.SnapToGrid)
        {
            newGameX = (float)(Math.Round(newGameX / 10.0) * 10.0);
            newGameY = (float)(Math.Round(newGameY / 10.0) * 10.0);
        }

        _dragNode.GameX = newGameX;
        _dragNode.GameY = newGameY;
    }

    private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element)
            element.ReleaseMouseCapture();

        if (_dragNode != null &&
            (Math.Abs(_dragNode.GameX - _dragStartGameX) > 0.001f || Math.Abs(_dragNode.GameY - _dragStartGameY) > 0.001f))
        {
            _undoMoveNode = _dragNode;
            _undoMoveX = _dragStartGameX;
            _undoMoveY = _dragStartGameY;
            _vm.Status = $"已移动 {_dragNode.NodeId}；可按 Ctrl+Z 撤销本次移动";
        }
        _dragNode = null;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Z || Keyboard.Modifiers != ModifierKeys.Control || _undoMoveNode == null)
            return;

        var node = _undoMoveNode;
        node.GameX = _undoMoveX;
        node.GameY = _undoMoveY;
        _undoMoveNode = null;
        _vm.Status = $"已撤销上一次坐标移动：{node.NodeId}";
        e.Handled = true;
    }

    private void ClearMoveUndo() => _undoMoveNode = null;

    private void WorkspaceGrid_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var oldZoom = _vm.Zoom;
        var newZoom = e.Delta > 0 ? oldZoom * 1.1 : oldZoom / 1.1;
        newZoom = Math.Clamp(newZoom, 0.25, 8.0);
        if (Math.Abs(newZoom - oldZoom) < 0.0001)
            return;

        var mouse = e.GetPosition(WorkspaceGrid);

        // 让鼠标指向的“游戏坐标”在缩放前后保持不变
        var gameX = (mouse.X - RenderNodeVM.CanvasCenterX - _vm.PanX) / oldZoom;
        var gameY = (RenderNodeVM.CanvasCenterY + _vm.PanY - mouse.Y) / oldZoom;

        _vm.Zoom = newZoom;
        _vm.PanX = mouse.X - RenderNodeVM.CanvasCenterX - gameX * newZoom;
        _vm.PanY = mouse.Y - RenderNodeVM.CanvasCenterY + gameY * newZoom;
        e.Handled = true;
    }

    private void WorkspaceGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (HasNodeAncestor(e.OriginalSource as DependencyObject))
            return;

        _isPanning = true;
        _panStartMouse = e.GetPosition(WorkspaceGrid);
        _panStartX = _vm.PanX;
        _panStartY = _vm.PanY;
        WorkspaceGrid.CaptureMouse();
        WorkspaceGrid.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void WorkspaceGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning || e.LeftButton != MouseButtonState.Pressed)
            return;

        var now = e.GetPosition(WorkspaceGrid);
        _vm.PanX = _panStartX + (now.X - _panStartMouse.X);
        _vm.PanY = _panStartY + (now.Y - _panStartMouse.Y);
    }

    private void WorkspaceGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning)
            return;

        _isPanning = false;
        WorkspaceGrid.ReleaseMouseCapture();
        WorkspaceGrid.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void ResetZoom_Click(object sender, RoutedEventArgs e)
    {
        _vm.Zoom = 1.0;
        _vm.PanX = 0.0;
        _vm.PanY = 0.0;
    }

    private static bool HasNodeAncestor(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is FrameworkElement element && element.Tag is RenderNodeVM)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private void SelectNode(RenderNodeVM node)
    {
        foreach (var n in _vm.Nodes)
            n.IsSelected = false;

        node.IsSelected = true;
        _vm.SelectedNode = node;
        _vm.SelectedPreExtCondition = null;
        _vm.SelectedShowExtCondition = null;
        _vm.SelectedHideExtCondition = null;
        RefreshSelectedNodeGameRange();

        if (_project != null && !string.IsNullOrWhiteSpace(node.BindSkillId) && _project.SkillIdToNode.TryGetValue(node.BindSkillId!, out var skill))
        {
            _vm.SelectedSkill = new SkillNodeVM(skill);
        }
        else
        {
            _vm.SelectedSkill = null;
        }

        SyncTreeSelection(node.Model.renderNodeIDInfo);
    }

    private void SyncTreeSelection(string renderNodeId)
    {
        if (_treeSelectionGuard) return;

        _treeSelectionGuard = true;
        try
        {
            foreach (var root in _vm.TreeNodes)
                SetTreeSelectionRecursive(root, renderNodeId);
        }
        finally
        {
            _treeSelectionGuard = false;
        }
    }

    private bool SetTreeSelectionRecursive(TreeNodeItemVM item, string renderNodeId)
    {
        var matched = item.Model != null && string.Equals(item.Model.renderNodeIDInfo, renderNodeId, StringComparison.OrdinalIgnoreCase);
        item.IsSelected = matched;

        var anyChildMatched = false;
        foreach (var child in item.Children)
        {
            if (SetTreeSelectionRecursive(child, renderNodeId))
            {
                anyChildMatched = true;
                item.IsExpanded = true;
            }
        }

        return matched || anyChildMatched;
    }

    private void NodeTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_treeSelectionGuard) return;
        if (e.NewValue is not TreeNodeItemVM item)
            return;

        _selectedTreeItem = item;
        _selectedLineNode = item.Model?.typeInfo?.Equals("LineNode", StringComparison.OrdinalIgnoreCase) == true
            ? item.Model
            : null;

        if (_selectedLineNode != null)
        {
            RefreshLineEditor();
            _vm.Status = $"Line 编辑模式：{_selectedLineNode.renderNodeIDInfo}（双击追加点，拖橙点移动，右键橙点删除）";
        }
        else
        {
            ClearLineEditor();
        }

        if (item.CanvasNode != null)
        {
            _treeSelectionGuard = true;
            try
            {
                SelectNode(item.CanvasNode);
            }
            finally
            {
                _treeSelectionGuard = false;
            }
        }
        else
        {
            // Tree 选中了非画布节点（例如 NodeGroup / LineNode）。
            // 仍然希望右侧 Inspector 能显示该节点的 typeInfo 等信息。
            foreach (var n in _vm.Nodes)
                n.IsSelected = false;

            _vm.SelectedNode = item.Model != null
                ? new RenderNodeVM(item.Model) { IconImage = TryLoadIcon(item.Model.iconSprite) }
                : null;
            _vm.SelectedPreExtCondition = null;
            _vm.SelectedShowExtCondition = null;
            _vm.SelectedHideExtCondition = null;

            if (_vm.SelectedNode != null)
            {
                _vm.SelectedNode.ApplyViewMode(_vm.ViewMode);
                _vm.SelectedNode.ApplyViewport(_vm.Zoom, _vm.PanX, _vm.PanY);
            }
            RefreshSelectedNodeGameRange();

            if (_project != null && item.Model != null && !string.IsNullOrWhiteSpace(item.Model.bindSkillNodeInfo) &&
                _project.SkillIdToNode.TryGetValue(item.Model.bindSkillNodeInfo!, out var skill))
            {
                _vm.SelectedSkill = new SkillNodeVM(skill);
            }
            else
            {
                _vm.SelectedSkill = null;
            }
        }
    }

    private void AddCondition_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedSkill == null)
        {
            MessageBox.Show(this, "请先选择一个绑定了 SkillNode 的技能节点。", "编辑 conditions", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var defaultInfo = _vm.AvailableSkillIds.FirstOrDefault(id =>
            !string.Equals(id, _vm.SelectedSkill.SkillId, StringComparison.OrdinalIgnoreCase)) ?? "";
        var model = new SkillNodeConditionInfo
        {
            type = "SkillNode",
            boolType = "And",
            info = defaultInfo
        };
        _vm.SelectedSkill.Model.conditions ??= new List<SkillNodeConditionInfo>();
        _vm.SelectedSkill.Model.conditions.Add(model);
        var condition = new SkillConditionVM(model);
        _vm.SelectedSkill.Conditions.Add(condition);
        _vm.SelectedCondition = condition;
        _vm.Status = $"已新增条件：{_vm.SelectedSkill.SkillId}";
    }

    private void AddRenderExtCondition_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedNode == null || !_vm.SelectedNode.NodeType.Equals("LineNode", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "请先选择一个 LineNode。", "编辑 extConditions", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var groupType = (sender as FrameworkElement)?.Tag?.ToString() ?? "Pre";
        var defaultInfo = _vm.AvailableSkillIds.FirstOrDefault(id =>
            !string.Equals(id, _vm.SelectedNode.BindSkillId, StringComparison.OrdinalIgnoreCase)) ?? "";
        var model = new SkillTreeRenderNodeExtCondition
        {
            type = groupType,
            boolType = "And",
            conditionType = "SkillNode",
            info = defaultInfo
        };
        _vm.SelectedNode.Model.extConditions ??= new List<SkillTreeRenderNodeExtCondition>();
        _vm.SelectedNode.Model.extConditions.Add(model);
        var condition = new RenderExtConditionVM(model);
        GetExtConditionGroup(groupType).Add(condition);
        SetSelectedExtCondition(groupType, condition);
        _vm.Status = $"已新增 {groupType} extCondition：{_vm.SelectedNode.NodeId}";
    }

    private void DeleteCondition_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedSkill == null || _vm.SelectedCondition == null)
        {
            MessageBox.Show(this, "请先在条件表中选择一条要删除的条件。", "编辑 conditions", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var condition = _vm.SelectedCondition;
        _vm.SelectedSkill.Conditions.Remove(condition);
        _vm.SelectedSkill.Model.conditions?.Remove(condition.Model);
        _vm.SelectedCondition = null;
        _vm.Status = $"已删除条件：{_vm.SelectedSkill.SkillId}";
    }

    private void DeleteRenderExtCondition_Click(object sender, RoutedEventArgs e)
    {
        var groupType = (sender as FrameworkElement)?.Tag?.ToString() ?? "Pre";
        var condition = GetSelectedExtCondition(groupType);
        if (_vm.SelectedNode == null || condition == null)
        {
            MessageBox.Show(this, $"请先在 {groupType} 表中选择一条要删除的条件。", "编辑 extConditions", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        GetExtConditionGroup(groupType).Remove(condition);
        _vm.SelectedNode.Model.extConditions?.Remove(condition.Model);
        SetSelectedExtCondition(groupType, null);
        _vm.Status = $"已删除 {groupType} extCondition：{_vm.SelectedNode.NodeId}";
    }

    private ObservableCollection<RenderExtConditionVM> GetExtConditionGroup(string groupType) => groupType switch
    {
        "Hide" => _vm.SelectedNode!.HideExtConditions,
        "Show" => _vm.SelectedNode!.ShowExtConditions,
        _ => _vm.SelectedNode!.PreExtConditions
    };

    private RenderExtConditionVM? GetSelectedExtCondition(string groupType) => groupType switch
    {
        "Hide" => _vm.SelectedHideExtCondition,
        "Show" => _vm.SelectedShowExtCondition,
        _ => _vm.SelectedPreExtCondition
    };

    private void SetSelectedExtCondition(string groupType, RenderExtConditionVM? condition)
    {
        if (groupType == "Hide") _vm.SelectedHideExtCondition = condition;
        else if (groupType == "Show") _vm.SelectedShowExtCondition = condition;
        else _vm.SelectedPreExtCondition = condition;
    }

    private void NodeTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _treeDragStart = e.GetPosition(NodeTreeView);
        _treeDragSource = FindTreeItem(e.OriginalSource as DependencyObject)?.DataContext as TreeNodeItemVM;
        if (_treeDragSource?.Model == null)
            _treeDragSource = null;
    }

    private void NodeTreeView_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _treeDragSource == null)
            return;

        var current = e.GetPosition(NodeTreeView);
        if (Math.Abs(current.X - _treeDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _treeDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        DragDrop.DoDragDrop(NodeTreeView, _treeDragSource, DragDropEffects.Move);
        _treeDragSource = null;
    }

    private void NodeTreeView_DragOver(object sender, DragEventArgs e)
    {
        var source = e.Data.GetData(typeof(TreeNodeItemVM)) as TreeNodeItemVM;
        var target = FindTreeItem(e.OriginalSource as DependencyObject)?.DataContext as TreeNodeItemVM;
        e.Effects = CanReparent(source, target) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void NodeTreeView_Drop(object sender, DragEventArgs e)
    {
        var source = e.Data.GetData(typeof(TreeNodeItemVM)) as TreeNodeItemVM;
        var target = FindTreeItem(e.OriginalSource as DependencyObject)?.DataContext as TreeNodeItemVM;
        if (!CanReparent(source, target) || source?.Model == null || target == null)
            return;

        ClearMoveUndo();
        var sourceId = source.Model.renderNodeIDInfo;
        RemoveChildReferenceFromAllNodes(sourceId);

        if (target.Model != null)
        {
            target.Model.subRenderNodeInfo ??= new List<string>();
            if (!target.Model.subRenderNodeInfo.Contains(sourceId, StringComparer.OrdinalIgnoreCase))
                target.Model.subRenderNodeInfo.Add(sourceId);
        }

        RebuildViewModel();
        _vm.Status = target.Model == null
            ? $"已将 {sourceId} 移到根级"
            : $"已将 {sourceId} 改为 {target.Model.renderNodeIDInfo} 的子节点";
        e.Handled = true;
    }

    private void RemoveChildReferenceFromAllNodes(string childId)
    {
        if (_project == null) return;
        foreach (var node in _project.IndexFileToNodes.Values.SelectMany(x => x))
            node.subRenderNodeInfo?.RemoveAll(id => string.Equals(id, childId, StringComparison.OrdinalIgnoreCase));
    }

    private bool CanReparent(TreeNodeItemVM? source, TreeNodeItemVM? target)
    {
        if (source?.Model == null || target == null || ReferenceEquals(source, target))
            return false;
        if (target.Model?.typeInfo?.Equals("LineNode", StringComparison.OrdinalIgnoreCase) == true)
            return false;
        return !ContainsTreeItem(source, target);
    }

    private static bool ContainsTreeItem(TreeNodeItemVM root, TreeNodeItemVM candidate)
    {
        if (ReferenceEquals(root, candidate)) return true;
        return root.Children.Any(child => ContainsTreeItem(child, candidate));
    }

    private static TreeViewItem? FindTreeItem(DependencyObject? source)
    {
        while (source != null && source is not TreeViewItem)
            source = VisualTreeHelper.GetParent(source);
        return source as TreeViewItem;
    }

    private void TreeVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TreeNodeItemVM item } || item.Model == null)
            return;

        var targetVisible = !item.IsVisible;
        SetTreeBranchVisibility(item, targetVisible);
        e.Handled = true;
    }

    private void TreeItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem treeItem)
        {
            treeItem.IsSelected = true;
            treeItem.Focus();
        }
    }

    private void ShowOnlySelectedBranch_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetContextTreeItem(sender) ?? _selectedTreeItem;
        if (selected == null) return;

        var keepVisible = new HashSet<TreeNodeItemVM>();
        CollectBranch(selected, keepVisible);

        foreach (var root in _vm.TreeNodes)
            ApplyOnlyBranchVisibility(root, keepVisible);

        RebuildLines();
        RefreshLineEditor();
        _vm.Status = $"仅显示：{selected.Model?.renderNodeIDInfo ?? selected.Header} 及其子节点（包含 LineNode）";
    }

    private static void CollectBranch(TreeNodeItemVM item, HashSet<TreeNodeItemVM> result)
    {
        result.Add(item);
        foreach (var child in item.Children)
            CollectBranch(child, result);
    }

    private void ApplyOnlyBranchVisibility(TreeNodeItemVM item, HashSet<TreeNodeItemVM> keepVisible)
    {
        SetSingleTreeItemVisibility(item, keepVisible.Contains(item));

        foreach (var child in item.Children)
            ApplyOnlyBranchVisibility(child, keepVisible);
    }

    private void SetSingleTreeItemVisibility(TreeNodeItemVM item, bool visible)
    {
        item.IsVisible = visible;
        if (item.Model == null || string.IsNullOrWhiteSpace(item.Model.renderNodeIDInfo))
            return;

        if (visible)
            _hiddenNodeIds.Remove(item.Model.renderNodeIDInfo);
        else
            _hiddenNodeIds.Add(item.Model.renderNodeIDInfo);

        if (item.CanvasNode != null)
            item.CanvasNode.IsVisible = visible;
    }

    private void ShowAllNodes_Click(object sender, RoutedEventArgs e)
    {
        _hiddenNodeIds.Clear();
        foreach (var root in _vm.TreeNodes)
            SetTreeBranchVisibility(root, true);
    }

    private static TreeNodeItemVM? GetContextTreeItem(object sender)
    {
        if (sender is not MenuItem menuItem || menuItem.Parent is not ContextMenu menu)
            return null;
        return (menu.PlacementTarget as TreeViewItem)?.DataContext as TreeNodeItemVM;
    }

    private void SetTreeBranchVisibility(TreeNodeItemVM item, bool visible)
    {
        item.IsVisible = visible;
        if (item.Model != null && !string.IsNullOrWhiteSpace(item.Model.renderNodeIDInfo))
        {
            if (visible)
                _hiddenNodeIds.Remove(item.Model.renderNodeIDInfo);
            else
                _hiddenNodeIds.Add(item.Model.renderNodeIDInfo);

            if (item.CanvasNode != null)
                item.CanvasNode.IsVisible = visible;

            var line = _vm.Lines.FirstOrDefault(x => string.Equals(x.Id, item.Model.renderNodeIDInfo, StringComparison.OrdinalIgnoreCase));
            if (line != null)
            {
                // LineVM 不是可通知对象，重建一次以刷新 Visibility 绑定。
                RebuildLines();
            }
        }

        foreach (var child in item.Children)
            SetTreeBranchVisibility(child, visible);
    }

    private void AddNode_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_project == null)
            {
                MessageBox.Show(this, "请先加载 skilltree。", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var suggestedIndex = _selectedTreeItem?.Model?.__sourceIndexFile ?? _project.IndexFileToNodes.Keys.FirstOrDefault();
            var selectedParentId = _selectedTreeItem?.Model?.renderNodeIDInfo;
            var selectedParentType = _selectedTreeItem?.Model?.typeInfo;

            // 虚拟根节点不参与存储，但作为新增节点的命名前缀来源。
            // 因而在虚拟 RenderNode 下新增时，输入框默认得到 "RenderNode."。
            if (_selectedTreeItem?.Model == null && string.Equals(_selectedTreeItem?.Header, "RenderNode", StringComparison.Ordinal))
            {
                selectedParentId = "RenderNode";
                selectedParentType = "VirtualRoot";
            }

            var dlg = new AddNodeWindow(
                _project.IndexFileToNodes.Keys.Select(Path.GetFileName)!,
                Path.GetFileName(suggestedIndex),
                selectedParentId,
                selectedParentType)
            {
                Owner = this
            };
            if (dlg.ShowDialog() != true || dlg.Result == null)
                return;

            var req = dlg.Result;

            var targetIndexPath = _project.IndexFileToNodes.Keys.FirstOrDefault(p =>
                string.Equals(Path.GetFileName(p), req.TargetIndexFile, StringComparison.OrdinalIgnoreCase));
            if (targetIndexPath == null)
            {
                MessageBox.Show(this, "找不到目标 index 文件。", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // duplicate guard
            var allNodes = _project.IndexFileToNodes.Values.SelectMany(x => x);
            if (allNodes.Any(n => string.Equals(n.renderNodeIDInfo, req.RenderNodeId, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, $"RenderNode 已存在：{req.RenderNodeId}", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(req.BindSkillId) && _project.SkillIdToNode.ContainsKey(req.BindSkillId))
            {
                var res = MessageBox.Show(this, $"SkillID 已存在：{req.BindSkillId}\n仍然继续创建 RenderNode 并绑定到已有 SkillNode 吗？", "SkillTreeEditor", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes)
                    return;
            }

            var newNode = CreateRenderNodeFromRequest(req, targetIndexPath);
            _project.IndexFileToNodes[targetIndexPath].Add(newNode);

            if (req.AttachToSelectedParent && _selectedTreeItem?.Model != null)
            {
                _selectedTreeItem.Model.subRenderNodeInfo ??= new List<string>();
                if (!_selectedTreeItem.Model.subRenderNodeInfo.Contains(req.RenderNodeId, StringComparer.OrdinalIgnoreCase))
                    _selectedTreeItem.Model.subRenderNodeInfo.Add(req.RenderNodeId);
            }

            if (req.CreateSkillNode && !string.IsNullOrWhiteSpace(req.BindSkillId) && !_project.SkillIdToNode.ContainsKey(req.BindSkillId))
            {
                var skillNode = new SkillNode
                {
                    skillID = req.BindSkillId!,
                    cost = req.Cost,
                    conditions = new List<SkillNodeConditionInfo>(),
                    descriptionInfos = new Dictionary<string, SkillNodeDescriptionInfo>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Chinese"] = new SkillNodeDescriptionInfo { name = req.ChineseName, description = req.ChineseDescription },
                        ["English"] = new SkillNodeDescriptionInfo { name = req.EnglishName, description = req.EnglishDescription },
                    },
                    __sourceSkillFile = SkillTreeIO.BuildSkillNodeFilePath(_project, req.SkillFolder, req.BindSkillId!)
                };
                _project.SkillIdToNode[skillNode.skillID] = skillNode;

                try
                {
                    var text = JsonConvert.SerializeObject(skillNode, Formatting.Indented);
                    Directory.CreateDirectory(Path.GetDirectoryName(skillNode.__sourceSkillFile!)!);
                    File.WriteAllText(skillNode.__sourceSkillFile!, text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"已创建 SkillNode 但写入文件失败：\n{skillNode.__sourceSkillFile}\n\n{ex}", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            RebuildViewModel();
            var addedVm = _vm.Nodes.FirstOrDefault(n => string.Equals(n.NodeId, req.RenderNodeId, StringComparison.OrdinalIgnoreCase));
            if (addedVm != null)
                SelectNode(addedVm);
            _vm.Status = $"已新增节点：{req.RenderNodeId}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"打开/创建新增节点窗口时发生错误：\n\n{ex}", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteNode_Click(object sender, RoutedEventArgs e)
    {
        if (_project == null)
        {
            MessageBox.Show(this, "请先加载 skilltree。", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_selectedTreeItem?.IsMissing == true &&
            _selectedTreeItem.Parent?.Model != null &&
            !string.IsNullOrWhiteSpace(_selectedTreeItem.MissingRenderNodeId))
        {
            var missingId = _selectedTreeItem.MissingRenderNodeId!;
            var removed = _selectedTreeItem.Parent.Model.subRenderNodeInfo?.RemoveAll(id =>
                string.Equals(id, missingId, StringComparison.OrdinalIgnoreCase)) ?? 0;
            if (removed > 0)
            {
                ClearMoveUndo();
                RebuildViewModel();
                _vm.Status = $"已清除缺失节点引用：{missingId}";
            }
            return;
        }

        // 优先删画布选中的节点，否则删树选中的节点（可能是 Group/Line）
        var model = _vm.SelectedNode?.Model ?? _selectedTreeItem?.Model;
        if (model == null || string.IsNullOrWhiteSpace(model.renderNodeIDInfo))
        {
            MessageBox.Show(this, "请先选择一个要删除的节点。", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var renderNodeId = model.renderNodeIDInfo;
        var allRenderNodes = _project.IndexFileToNodes.Values.SelectMany(x => x).ToList();

        // 找到该节点所在 index 文件
        var indexPath = model.__sourceIndexFile;
        if (string.IsNullOrWhiteSpace(indexPath) || !_project.IndexFileToNodes.ContainsKey(indexPath))
        {
            indexPath = _project.IndexFileToNodes
                .FirstOrDefault(kv => kv.Value.Any(n => string.Equals(n.renderNodeIDInfo, renderNodeId, StringComparison.OrdinalIgnoreCase)))
                .Key;
        }

        if (string.IsNullOrWhiteSpace(indexPath))
        {
            MessageBox.Show(this, $"找不到该节点所在的 index 文件：{renderNodeId}", "SkillTreeEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var bindSkillId = model.bindSkillNodeInfo;
        SkillNode? skillNode = null;
        var hasSkill = !string.IsNullOrWhiteSpace(bindSkillId) && _project.SkillIdToNode.TryGetValue(bindSkillId!, out skillNode);
        var referencedElsewhere = false;
        if (hasSkill)
        {
            referencedElsewhere = allRenderNodes.Any(n =>
                !string.Equals(n.renderNodeIDInfo, renderNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(n.bindSkillNodeInfo, bindSkillId, StringComparison.OrdinalIgnoreCase));
        }

        var msg = $"确定要删除节点吗？\n\nRenderNode: {renderNodeId}\nIndex: {Path.GetFileName(indexPath)}";
        if (hasSkill)
        {
            msg += $"\n\n绑定 SkillID: {bindSkillId}";
            if (referencedElsewhere) msg += "\n注意：该 SkillID 还被其他节点引用。";
            msg += "\n\n选择“是”将同时删除对应的 skill JSON 文件（如果存在）。";
        }

        var confirm = MessageBox.Show(this, msg, "删除节点", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        // 1) 从 index 文件列表移除节点
        _project.IndexFileToNodes[indexPath].RemoveAll(n => string.Equals(n.renderNodeIDInfo, renderNodeId, StringComparison.OrdinalIgnoreCase));

        // 2) 从所有父节点的 subRenderNodeInfo 移除引用
        foreach (var n in allRenderNodes)
        {
            if (n.subRenderNodeInfo == null) continue;
            n.subRenderNodeInfo.RemoveAll(id => string.Equals(id, renderNodeId, StringComparison.OrdinalIgnoreCase));
        }

        // 3) 删除 skill 数据（若存在）
        if (hasSkill)
        {
            if (referencedElsewhere)
            {
                var confirm2 = MessageBox.Show(this, $"SkillID 仍被其他节点引用：{bindSkillId}\n仍然删除 skill 文件吗？", "删除 SkillNode", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm2 == MessageBoxResult.Yes)
                    DeleteSkillNodeAndFile(_project, skillNode!);
            }
            else
            {
                DeleteSkillNodeAndFile(_project, skillNode!);
            }
        }

        RebuildViewModel();
        _vm.Status = $"已删除节点：{renderNodeId}";
    }

    private static void DeleteSkillNodeAndFile(SkillTreeProject project, SkillNode skillNode)
    {
        if (string.IsNullOrWhiteSpace(skillNode.skillID)) return;
        project.SkillIdToNode.Remove(skillNode.skillID);

        if (string.IsNullOrWhiteSpace(skillNode.__sourceSkillFile))
            return;

        try
        {
            var file = Path.GetFullPath(skillNode.__sourceSkillFile!);
            var skillsRoot = Path.GetFullPath(Path.Combine(project.SkillTreeFolder, "skills"));
            if (!file.StartsWith(skillsRoot, StringComparison.OrdinalIgnoreCase))
                return;
            if (File.Exists(file))
                File.Delete(file);
        }
        catch
        {
            // ignore
        }
    }

    private SkillTreeRenderNode CreateRenderNodeFromRequest(AddNodeRequest req, string targetIndexPath)
    {
        var node = new SkillTreeRenderNode
        {
            renderNodeIDInfo = req.RenderNodeId,
            bindSkillNodeInfo = string.IsNullOrWhiteSpace(req.BindSkillId) ? null : req.BindSkillId,
            layer = req.NodeType.Equals("SubBasicNode", StringComparison.OrdinalIgnoreCase)
                ? 2
                : req.NodeType.Equals("LineNode", StringComparison.OrdinalIgnoreCase)
                    ? (req.ParentIsNodeGroup ? 2 : 0)
                    : 1,
            scaleInfo = req.ScaleInfo,
            typeInfo = req.NodeType,
            iconSprite = string.IsNullOrWhiteSpace(req.IconSprite) ? null : req.IconSprite,
            __sourceIndexFile = targetIndexPath
        };

        switch (req.NodeType)
        {
            case "StaticNode":
                node.posInfo = new List<Vec2> { new(0, 0) };
                break;
            case "BasicNode":
            case "SubBasicNode":
                node.posInfo = new List<Vec2> { new(0, 0), new(180, 0) };
                break;
            case "LineNode":
                if (_vm.ViewMode == LayoutViewMode.Expand)
                {
                    // Expand 的 1366×768 可见范围以 0,0 为左下角；新线放在范围中心附近。
                    node.posInfo = new List<Vec2> { new(633, 384), new(733, 384) };
                }
                else
                {
                    node.posInfo = new List<Vec2> { new(-50, 0), new(50, 0) };
                }
                break;
            case "NodeGroup":
                node.subRenderNodeInfo = new List<string>();
                break;
            default:
                node.posInfo = new List<Vec2> { new(0, 0), new(180, 0) };
                break;
        }

        return node;
    }
}
