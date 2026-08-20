using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Scada.Controls;
using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Controls.Wpf;

namespace Scada.Controls.Preview.Wpf;

internal sealed class MainWindow : Window
{
    private readonly Canvas _sheet;
    private readonly ComboBox _stateSelector;
    private readonly CheckBox _reducedMotion;

    public MainWindow()
    {
        Title = "Industrial Control SDK Preview";
        Width = 1280;
        Height = 820;
        MinWidth = 900;
        MinHeight = 620;
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(VisualTokens.Canvas));

        _stateSelector = new ComboBox
        {
            Width = 150,
            Margin = new Thickness(8),
            ItemsSource = Enum.GetValues<ControlState>(),
            SelectedItem = ControlState.Stopped
        };
        _stateSelector.SelectionChanged += (_, _) => RefreshSheet();
        _reducedMotion = new CheckBox
        {
            Content = "减弱动画",
            Foreground = Brushes.White,
            Margin = new Thickness(8),
            VerticalAlignment = VerticalAlignment.Center
        };
        _reducedMotion.Checked += (_, _) => RefreshSheet();
        _reducedMotion.Unchecked += (_, _) => RefreshSheet();

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Background = new SolidColorBrush(Color.FromRgb(45, 51, 56)) };
        toolbar.Children.Add(new TextBlock { Text = "状态", Foreground = Brushes.White, Margin = new Thickness(12, 8, 0, 8), VerticalAlignment = VerticalAlignment.Center });
        toolbar.Children.Add(_stateSelector);
        toolbar.Children.Add(_reducedMotion);

        _sheet = new Canvas { Background = Background, ClipToBounds = true };
        Content = new DockPanel
        {
            Children =
            {
                toolbar,
                _sheet
            }
        };
        DockPanel.SetDock(toolbar, Dock.Top);
        Loaded += (_, _) => RefreshSheet();
    }

    private void RefreshSheet()
    {
        _sheet.Children.Clear();
        var state = _stateSelector.SelectedItem is ControlState selected ? selected : ControlState.Stopped;
        var context = PreviewPlans.CreateContext(state, _reducedMotion.IsChecked == true);
        var x = 40d;
        var y = 50d;
        foreach (var typeId in ControlTypeIds.All)
        {
            var plan = ControlGeometryFactory.Build(typeId, context);
            var control = new IndustrialControl
            {
                RenderPlan = plan,
                State = state,
                ReducedMotion = context.ReducedMotion,
                Width = Math.Min(210, Math.Max(120, plan.DesignSize.Width * 1.35)),
                Height = Math.Min(240, Math.Max(70, plan.DesignSize.Height * 1.1))
            };
            Canvas.SetLeft(control, x);
            Canvas.SetTop(control, y);
            _sheet.Children.Add(control);
            x += 230;
            if (x > 980)
            {
                x = 40;
                y += 260;
            }
        }
    }
}
