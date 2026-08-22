using System.Windows;

namespace Scada.Editor.Wpf;

public partial class EditorShellWindow : Window
{
    public EditorShellWindow(EditorRole role = EditorRole.Developer, EditorSession? session = null)
    {
        InitializeComponent();
        DataContext = new EditorShellViewModel(role, session: session);
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
}
