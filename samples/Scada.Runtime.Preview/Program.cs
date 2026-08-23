using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Scada.Runtime;
using Scada.Storage;

namespace Scada.Runtime.Preview;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = RuntimeHostOptions.Parse(args);
            var source = CreateSource(options);
            var project = await source.LoadAsync();
            var simulator = SimulatedVariableSource.CreateDemo(project.Variables);
            var builder = WebApplication.CreateBuilder(Array.Empty<string>());
            builder.WebHost.UseUrls(options.Urls);
            var app = RuntimeHostApplication.Build(builder, source, simulator);
            await app.StartAsync();
            Console.WriteLine($"离线运行时地址: {string.Join(", ", app.Urls)}");
            await app.WaitForShutdownAsync();
            return 0;
        }
        catch (RuntimeSourceException exception)
        {
            Console.Error.WriteLine($"[{exception.Diagnostic.Code}] {exception.Diagnostic.Message}");
            return 2;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine($"[runtime.startup] {exception.Message}");
            return 2;
        }
    }

    private static IRuntimeProjectSource CreateSource(RuntimeHostOptions options)
    {
        if (options.ProjectPath is not null)
        {
            return new JsonRuntimeProjectSource(options.ProjectPath);
        }

        if (options.DatabasePath is not null && options.ProjectId is { } projectId && options.RevisionId is { } revisionId)
        {
            return new RevisionRuntimeProjectSource(
                new RevisionStore(options.DatabasePath),
                projectId,
                revisionId);
        }

        throw new ArgumentException("没有配置有效的运行时项目源。", nameof(options));
    }
}
