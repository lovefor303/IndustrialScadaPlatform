using System.Text.Json;
using System.Text.Json.Nodes;

namespace Scada.Storage;

public sealed class UnsupportedSchemaVersionException : InvalidOperationException
{
    public UnsupportedSchemaVersionException(int version)
        : base($"Project schema version {version} is not supported. Current version is {SchemaMigrations.CurrentVersion}.")
    {
        Version = version;
    }

    public int Version { get; }
}

public static class SchemaMigrations
{
    public const int CurrentVersion = 1;

    public static string UpgradeToCurrent(string json, int sourceVersion)
    {
        if (sourceVersion > CurrentVersion)
        {
            throw new UnsupportedSchemaVersionException(sourceVersion);
        }

        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new JsonException("Project JSON must contain an object.");

        if (sourceVersion == 0)
        {
            root["schemaVersion"] = CurrentVersion;
            root["status"] ??= "draft";
            root["screens"] ??= new JsonArray();
            root["variables"] ??= new JsonArray();
        }

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
