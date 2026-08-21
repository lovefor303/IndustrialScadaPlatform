using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Scada.Editor.Wpf;
using Scada.Controls;
using Scada.Scene;
using Xunit;

namespace Scada.Editor.Wpf.Tests;

public sealed class WpfEditorVisualTests
{
    [Fact]
    public void CanvasRendersIndependentValveAndPipeObjects()
    {
        StaThread.Run(() =>
        {
            var valve = ControlObject.Create(
                ControlTypeIds.AutomatedValve,
                new RectD(100, 100, 80, 60));
            var pipe = PipeObject.Create(
                new PointD(20, 130),
                new PointD(100, 130),
                new[] { new PointD(60, 130) });
            var project = ProjectDocument.Create(
                "Canvas test",
                screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve, pipe }) });
            var session = new EditorSession(project, "Main");
            session.SelectOnly(valve.Id);
            var canvas = new EditorCanvas { Session = session };

            canvas.Refresh();

            Assert.True(canvas.Children.Count >= 2);
            Assert.Contains(canvas.Children.OfType<Border>(), border =>
                border.Tag is Guid id && id == valve.Id);
            Assert.Contains(canvas.Children.OfType<Polyline>(), polyline => polyline.Points.Count == 3);
            return true;
        });
    }

    [Fact]
    public void DeveloperShellLoadsTheWinccStylePanels()
    {
        StaThread.Run(() =>
        {
            var window = new EditorShellWindow(EditorRole.Developer);
            var toolbox = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("ToolboxPanel"));
            var tabs = Assert.IsType<TabControl>(window.FindName("InspectorTabs"));

            Assert.Equal(Visibility.Visible, toolbox.Visibility);
            Assert.Equal(4, tabs.Items.Count);
            Assert.Equal("属性", ((TabItem)tabs.Items[0]).Header);
            Assert.Equal("变量", ((TabItem)tabs.Items[1]).Header);
            Assert.Equal("动态", ((TabItem)tabs.Items[2]).Header);
            Assert.Equal("事件", ((TabItem)tabs.Items[3]).Header);
            window.Close();
            return true;
        });
    }

    [Fact]
    public void OperatorShellDoesNotExposeEngineeringPanels()
    {
        StaThread.Run(() =>
        {
            var window = new EditorShellWindow(EditorRole.Operator);
            var toolbox = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("ToolboxPanel"));
            var tabs = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("InspectorTabs"));

            Assert.Equal(Visibility.Collapsed, toolbox.Visibility);
            Assert.Equal(Visibility.Collapsed, tabs.Visibility);
            window.Close();
            return true;
        });
    }

    [Fact]
    public void DeveloperShellExposesEditableInspectorSurfaces()
    {
        StaThread.Run(() =>
        {
            var window = new EditorShellWindow(EditorRole.Developer);

            Assert.IsAssignableFrom<FrameworkElement>(window.FindName("PropertyPanel"));
            Assert.IsAssignableFrom<FrameworkElement>(window.FindName("BindingPanel"));
            Assert.IsAssignableFrom<FrameworkElement>(window.FindName("DynamicsPanel"));
            Assert.IsAssignableFrom<FrameworkElement>(window.FindName("EventsPanel"));

            window.Close();
            return true;
        });
    }

    [Fact]
    public void ShellViewModelExposesEngineeringProjectCommandsOnlyToEditors()
    {
        var developer = new EditorShellViewModel(EditorRole.Developer);
        Assert.IsAssignableFrom<ICommand>(developer.NewProjectCommand);
        Assert.IsAssignableFrom<ICommand>(developer.OpenProjectCommand);
        Assert.IsAssignableFrom<ICommand>(developer.SaveDraftCommand);
        Assert.True(developer.SaveDraftCommand.CanExecute(null));

        var operatorView = new EditorShellViewModel(EditorRole.Operator);
        Assert.False(operatorView.SaveDraftCommand.CanExecute(null));
    }

    [Fact]
    public void ViewportZoomKeepsCursorModelPointAndSupportsGridSnapping()
    {
        var viewport = new EditorViewport();
        var anchor = new Point(240, 180);
        var modelPoint = viewport.ScreenToModel(anchor);

        viewport.SetZoomAt(2, anchor);

        Assert.Equal(modelPoint.X, viewport.ScreenToModel(anchor).X, 6);
        Assert.Equal(modelPoint.Y, viewport.ScreenToModel(anchor).Y, 6);
        Assert.Equal(new Point(30, 20), viewport.SnapToGrid(new Point(27, 24)));

        viewport.GridEnabled = false;
        Assert.Equal(new Point(27, 24), viewport.SnapToGrid(new Point(27, 24)));
        var panBefore = viewport.Pan;
        viewport.PanBy(new Vector(10, -5));
        Assert.Equal(panBefore + new Vector(10, -5), viewport.Pan);
    }

    [Fact]
    public void SelectedObjectRendersResizeAndRotationHandles()
    {
        StaThread.Run(() =>
        {
            var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(100, 100, 100, 80));
            var pipe = PipeObject.Create(new PointD(0, 140), new PointD(100, 140));
            var project = ProjectDocument.Create(
                "Handle test",
                screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve, pipe }) });
            var session = new EditorSession(project, "Main");
            session.SelectOnly(valve.Id);
            var canvas = new EditorCanvas { Session = session };

            canvas.Refresh();

            Assert.Equal(9, canvas.Children.OfType<Thumb>().Count());
            Assert.Contains(canvas.Children.OfType<Thumb>(), thumb => Equals(thumb.Tag, "rotation"));
            return true;
        });
    }

    [Fact]
    public void EditorShellRendersNonEmptyAt1920By1080()
    {
        StaThread.Run(() =>
        {
            var window = new EditorShellWindow(EditorRole.Developer);
            window.Show();
            window.Measure(new Size(1920, 1080));
            window.Arrange(new Rect(0, 0, 1920, 1080));
            window.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var pixels = new byte[1920 * 1080 * 4];
            bitmap.CopyPixels(pixels, 1920 * 4, 0);

            Assert.Contains(pixels, value => value != 0);
            window.Close();
            return true;
        });
    }
}
