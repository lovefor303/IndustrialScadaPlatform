using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Scada.Core;
using Scada.Runtime;
using Scada.Scene;

namespace Scada.Gateway;

public static class GatewayApplication
{
    private const string RuntimeViewPolicy = "Runtime.View";

    public static WebApplication Build(
        WebApplicationBuilder builder,
        IRuntimeProjectSource projectSource,
        IRuntimeDataProvider dataProvider,
        AuthStore authStore,
        GatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(projectSource);
        ArgumentNullException.ThrowIfNull(dataProvider);
        ArgumentNullException.ThrowIfNull(authStore);
        ArgumentNullException.ThrowIfNull(options);

        builder.Services.AddSingleton(projectSource);
        builder.Services.AddSingleton(dataProvider);
        builder.Services.AddSingleton(authStore);
        builder.Services.AddSingleton(options);
        builder.Services.ConfigureHttpJsonOptions(json =>
            json.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(cookie =>
            {
                cookie.Cookie.Name = "scada.gateway.session";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SameSite = SameSiteMode.Strict;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                cookie.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                cookie.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
        builder.Services.AddAuthorization(auth =>
            auth.AddPolicy(RuntimeViewPolicy, policy =>
                policy.RequireRole(
                    AuthRole.Viewer.ToString(),
                    AuthRole.Operator.ToString(),
                    AuthRole.Engineer.ToString(),
                    AuthRole.Admin.ToString())));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/api/runtime/health", (GatewayOptions gatewayOptions) => Results.Ok(new
        {
            status = "ok",
            source = "published-project",
            readOnly = true,
            anonymousDevelopment = gatewayOptions.AnonymousDevelopment
        })).AllowAnonymous();

        app.MapGet("/api/runtime/project", async (
            IRuntimeProjectSource source,
            CancellationToken cancellationToken) =>
            await ReadProjectAsync(source, cancellationToken))
            .RequireAuthorization(RuntimeViewPolicy);

        app.MapPost("/api/auth/initialize", async (
            InitializeRequest request,
            AuthStore store,
            GatewayOptions gatewayOptions,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (!IsLoopback(context) || string.IsNullOrWhiteSpace(gatewayOptions.SetupToken)
                || !string.Equals(request.Token, gatewayOptions.SetupToken, StringComparison.Ordinal))
            {
                return Results.NotFound();
            }

            if (await store.HasUsersAsync(cancellationToken).ConfigureAwait(false))
            {
                return Results.NotFound();
            }

            var user = await store.CreateFirstAdminAsync(
                request.UserName,
                request.Password,
                cancellationToken).ConfigureAwait(false);
            return Results.Ok(new { userName = user.UserName, role = user.Role.ToString() });
        }).AllowAnonymous();

        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            AuthStore store,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var user = await store.AuthenticateAsync(request.UserName, request.Password, cancellationToken)
                .ConfigureAwait(false);
            if (user is null)
            {
                return Results.Json(
                    new { code = GatewayDiagnostics.AuthRequired, message = "用户名或密码不正确。" },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString("D")),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return Results.Ok(new { userName = user.UserName, role = user.Role.ToString() });
        }).AllowAnonymous();

        app.MapPost("/api/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ReadProjectAsync(
        IRuntimeProjectSource source,
        CancellationToken cancellationToken)
    {
        try
        {
            var project = await source.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (project.Status != ProjectStatus.Published || project.Screens.Count == 0)
            {
                return Results.Json(
                    new { code = RuntimeDiagnostics.ProjectNotPublished, message = "项目尚未发布，不能进入运行时。" },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Ok(new RuntimeProjectMetadata(
                project.ProjectId,
                project.Name,
                project.Status,
                project.Screens.Select(screen => screen.Name).ToArray(),
                project.UpdatedAt));
        }
        catch (RuntimeSourceException exception)
        {
            return Results.Json(
                new { code = exception.Diagnostic.Code, message = exception.Diagnostic.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static bool IsLoopback(HttpContext context) =>
        context.Connection.RemoteIpAddress is null
        || System.Net.IPAddress.IsLoopback(context.Connection.RemoteIpAddress);

    private sealed record InitializeRequest(string Token, string UserName, string Password);
    private sealed record LoginRequest(string UserName, string Password);
}
