using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Scada.Controls;
using Scada.Controls.Rendering;
using Scada.Controls.SampleProject;
using Scada.Controls.Wpf;

namespace Scada.Controls.Preview.Wpf;

internal sealed class MainWindow : Window
{
    private readonly Canvas _sheet;
    private readonly ComboBox _scenarioSelector;
    private readonly CheckBox _reducedMotion;

    public MainWindow()
    {
        Title = "Industrial Control SDK Preview";
        Width = 1280;
        Height = 820;
        MinWidth = 900;
        MinHeight = 620;
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(VisualTokens.Canvas));

        _scenarioSelector = new ComboBox
        {
            Width = 150,
            Margin = new Thickness(8),
            ItemsSource = new[] { "stopped", "active", "transition", "fault", "unknown" },
            SelectedItem = "stopped"
        };
        _scenarioSelector.SelectionChanged += (_, _) => RefreshSheet();
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
        toolbar.Children.Add(new TextBlock { Text = "离线场景", Foreground = Brushes.White, Margin = new Thickness(12, 8, 0, 8), VerticalAlignment = VerticalAlignment.Center });
        toolbar.Children.Add(_scenarioSelector);
        toolbar.Children.Add(_reducedMotion);

        _sheet = new Canvas { Width = 1920, Height = 1080, Background = Background, ClipToBounds = true };
        var scrollViewer = new ScrollViewer { Content = _sheet, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Content = new DockPanel
        {
            Children =
            {
                toolbar,
                scrollViewer
            }
        };
        DockPanel.SetDock(toolbar, Dock.Top);
        Loaded += (_, _) => RefreshSheet();
    }

    private void RefreshSheet()
    {
        _sheet.Children.Clear();
        var scenarioName = _scenarioSelector.SelectedItem as string ?? "stopped";
        foreach (var renderable in SampleProjectFactory.BuildRenderables(scenarioName, reducedMotion: _reducedMotion.IsChecked == true))
        {
            var control = new IndustrialControl
            {
                RenderPlan = renderable.Plan,
                State = renderable.Plan.State,
                ReducedMotion = _reducedMotion.IsChecked == true,
                Width = renderable.Control.Bounds.Width,
                Height = renderable.Control.Bounds.Height
            };
            Canvas.SetLeft(control, renderable.Control.Bounds.X);
            Canvas.SetTop(control, renderable.Control.Bounds.Y);
            _sheet.Children.Add(control);
        }
    }
}
