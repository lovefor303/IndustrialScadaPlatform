using System.Text.Json;
using System.Text.Json.Nodes;
using Scada.Core;

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
    public const int CurrentVersion = ProjectFormat.CurrentVersion;

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
            sourceVersion = 1;
        }

        if (sourceVersion == 1)
        {
            foreach (var screen in root["screens"]?.AsArray() ?? [])
            {
                foreach (var sceneObject in screen?["objects"]?.AsArray() ?? [])
                {
                    var objectNode = sceneObject?.AsObject();
                    if (objectNode is null)
                    {
                        continue;
                    }

                    if (string.Equals(objectNode["$type"]?.GetValue<string>(), "control", StringComparison.Ordinal))
                    {
                        objectNode["controlVersion"] ??= 1;
                    }

                    if (string.Equals(objectNode["$type"]?.GetValue<string>(), "pipe", StringComparison.Ordinal)
                        && string.Equals(objectNode["type"]?.GetValue<string>(), "pipe", StringComparison.Ordinal))
                    {
                        objectNode["type"] = "pipe.straight";
                    }

                    if (objectNode["interactions"] is JsonObject interactions)
                    {
                        foreach (var interaction in interactions)
                        {
                            if (interaction.Value is JsonObject interactionObject)
                            {
                                interactionObject["parameters"] ??= new JsonObject();
                            }
                        }
                    }
                }
            }

            root["schemaVersion"] = CurrentVersion;
        }

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
