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
    public void ShellViewModelExposesProductivityCommandsWithEngineeringEnablement()
    {
        var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 0, 40, 40));
        var project = ProjectDocument.Create(
            "Command test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve }) });
        var session = new EditorSession(project, "Main");
        session.SelectOnly(valve.Id);
        session.MoveSelection(1, 0);

        var developer = new EditorShellViewModel(EditorRole.Developer, session: session);
        Assert.Equal("撤销", developer.UndoCommandLabel);
        Assert.Equal("重做", developer.RedoCommandLabel);
        Assert.Equal("左对齐", developer.AlignLeftCommandLabel);
        Assert.Equal("水平等距", developer.DistributeHorizontalCommandLabel);
        Assert.True(developer.UndoCommand.CanExecute(null));
        Assert.False(developer.RedoCommand.CanExecute(null));
        Assert.False(developer.AlignLeftCommand.CanExecute(null));
        Assert.False(developer.GroupCommand.CanExecute(null));

        var operatorView = new EditorShellViewModel(EditorRole.Operator, session: session);
        Assert.False(operatorView.UndoCommand.CanExecute(null));
        Assert.False(operatorView.AlignLeftCommand.CanExecute(null));
        Assert.False(operatorView.GroupCommand.CanExecute(null));
    }

    [Fact]
    public void SelectionChangesRefreshAlignmentAndDistributionCommandEnablement()
    {
        var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 0, 40, 40));
        var second = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(80, 20, 60, 60));
        var project = ProjectDocument.Create(
            "Selection command test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { first, second }) });
        var session = new EditorSession(project, "Main");
        var developer = new EditorShellViewModel(EditorRole.Developer, session: session);

        Assert.False(developer.AlignTopCommand.CanExecute(null));
        Assert.False(developer.AlignBottomCommand.CanExecute(null));
        Assert.False(developer.AlignLeftCommand.CanExecute(null));
        Assert.False(developer.DistributeHorizontalCommand.CanExecute(null));

        var canExecuteChanged = false;
        developer.AlignTopCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;

        session.SelectMany(new[] { first.Id, second.Id });

        Assert.True(canExecuteChanged);
        Assert.True(developer.AlignTopCommand.CanExecute(null));
        Assert.True(developer.AlignBottomCommand.CanExecute(null));
        Assert.True(developer.AlignLeftCommand.CanExecute(null));
        Assert.True(developer.DistributeHorizontalCommand.CanExecute(null));
    }

    [Fact]
    public void GeometryCommandRefreshesCanvasImmediatelyWithoutBlankCanvasClick()
    {
        StaThread.Run(() =>
        {
            var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(100, 100, 40, 40));
            var second = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(240, 180, 60, 60));
            var project = ProjectDocument.Create(
                "Immediate refresh test",
                screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { first, second }) });
            var session = new EditorSession(project, "Main");
            session.SelectMany(new[] { first.Id, second.Id });
            var canvas = new EditorCanvas { Session = session };
            canvas.Refresh();
            var viewModel = new EditorShellViewModel(EditorRole.Developer, session: session);

            viewModel.AlignTopCommand.Execute(null);

            var secondVisual = canvas.Children.OfType<Border>().Single(border => Equals(border.Tag, second.Id));
            Assert.Equal(100, Canvas.GetTop(secondVisual));
            return true;
        });
    }

    [Fact]
    public void AlignmentRefreshesUndoAndRedoCommands()
    {
        var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(0, 0, 40, 40));
        var second = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(80, 20, 60, 60));
        var project = ProjectDocument.Create(
            "Undo command refresh test",
            screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { first, second }) });
        var session = new EditorSession(project, "Main");
        session.SelectMany(new[] { first.Id, second.Id });
        var viewModel = new EditorShellViewModel(EditorRole.Developer, session: session);

        Assert.False(viewModel.UndoCommand.CanExecute(null));
        Assert.False(viewModel.RedoCommand.CanExecute(null));
        var undoStateChanged = false;
        viewModel.UndoCommand.CanExecuteChanged += (_, _) => undoStateChanged = true;

        viewModel.AlignTopCommand.Execute(null);

        Assert.True(undoStateChanged);
        Assert.True(viewModel.UndoCommand.CanExecute(null));
        Assert.False(viewModel.RedoCommand.CanExecute(null));

        viewModel.UndoCommand.Execute(null);
        Assert.False(viewModel.UndoCommand.CanExecute(null));
        Assert.True(viewModel.RedoCommand.CanExecute(null));
    }

    [Fact]
    public void UndoAndRedoRefreshCanvasImmediately()
    {
        StaThread.Run(() =>
        {
            var first = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(100, 100, 40, 40));
            var second = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(240, 180, 60, 60));
            var project = ProjectDocument.Create(
                "Undo canvas refresh test",
                screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { first, second }) });
            var session = new EditorSession(project, "Main");
            session.SelectMany(new[] { first.Id, second.Id });
            var canvas = new EditorCanvas { Session = session };
            canvas.Refresh();
            var viewModel = new EditorShellViewModel(EditorRole.Developer, session: session);

            viewModel.AlignTopCommand.Execute(null);
            viewModel.UndoCommand.Execute(null);
            var secondVisual = canvas.Children.OfType<Border>().Single(border => Equals(border.Tag, second.Id));
            Assert.Equal(180, Canvas.GetTop(secondVisual));

            viewModel.RedoCommand.Execute(null);
            secondVisual = canvas.Children.OfType<Border>().Single(border => Equals(border.Tag, second.Id));
            Assert.Equal(100, Canvas.GetTop(secondVisual));
            return true;
        });
    }

    [Fact]
    public void ToolboxLeafItemsExposeStableControlTypeIds()
    {
        StaThread.Run(() =>
        {
            var window = new EditorShellWindow(EditorRole.Developer);
            var toolbox = Assert.IsType<TreeView>(window.FindName("ToolboxTree"));
            var leaves = toolbox.Items
                .OfType<TreeViewItem>()
                .SelectMany(item => item.Items.OfType<TreeViewItem>())
                .SelectMany(item => item.Items.Count == 0
                    ? new[] { item }
                    : item.Items.OfType<TreeViewItem>())
                .ToArray();

            Assert.Contains(leaves, item => Equals(item.Tag, ControlTypeIds.AutomatedValve));
            Assert.Contains(leaves, item => Equals(item.Tag, ControlTypeIds.StraightPipe));
            Assert.Contains(leaves, item => Equals(item.Tag, "text"));
            window.Close();
            return true;
        });
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
    public void SelectionHandlesUseDirectionalResizeCursorsAndRotationCursor()
    {
        StaThread.Run(() =>
        {
            var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(100, 100, 100, 80));
            var project = ProjectDocument.Create(
                "Cursor test",
                screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve }) });
            var session = new EditorSession(project, "Main");
            session.SelectOnly(valve.Id);
            var canvas = new EditorCanvas { Session = session };

            canvas.Refresh();

            var handles = canvas.Children.OfType<Thumb>().ToArray();
            Assert.Equal(9, handles.Length);
            Assert.Equal(Cursors.SizeAll, Assert.IsAssignableFrom<FrameworkElement>(Assert.IsType<Border>(canvas.Children[0]).Child).Cursor);
            Assert.Equal(Brushes.White, handles[0].Background);
            Assert.Equal(Brushes.DeepSkyBlue, handles[0].BorderBrush);
            Assert.Equal(handles[0].Background, handles[8].Background);
            Assert.Equal(handles[0].BorderBrush, handles[8].BorderBrush);
            Assert.Equal(Cursors.SizeNWSE, handles[0].Cursor);
            Assert.Equal(Cursors.SizeNS, handles[1].Cursor);
            Assert.Equal(Cursors.SizeNESW, handles[2].Cursor);
            Assert.Equal(Cursors.SizeWE, handles[3].Cursor);
            Assert.Equal(Cursors.SizeNWSE, handles[4].Cursor);
            Assert.Equal(Cursors.SizeNS, handles[5].Cursor);
            Assert.Equal(Cursors.SizeNESW, handles[6].Cursor);
            Assert.Equal(Cursors.SizeWE, handles[7].Cursor);
            Assert.NotEqual(Cursors.Hand, handles[8].Cursor);
            Assert.NotEqual(Cursors.ScrollAll, handles[8].Cursor);
            return true;
        });
    }

    [Fact]
    public void ResizeDragSupportsGrowthAndShrinkWithoutRebuildingTheActiveHandle()
    {
        StaThread.Run(() =>
        {
            var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(100, 100, 100, 80));
            var project = ProjectDocument.Create(
                "Resize drag test",
                screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve }) });
            var session = new EditorSession(project, "Main");
            session.SelectOnly(valve.Id);
            var canvas = new EditorCanvas { Session = session };
            canvas.Refresh();

            var bottomRight = canvas.Children.OfType<Thumb>().ElementAt(4);
            bottomRight.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
            bottomRight.RaiseEvent(new DragDeltaEventArgs(20, 10) { RoutedEvent = Thumb.DragDeltaEvent });

            var grown = session.ActiveScreen.FindObject(valve.Id)!;
            Assert.Equal(120, grown.Bounds.Width);
            Assert.Equal(90, grown.Bounds.Height);
            Assert.True(canvas.Children.Contains(bottomRight));

            bottomRight.RaiseEvent(new DragDeltaEventArgs(-30, -20) { RoutedEvent = Thumb.DragDeltaEvent });
            var shrunk = session.ActiveScreen.FindObject(valve.Id)!;
            Assert.Equal(90, shrunk.Bounds.Width);
            Assert.Equal(70, shrunk.Bounds.Height);

            bottomRight.RaiseEvent(new DragCompletedEventArgs(0, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
            return true;
        });
    }

    [Fact]
    public void RotationDragSupportsBothDirectionsWithoutRebuildingTheActiveHandle()
    {
        StaThread.Run(() =>
        {
            var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(100, 100, 100, 80));
            var project = ProjectDocument.Create(
                "Rotation drag test",
                screens: new[] { ScreenDocument.Create("Main", new SceneObject[] { valve }) });
            var session = new EditorSession(project, "Main");
            session.SelectOnly(valve.Id);
            var canvas = new EditorCanvas { Session = session };
            canvas.Refresh();

            var rotation = canvas.Children.OfType<Thumb>().Single(thumb => Equals(thumb.Tag, "rotation"));
            rotation.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
            rotation.RaiseEvent(new DragDeltaEventArgs(20, 0) { RoutedEvent = Thumb.DragDeltaEvent });
            rotation.RaiseEvent(new DragDeltaEventArgs(-5, 0) { RoutedEvent = Thumb.DragDeltaEvent });

            var result = session.ActiveScreen.FindObject(valve.Id)!;
            Assert.Equal(0.15, result.Rotation, 6);
            Assert.True(canvas.Children.Contains(rotation));

            rotation.RaiseEvent(new DragCompletedEventArgs(0, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
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
