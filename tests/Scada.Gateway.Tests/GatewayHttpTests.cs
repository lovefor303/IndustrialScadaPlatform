using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Scada.Controls;
using Scada.Core;
using Scada.Gateway;
using Scada.Runtime;
using Scada.Scene;
using Xunit;

namespace Scada.Gateway.Tests;

public sealed class GatewayHttpTests
{
    [Fact]
    public async Task HealthIsAvailableInExplicitAnonymousDevelopmentMode()
    {
        await using var host = await StartAsync();
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/api/runtime/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("readOnly", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProjectRequiresLoginThenSupportsInitializeLoginLogout()
    {
        await using var host = await StartAsync();
        using var anonymous = host.GetTestClient();

        var beforeLogin = await anonymous.GetAsync("/api/runtime/project");
        Assert.Equal(HttpStatusCode.Unauthorized, beforeLogin.StatusCode);

        var initialize = await anonymous.PostAsJsonAsync(
            "/api/auth/initialize",
            new { token = "setup-token", userName = "admin", password = "password-123" });
        Assert.Equal(HttpStatusCode.OK, initialize.StatusCode);

        var secondInitialize = await anonymous.PostAsJsonAsync(
            "/api/auth/initialize",
            new { token = "setup-token", userName = "second", password = "password-123" });
        Assert.Equal(HttpStatusCode.NotFound, secondInitialize.StatusCode);

        using var authenticated = host.GetTestClient();
        var login = await authenticated.PostAsJsonAsync(
            "/api/auth/login",
            new { userName = "admin", password = "password-123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        authenticated.DefaultRequestHeaders.Add(
            "Cookie",
            login.Headers.GetValues("Set-Cookie").Single().Split(';', 2)[0]);

        var project = await authenticated.GetAsync("/api/runtime/project");
        Assert.Equal(HttpStatusCode.OK, project.StatusCode);
        Assert.Contains("Gateway Test", await project.Content.ReadAsStringAsync());

        var logout = await authenticated.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var loggedOut = host.GetTestClient();
        var afterLogout = await loggedOut.GetAsync("/api/runtime/project");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task InvalidLoginReturnsUnauthorizedAndWriteRouteDoesNotExist()
    {
        await using var host = await StartAsync();
        using var client = host.GetTestClient();
        await client.PostAsJsonAsync(
            "/api/auth/initialize",
            new { token = "setup-token", userName = "admin", password = "password-123" });

        var invalid = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { userName = "admin", password = "wrong-password" });
        var write = await client.PostAsJsonAsync(
            "/api/runtime/write",
            new { key = "Pump.Start", value = true });

        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, write.StatusCode);
    }

    private static async Task<WebApplication> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var databasePath = Path.Combine(Path.GetTempPath(), $"scada-http-{Guid.NewGuid():N}.db");
        var auth = new AuthStore(databasePath);
        await auth.InitializeAsync();
        var options = new GatewayOptions(
            ProjectName: "Gateway Test",
            AnonymousDevelopment: true,
            SetupToken: "setup-token");
        var app = GatewayApplication.Build(
            builder,
            new InMemoryProjectSource(PublishedProject()),
            new SimulatedVariableSource(PublishedProject().Variables),
            auth,
            options);
        app.Lifetime.ApplicationStopping.Register(() =>
        {
            File.Delete(databasePath);
            File.Delete(databasePath + "-wal");
            File.Delete(databasePath + "-shm");
        });
        await app.StartAsync();
        return app;
    }

    private static ProjectDocument PublishedProject()
    {
        var screen = ScreenDocument.Create(
            "Main",
            new[] { ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(10, 10, 120, 72)) });
        return ProjectDocument.FromStorage(
            Guid.NewGuid(),
            ProjectFormat.CurrentVersion,
            "Gateway Test",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            ProjectStatus.Published,
            screens: new[] { screen });
    }

    private sealed class InMemoryProjectSource(ProjectDocument project) : IRuntimeProjectSource
    {
        public Task<ProjectDocument> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(project);
    }
}
