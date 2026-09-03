using System.Windows;
using System.IO;

namespace Scada.Controls.Preview.Wpf;

internal static class Program
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        var application = new Application();
        if (TryGetReviewDirectory(arguments, out var reviewDirectory))
        {
            ReviewArtifactWriter.Write(reviewDirectory);
            application.Shutdown();
            return 0;
        }

        application.Run(new MainWindow());
        return 0;
    }

    private static bool TryGetReviewDirectory(string[] arguments, out string directory)
    {
        directory = string.Empty;
        if (arguments.Length != 2 || !string.Equals(arguments[0], "--render-review", StringComparison.Ordinal))
        {
            return false;
        }

        directory = arguments[1];
        if (!Path.IsPathFullyQualified(directory))
        {
            throw new ArgumentException("The --render-review directory must be an absolute path.", nameof(arguments));
        }

        return true;
    }
}
