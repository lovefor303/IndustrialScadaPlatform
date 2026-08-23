using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Scada.Controls;
using Scada.Core;
using Scada.Runtime.Preview;
using Scada.Scene;
using Xunit;

namespace Scada.Runtime.Tests;

public sealed class RuntimeHostTests
{
    [Fact]
    public async Task ReadOnlyRuntimeRoutesReturnProjectHealthAndScreen()
    {
        await using var host = await StartAsync(PublishedProject());
        using var client = host.GetTestClient();

        var health = await client.GetAsync("/api/runtime/health");
        var project = await client.GetAsync("/api/runtime/project");
        var screen = await client.GetAsync("/api/runtime/screens/Main");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, project.StatusCode);
        Assert.Equal(HttpStatusCode.OK, screen.StatusCode);
        Assert.Contains("readOnly", await health.Content.ReadAsStringAsync());
        Assert.Contains("Runtime Host Test", await project.Content.ReadAsStringAsync());
        Assert.Contains("published", await project.Content.ReadAsStringAsync());
        Assert.Contains("Main", await screen.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MissingScreenReturnsStableNotFoundDiagnostic()
    {
        await using var host = await StartAsync(PublishedProject());
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/api/runtime/screens/Missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(RuntimeDiagnostics.ScreenNotFound, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DraftProjectReturnsBadRequestAndNoCommandRouteExists()
    {
        await using var host = await StartAsync(PublishedProject(ProjectStatus.Draft));
        using var client = host.GetTestClient();

        var project = await client.GetAsync("/api/runtime/project");
        var command = await client.PostAsJsonAsync("/api/runtime/commands", new { key = "Pump.Start", value = true });

        Assert.Equal(HttpStatusCode.BadRequest, project.StatusCode);
        Assert.Contains(RuntimeDiagnostics.ProjectNotPublished, await project.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, command.StatusCode);
    }

    private static async Task<WebApplication> StartAsync(ProjectDocument project)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var source = new InMemoryProjectSource(project);
        var simulator = SimulatedVariableSource.CreateDemo(project.Variables);
        var app = RuntimeHostApplication.Build(builder, source, simulator);
        await app.StartAsync();
        return app;
    }

    private static ProjectDocument PublishedProject(ProjectStatus status = ProjectStatus.Published)
    {
        var screen = ScreenDocument.Create(
            "Main",
            new[] { ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(10, 10, 120, 72)) });
        return ProjectDocument.FromStorage(
            Guid.NewGuid(),
            ProjectFormat.CurrentVersion,
            "Runtime Host Test",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            status,
            screens: new[] { screen });
    }

    private sealed class InMemoryProjectSource(ProjectDocument project) : IRuntimeProjectSource
    {
        public Task<ProjectDocument> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(project);
    }
}
