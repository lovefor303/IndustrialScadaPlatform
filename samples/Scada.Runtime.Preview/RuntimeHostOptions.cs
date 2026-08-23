namespace Scada.Runtime.Preview;

public sealed record RuntimeHostOptions(
    string? ProjectPath,
    string? DatabasePath,
    Guid? ProjectId,
    Guid? RevisionId,
    string? AuthDatabasePath,
    string Urls,
    bool AllowNonLoopback = false)
{
    public static RuntimeHostOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string? projectPath = null;
        string? databasePath = null;
        Guid? projectId = null;
        Guid? revisionId = null;
        string? authDatabasePath = null;
        var urls = "http://127.0.0.1:0";
        var allowNonLoopback = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--project":
                    projectPath = ReadValue(args, ref index, argument);
                    break;
                case "--db":
                    databasePath = ReadValue(args, ref index, argument);
                    break;
                case "--project-id":
                    projectId = ParseGuid(ReadValue(args, ref index, argument), argument);
                    break;
                case "--revision-id":
                    revisionId = ParseGuid(ReadValue(args, ref index, argument), argument);
                    break;
                case "--urls":
                    urls = ReadValue(args, ref index, argument);
                    break;
                case "--auth-db":
                    authDatabasePath = ReadValue(args, ref index, argument);
                    break;
                case "--allow-non-loopback":
                    allowNonLoopback = true;
                    break;
                default:
                    throw new ArgumentException($"不支持的运行时参数“{argument}”。", nameof(args));
            }
        }

        var jsonSelected = projectPath is not null;
        var revisionSelected = databasePath is not null || projectId is not null || revisionId is not null;
        if (jsonSelected == revisionSelected)
        {
            throw new ArgumentException("必须且只能选择 --project 或 --db/--project-id/--revision-id。", nameof(args));
        }

        if (revisionSelected && (databasePath is null || projectId is null || revisionId is null))
        {
            throw new ArgumentException("RevisionStore 运行时必须同时提供 --db、--project-id 和 --revision-id。", nameof(args));
        }

        if (!allowNonLoopback && !IsLoopbackUrl(urls))
        {
            throw new ArgumentException("默认只允许回环地址；需要外部监听时请显式提供 --allow-non-loopback。", nameof(args));
        }

        return new RuntimeHostOptions(projectPath, databasePath, projectId, revisionId, authDatabasePath, urls, allowNonLoopback);
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
        {
            throw new ArgumentException($"参数“{option}”缺少值。", nameof(args));
        }

        index++;
        return args[index];
    }

    private static Guid ParseGuid(string value, string option) =>
        Guid.TryParse(value, out var result) && result != Guid.Empty
            ? result
            : throw new ArgumentException($"参数“{option}”不是有效的 GUID。", nameof(value));

    private static bool IsLoopbackUrl(string urls)
    {
        foreach (var url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || uri.Host is not ("localhost" or "127.0.0.1" or "::1"))
            {
                return false;
            }
        }

        return true;
    }
}
