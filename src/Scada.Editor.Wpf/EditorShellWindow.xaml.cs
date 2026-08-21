using System.Windows;

namespace Scada.Editor.Wpf;

public partial class EditorShellWindow : Window
{
    public EditorShellWindow(EditorRole role = EditorRole.Developer)
    {
        InitializeComponent();
        DataContext = new EditorShellViewModel(role);
        if (!EditorAuthorization.CanEdit(role))
        {
            ToolboxPanel.Visibility = Visibility.Collapsed;
            InspectorTabs.Visibility = Visibility.Collapsed;
        }
    }
}
