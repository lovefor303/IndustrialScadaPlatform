using Scada.Core;
using Scada.Runtime;
using Scada.Scene;
using Xunit;

namespace Scada.Runtime.Tests;

public sealed class Phase4M1AcceptanceTests
{
    [Fact]
    public async Task PublishedFixtureProjectsEveryObjectExactlyOnce()
    {
        var fixture = Path.Combine(FindRepositoryRoot(), "tests", "Scada.Runtime.Tests", "Fixtures", "runtime-published.json");
        var project = await new JsonRuntimeProjectSource(fixture).LoadAsync();
        var runtime = new RuntimeSceneProjector().Project(
            project,
            SimulatedVariableSource.CreateDemo(project.Variables),
            "Main",
            "desktop",
            new DateTimeOffset(2026, 8, 23, 0, 0, 0, TimeSpan.Zero));

        var sourceIds = project.Screens.Single(screen => screen.Name == "Main").Objects.Select(item => item.Id).ToArray();
        var runtimeIds = runtime.Objects.Select(item => item.Id).ToArray();

        Assert.Equal(sourceIds.OrderBy(id => id), runtimeIds.OrderBy(id => id));
        Assert.All(runtime.Objects, item => Assert.True(item.ReadOnly));
        Assert.All(runtime.Objects.Where(item => item.Type == "pipe.straight"), item =>
        {
            Assert.NotNull(item.PipeStart);
            Assert.NotNull(item.PipeEnd);
            Assert.NotNull(item.PipeBends);
        });
    }

    [Fact]
    public async Task PublishedFixtureUsesControlledUnknownQualityWithoutMutatingProject()
    {
        var fixture = Path.Combine(FindRepositoryRoot(), "tests", "Scada.Runtime.Tests", "Fixtures", "runtime-published.json");
        var project = await new JsonRuntimeProjectSource(fixture).LoadAsync();
        var before = new Scada.Storage.JsonProjectSerializer().Serialize(project);

        var runtime = new RuntimeSceneProjector().Project(
            project,
            new SimulatedVariableSource(project.Variables),
            "Main",
            "phone",
            new DateTimeOffset(2026, 8, 23, 0, 0, 0, TimeSpan.Zero));

        var after = new Scada.Storage.JsonProjectSerializer().Serialize(project);

        Assert.Equal(before, after);
        Assert.Contains(runtime.Objects, item => item.Quality == VariableQuality.Bad);
        Assert.Equal("phone", runtime.LayoutProfile);
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
