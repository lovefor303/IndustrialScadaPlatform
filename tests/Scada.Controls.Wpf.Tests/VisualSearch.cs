using System.Windows;
using System.Windows.Media;

namespace Scada.Controls.Wpf.Tests;

internal static class VisualSearch
{
    public static DependencyObject? ByPartId(DependencyObject root, string partId)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(partId);

        if (WpfControlRenderer.GetPartId(root) == partId)
        {
            return root;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var match = ByPartId(VisualTreeHelper.GetChild(root, index), partId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
