using System.Windows;
using System.Windows.Controls;
using Scada.Editor.Wpf;
using Xunit;

namespace Scada.Editor.Wpf.Tests;

public sealed class WpfEditorVisualTests
{
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
}
