using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace Scada.Editor.Wpf;

public partial class EditorShellWindow : Window
{
    private Point _toolboxDragStart;
    private TreeViewItem? _toolboxDragItem;

    public EditorShellWindow(
        EditorRole role = EditorRole.Developer,
        EditorSession? session = null,
        EditorCommands? commands = null,
        IProjectFileDialog? fileDialog = null)
    {
        InitializeComponent();
        DataContext = new EditorShellViewModel(
            role,
            commands,
            fileDialog ?? (commands is null ? null : new WpfProjectFileDialog()),
            session);
        if (session is not null)
        {
            ProcessCanvas.Session = session;
            ProcessCanvas.Refresh();
        }

        if (!EditorAuthorization.CanEdit(role))
        {
            ToolboxPanel.Visibility = Visibility.Collapsed;
            InspectorTabs.Visibility = Visibility.Collapsed;
        }
    }

    private void OnToolboxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _toolboxDragItem = FindTreeViewItem(e.OriginalSource as DependencyObject);
        _toolboxDragStart = e.GetPosition(ToolboxTree);
    }

    private void OnToolboxPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_toolboxDragItem?.Tag is not string typeId
            || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(ToolboxTree);
        if (Math.Abs(current.X - _toolboxDragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _toolboxDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _toolboxDragItem = null;
        EditorCanvas.BeginToolboxDrag(sender as DependencyObject ?? ToolboxTree, typeId);
        e.Handled = true;
    }

    private void OnToolboxPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        _toolboxDragItem = null;

    private static TreeViewItem? FindTreeViewItem(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is TreeViewItem item)
            {
                return item.Items.Count == 0 ? item : null;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private sealed class WpfProjectFileDialog : IProjectFileDialog
    {
        public Task<string?> PickOpenPathAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dialog = new OpenFileDialog
            {
                Title = "打开组态项目",
                Filter = "组态项目 (*.json)|*.json",
                CheckFileExists = true,
                Multiselect = false
            };
            return Task.FromResult<string?>(dialog.ShowDialog() == true ? dialog.FileName : null);
        }

        public Task<string?> PickSavePathAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dialog = new SaveFileDialog
            {
                Title = "导出组态项目",
                Filter = "组态项目 (*.json)|*.json",
                DefaultExt = ".json",
                AddExtension = true
            };
            return Task.FromResult<string?>(dialog.ShowDialog() == true ? dialog.FileName : null);
        }
    }
}
