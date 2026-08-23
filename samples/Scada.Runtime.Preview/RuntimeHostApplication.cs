using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Scada.Runtime;

namespace Scada.Runtime.Preview;

public static class RuntimeHostApplication
{
    public static WebApplication Build(
        WebApplicationBuilder builder,
        IRuntimeProjectSource projectSource,
        IRuntimeVariableSource variableSource,
        RuntimeSceneProjector? projector = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(projectSource);
        ArgumentNullException.ThrowIfNull(variableSource);

        builder.Services.AddSingleton(projectSource);
        builder.Services.AddSingleton(variableSource);
        builder.Services.AddSingleton(projector ?? new RuntimeSceneProjector());
        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapGet("/api/runtime/health", () => Results.Ok(new
        {
            status = "ok",
            source = "published-project",
            simulator = "deterministic-in-memory",
            readOnly = true
        }));

        app.MapGet("/api/runtime/project", async (
            IRuntimeProjectSource source,
            CancellationToken cancellationToken) =>
            await ReadProjectMetadataAsync(source, cancellationToken));

        app.MapGet("/api/runtime/screens/{screenName}", async (
            string screenName,
            IRuntimeProjectSource source,
            IRuntimeVariableSource variables,
            RuntimeSceneProjector sceneProjector,
            CancellationToken cancellationToken) =>
            await ReadScreenAsync(screenName, source, variables, sceneProjector, cancellationToken));

        return app;
    }

    private static async Task<IResult> ReadProjectMetadataAsync(
        IRuntimeProjectSource source,
        CancellationToken cancellationToken)
    {
        try
        {
            var project = await source.LoadAsync(cancellationToken);
            EnsurePublished(project);
            return Results.Ok(new RuntimeProjectMetadata(
                project.ProjectId,
                project.Name,
                project.Status,
                project.Screens.Select(screen => screen.Name).ToArray(),
                project.UpdatedAt));
        }
        catch (RuntimeSourceException exception)
        {
            return Failure(exception);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception)
        {
            return Results.Json(
                new { code = RuntimeDiagnostics.ProjectInvalid, message = "读取运行时项目失败。" },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ReadScreenAsync(
        string screenName,
        IRuntimeProjectSource source,
        IRuntimeVariableSource variables,
        RuntimeSceneProjector projector,
        CancellationToken cancellationToken)
    {
        try
        {
            var project = await source.LoadAsync(cancellationToken);
            EnsurePublished(project);
            var screen = projector.Project(project, variables, screenName, "desktop", DateTimeOffset.UtcNow);
            return Results.Ok(screen);
        }
        catch (RuntimeSourceException exception)
        {
            return Failure(exception);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception)
        {
            return Results.Json(
                new { code = RuntimeDiagnostics.ProjectInvalid, message = "读取运行时画面失败。" },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static IResult Failure(RuntimeSourceException exception)
    {
        var statusCode = exception.Diagnostic.Code == RuntimeDiagnostics.ScreenNotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return Results.Json(
            new
            {
                code = exception.Diagnostic.Code,
                message = exception.Diagnostic.Message,
                readOnly = true
            },
            statusCode: statusCode);
    }

    private static void EnsurePublished(Scada.Scene.ProjectDocument project)
    {
        if (project.Status != Scada.Core.ProjectStatus.Published)
        {
            throw new RuntimeSourceException(
                RuntimeDiagnostics.Create(RuntimeDiagnostics.ProjectNotPublished, "项目尚未发布，不能进入运行时。"));
        }

        if (project.Screens.Count == 0)
        {
            throw new RuntimeSourceException(
                RuntimeDiagnostics.Create(RuntimeDiagnostics.ProjectInvalid, "已发布项目没有可运行的画面。"));
        }
    }
}
