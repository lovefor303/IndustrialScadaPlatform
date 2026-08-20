using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Scada.Controls.Rendering;

namespace Scada.Controls.Wpf;

public enum ControlOrientation
{
    Normal,
    Right,
    UpsideDown,
    Left
}

/// <summary>
/// Lookless host for one control render plan. Geometry is scaled independently from
/// scene placement so editing bounds never become a substitute for equipment resizing.
/// </summary>
public sealed class IndustrialControl : Control
{
    private Canvas? _root;
    private Canvas? _geometry;

    static IndustrialControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(IndustrialControl),
            new FrameworkPropertyMetadata(typeof(IndustrialControl)));
    }

    public static readonly DependencyProperty RenderPlanProperty = DependencyProperty.Register(
        nameof(RenderPlan),
        typeof(ControlRenderPlan),
        typeof(IndustrialControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnVisualPropertyChanged));

    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State),
        typeof(ControlState),
        typeof(IndustrialControl),
        new FrameworkPropertyMetadata(ControlState.Neutral, FrameworkPropertyMetadataOptions.AffectsRender, OnVisualPropertyChanged));

    public static readonly DependencyProperty ReducedMotionProperty = DependencyProperty.Register(
        nameof(ReducedMotion),
        typeof(bool),
        typeof(IndustrialControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnVisualPropertyChanged));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(ControlOrientation),
        typeof(IndustrialControl),
        new FrameworkPropertyMetadata(ControlOrientation.Normal, FrameworkPropertyMetadataOptions.AffectsArrange, OnVisualPropertyChanged));

    public ControlRenderPlan? RenderPlan
    {
        get => (ControlRenderPlan?)GetValue(RenderPlanProperty);
        set => SetValue(RenderPlanProperty, value);
    }

    public ControlState State
    {
        get => (ControlState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool ReducedMotion
    {
        get => (bool)GetValue(ReducedMotionProperty);
        set => SetValue(ReducedMotionProperty, value);
    }

    public ControlOrientation Orientation
    {
        get => (ControlOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double GeometryScaleX { get; private set; } = 1;

    public double GeometryScaleY { get; private set; } = 1;

    public Rect RenderedGeometryBounds { get; private set; } = Rect.Empty;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        EnsureApplicationThemeResources();
        _root = GetTemplateChild("PART_Root") as Canvas;
        RefreshVisual();
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        var result = base.ArrangeOverride(arrangeBounds);
        UpdateTransform(arrangeBounds);
        return result;
    }

    private static void OnVisualPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs _) =>
        ((IndustrialControl)dependencyObject).RefreshVisual();

    private void RefreshVisual()
    {
        if (_root is null)
        {
            return;
        }

        _root.Children.Clear();
        _geometry = null;
        if (RenderPlan is not null)
        {
            _geometry = WpfControlRenderer.Render(RenderPlan, EffectiveStateOverride(), ReducedMotion);
            _root.Children.Add(_geometry);
        }

        UpdateTransform(RenderSize);
        var effectiveState = EffectiveState;
        VisualStateManager.GoToState(this, StateName(effectiveState), useTransitions: !ReducedMotion && effectiveState != ControlState.Unknown);
        VisualStateManager.GoToState(this, effectiveState == ControlState.Unknown || PlanHasBadQuality(RenderPlan) ? "UnknownQuality" : "GoodQuality", useTransitions: false);
    }

    private void UpdateTransform(Size bounds)
    {
        if ((!double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height))
            && double.IsFinite(Width) && double.IsFinite(Height)
            && Width > 0 && Height > 0)
        {
            bounds = new Size(Width, Height);
        }

        if (RenderPlan is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            RenderedGeometryBounds = Rect.Empty;
            return;
        }

        var designWidth = RenderPlan.DesignSize.Width;
        var designHeight = RenderPlan.DesignSize.Height;
        var quarterTurn = Orientation is ControlOrientation.Right or ControlOrientation.Left;
        var orientedWidth = quarterTurn ? designHeight : designWidth;
        var orientedHeight = quarterTurn ? designWidth : designHeight;
        var policy = GetResizePolicy(RenderPlan);
        var scaleX = bounds.Width / orientedWidth;
        var scaleY = bounds.Height / orientedHeight;
        if (policy == ResizePolicy.PreserveAspectRatio || quarterTurn)
        {
            var uniform = Math.Min(scaleX, scaleY);
            scaleX = uniform;
            scaleY = uniform;
        }

        GeometryScaleX = scaleX;
        GeometryScaleY = scaleY;
        var renderedWidth = orientedWidth * scaleX;
        var renderedHeight = orientedHeight * scaleY;
        RenderedGeometryBounds = new Rect(
            Math.Max(0, (bounds.Width - renderedWidth) / 2),
            Math.Max(0, (bounds.Height - renderedHeight) / 2),
            renderedWidth,
            renderedHeight);

        if (_geometry is null)
        {
            return;
        }

        var centerX = (designWidth * scaleX) / 2;
        var centerY = (designHeight * scaleY) / 2;
        var transform = Matrix.Identity;
        transform.Scale(scaleX, scaleY);
        transform.RotateAt(OrientationDegrees(Orientation), centerX, centerY);
        transform.Translate((bounds.Width / 2) - centerX, (bounds.Height / 2) - centerY);
        _geometry.RenderTransform = new MatrixTransform(transform);
    }

    private static ResizePolicy GetResizePolicy(ControlRenderPlan plan)
    {
        try
        {
            return ControlCatalog.CreateDefault().Get(plan.TypeId, plan.Version).ResizePolicy;
        }
        catch (KeyNotFoundException)
        {
            return ResizePolicy.PreserveAspectRatio;
        }
    }

    private static string StateName(ControlState state) => state switch
    {
        ControlState.Active => "Active",
        ControlState.Transition => "Transition",
        ControlState.Fault => "Fault",
        ControlState.Unknown => "Unknown",
        ControlState.Stopped => "Stopped",
        _ => "Neutral"
    };

    private static int OrientationDegrees(ControlOrientation orientation) => orientation switch
    {
        ControlOrientation.Right => 90,
        ControlOrientation.UpsideDown => 180,
        ControlOrientation.Left => 270,
        _ => 0
    };

    private ControlState EffectiveState => EffectiveStateOverride() ?? RenderPlan?.State ?? ControlState.Neutral;

    private ControlState? EffectiveStateOverride() =>
        ReadLocalValue(StateProperty) == DependencyProperty.UnsetValue ? null : State;

    private static bool PlanHasBadQuality(ControlRenderPlan? plan) => plan is not null &&
        (plan.State == ControlState.Unknown || ContainsUnknownQualityMarker(plan.Primitives));

    private static bool ContainsUnknownQualityMarker(IEnumerable<RenderPrimitive> primitives) => primitives.Any(primitive =>
        primitive.PartId == "quality.unknown"
        || primitive is RenderGroup group && ContainsUnknownQualityMarker(group.Children));

    private static void EnsureApplicationThemeResources()
    {
        var application = Application.Current;
        if (application is null)
        {
            return;
        }

        var source = new ResourceDictionary
        {
            Source = new Uri("/Scada.Controls.Wpf;component/Themes/Generic.xaml", UriKind.Relative)
        };
        foreach (var key in source.Keys.OfType<string>().Where(key => key.StartsWith("ScadaBrush.", StringComparison.Ordinal)))
        {
            if (!application.Resources.Contains(key))
            {
                application.Resources[key] = source[key];
            }
        }
    }
}
