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

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
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
        if (RenderPlan is not null)
        {
            _root.Children.Add(WpfControlRenderer.Render(RenderPlan));
        }

        UpdateTransform(RenderSize);
        VisualStateManager.GoToState(this, StateName(State), useTransitions: !ReducedMotion && State != ControlState.Unknown);
        VisualStateManager.GoToState(this, RenderPlan?.State == ControlState.Unknown ? "UnknownQuality" : "GoodQuality", useTransitions: false);
    }

    private void UpdateTransform(Size bounds)
    {
        if (RenderPlan is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var policy = GetResizePolicy(RenderPlan);
        var scaleX = bounds.Width / RenderPlan.DesignSize.Width;
        var scaleY = bounds.Height / RenderPlan.DesignSize.Height;
        if (policy == ResizePolicy.PreserveAspectRatio)
        {
            var uniform = Math.Min(scaleX, scaleY);
            scaleX = uniform;
            scaleY = uniform;
        }

        GeometryScaleX = scaleX;
        GeometryScaleY = scaleY;
        if (_root is null)
        {
            return;
        }

        _root.RenderTransform = new TransformGroup
        {
            Children = new TransformCollection
            {
                new ScaleTransform(scaleX, scaleY),
                new RotateTransform(OrientationDegrees(Orientation), 0, 0)
            }
        };
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
}
