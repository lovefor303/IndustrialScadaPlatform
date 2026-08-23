using Xunit;

namespace Scada.Runtime.Tests;

public sealed class RuntimeBrowserAssetTests
{
    [Fact]
    public void BrowserShellUsesChineseReadOnlyRuntimeLabelsAndResponsiveScene()
    {
        var root = FindRepositoryRoot();
        var wwwroot = Path.Combine(root, "samples", "Scada.Runtime.Preview", "wwwroot");
        var html = File.ReadAllText(Path.Combine(wwwroot, "index.html"));
        var css = File.ReadAllText(Path.Combine(wwwroot, "app.css"));
        var script = File.ReadAllText(Path.Combine(wwwroot, "app.js"));

        Assert.Contains("运行时", html);
        Assert.Contains("只读", html);
        Assert.Contains("preserveAspectRatio", html);
        Assert.Contains("app.css", html);
        Assert.Contains("app.js", html);
        Assert.Contains("@media", css);
        Assert.Contains("screen", script);
        Assert.DoesNotContain("/api/runtime/commands", script);
        Assert.DoesNotContain("POST", script, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var index = 0; index < 10 && current is not null; index++)
        {
            if (File.Exists(Path.Combine(current.FullName, "IndustrialScadaPlatform.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("无法定位工业 SCADA 平台仓库根目录。");
    }
}
