using Json.Schema;
using Scada.Core;
using Scada.Scene;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Scada.Storage;

public sealed class JsonProjectSerializer
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly Lazy<JsonSchema> ProjectSchema = new(LoadProjectSchema);

    #pragma warning disable CA1822 // The instance API is intentional for dependency injection and future serializer options.
    public string Serialize(ProjectDocument project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var root = new JsonObject
        {
            ["schemaVersion"] = project.SchemaVersion,
            ["projectId"] = project.ProjectId.ToString(),
            ["name"] = project.Name,
            ["createdAt"] = project.CreatedAt.ToString("O"),
            ["updatedAt"] = project.UpdatedAt.ToString("O"),
            ["status"] = ToWire(project.Status),
            ["screens"] = new JsonArray(project.Screens.Select(SerializeScreen).ToArray()),
            ["variables"] = new JsonArray(project.Variables.Select(SerializeVariable).ToArray())
        };

        return root.ToJsonString(_jsonOptions);
    }

    public ProjectDocument Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var sourceVersion = ReadSchemaVersion(json);
        var upgraded = SchemaMigrations.UpgradeToCurrent(json, sourceVersion);
        var errors = ValidateCurrent(upgraded);
        if (errors.Count > 0)
        {
            throw new JsonException(string.Join(Environment.NewLine, errors.Select(error => error.Message)));
        }

        using var document = JsonDocument.Parse(upgraded);
        return DeserializeProject(document.RootElement);
    }

    public IReadOnlyList<ProjectValidationError> Validate(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [new ProjectValidationError("$", "empty", "Project JSON must not be blank.")];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return ValidateElement(document.RootElement);
        }
        catch (JsonException exception)
        {
            return [new ProjectValidationError("$", "json", exception.Message)];
        }
    }

    private IReadOnlyList<ProjectValidationError> ValidateCurrent(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ValidateElement(document.RootElement);
    }

    private IReadOnlyList<ProjectValidationError> ValidateElement(JsonElement element)
    {
        var result = ProjectSchema.Value.Evaluate(element, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        });

        if (result.IsValid)
        {
            return [];
        }

        return [new ProjectValidationError("$", "schema", result.ToString() ?? "Project JSON failed schema validation.")];
    }

    private static int ReadSchemaVersion(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("schemaVersion", out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out var version))
        {
            throw new JsonException("Project JSON must contain an integer schemaVersion.");
        }

        return version;
    }

    private static JsonObject SerializeVariable(VariableDefinition variable)
    {
        var result = new JsonObject
        {
            ["key"] = variable.Key,
            ["dataType"] = ToWire(variable.DataType),
            ["direction"] = ToWire(variable.Direction)
        };
        if (variable.Unit is not null)
        {
            result["unit"] = variable.Unit;
        }

        if (variable.Minimum is not null)
        {
            result["minimum"] = variable.Minimum;
        }

        if (variable.Maximum is not null)
        {
            result["maximum"] = variable.Maximum;
        }

        return result;
    }

    private static JsonObject SerializeScreen(ScreenDocument screen) => new()
    {
        ["name"] = screen.Name,
        ["objects"] = new JsonArray(screen.Objects.Select(SerializeObject).ToArray())
    };

    private static JsonObject SerializeObject(SceneObject sceneObject)
    {
        var result = new JsonObject
        {
            ["$type"] = sceneObject switch
            {
                ControlObject => "control",
                PipeObject => "pipe",
                TextObject => "text",
                _ => throw new NotSupportedException($"Unsupported scene object type {sceneObject.GetType().Name}.")
            },
            ["id"] = sceneObject.Id.ToString(),
            ["type"] = sceneObject.Type,
            ["bounds"] = SerializeBounds(sceneObject.Bounds),
            ["rotation"] = sceneObject.Rotation,
            ["zIndex"] = sceneObject.ZIndex,
            ["isVisible"] = sceneObject.IsVisible,
            ["properties"] = SerializeStringMap(sceneObject.Properties),
            ["bindings"] = SerializeBindings(sceneObject.Bindings),
            ["interactions"] = SerializeInteractions(sceneObject.Interactions)
        };

        switch (sceneObject)
        {
            case PipeObject pipe:
                result["start"] = SerializePoint(pipe.Start);
                result["end"] = SerializePoint(pipe.End);
                result["bends"] = new JsonArray(pipe.Bends.Select(SerializePoint).ToArray());
                break;
            case TextObject text:
                result["text"] = text.Text;
                break;
        }

        return result;
    }

    private static JsonObject SerializePoint(PointD point) => new()
    {
        ["x"] = point.X,
        ["y"] = point.Y
    };

    private static JsonObject SerializeBounds(RectD bounds) => new()
    {
        ["x"] = bounds.X,
        ["y"] = bounds.Y,
        ["width"] = bounds.Width,
        ["height"] = bounds.Height
    };

    private static JsonObject SerializeStringMap(IReadOnlyDictionary<string, string> values)
    {
        var result = new JsonObject();
        foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static JsonObject SerializeBindings(IReadOnlyDictionary<string, BindingDefinition> values)
    {
        var result = new JsonObject();
        foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            result[pair.Key] = new JsonObject
            {
                ["variableKey"] = pair.Value.VariableKey,
                ["targetProperty"] = pair.Value.TargetProperty
            };
        }

        return result;
    }

    private static JsonObject SerializeInteractions(IReadOnlyDictionary<string, InteractionDefinition> values)
    {
        var result = new JsonObject();
        foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            result[pair.Key] = new JsonObject
            {
                ["eventName"] = pair.Value.EventName,
                ["actionName"] = pair.Value.ActionName
            };
        }

        return result;
    }

    private static ProjectDocument DeserializeProject(JsonElement root)
    {
        var variables = root.GetProperty("variables").EnumerateArray().Select(DeserializeVariable).ToArray();
        var screens = root.GetProperty("screens").EnumerateArray().Select(DeserializeScreen).ToArray();
        return ProjectDocument.FromStorage(
            Guid.Parse(root.GetProperty("projectId").GetString()!),
            root.GetProperty("schemaVersion").GetInt32(),
            root.GetProperty("name").GetString()!,
            root.GetProperty("createdAt").GetDateTimeOffset(),
            root.GetProperty("updatedAt").GetDateTimeOffset(),
            FromWireStatus(root.GetProperty("status").GetString()!),
            variables,
            screens);
    }

    private static VariableDefinition DeserializeVariable(JsonElement element) =>
        new(
            element.GetProperty("key").GetString()!,
            FromWireDataType(element.GetProperty("dataType").GetString()!),
            FromWireDirection(element.GetProperty("direction").GetString()!),
            element.TryGetProperty("unit", out var unit) && unit.ValueKind != JsonValueKind.Null ? unit.GetString() : null,
            ReadNullableDouble(element, "minimum"),
            ReadNullableDouble(element, "maximum"));

    private static ScreenDocument DeserializeScreen(JsonElement element) =>
        ScreenDocument.Create(
            element.GetProperty("name").GetString()!,
            element.GetProperty("objects").EnumerateArray().Select(DeserializeObject));

    private static SceneObject DeserializeObject(JsonElement element)
    {
        var id = Guid.Parse(element.GetProperty("id").GetString()!);
        var bounds = DeserializeBounds(element.GetProperty("bounds"));
        var properties = DeserializeStringMap(element.GetProperty("properties"));
        var bindings = DeserializeBindings(element.GetProperty("bindings"));
        var interactions = DeserializeInteractions(element.GetProperty("interactions"));
        var type = element.GetProperty("type").GetString()!;
        var common = element.GetProperty("$type").GetString() switch
        {
            "control" => (SceneObject)ControlObject.Create(type, bounds, id),
            "pipe" => DeserializePipe(element, bounds, id),
            "text" => TextObject.Create(element.GetProperty("text").GetString()!, bounds, id),
            var discriminator => throw new JsonException($"Unsupported scene object discriminator '{discriminator}'.")
        };

        return common with
        {
            Rotation = element.GetProperty("rotation").GetDouble(),
            ZIndex = element.GetProperty("zIndex").GetInt32(),
            IsVisible = element.GetProperty("isVisible").GetBoolean(),
            Properties = properties,
            Bindings = bindings,
            Interactions = interactions
        };
    }

    private static PipeObject DeserializePipe(JsonElement element, RectD bounds, Guid id) =>
        PipeObject.Create(
            DeserializePoint(element.GetProperty("start")),
            DeserializePoint(element.GetProperty("end")),
            element.GetProperty("bends").EnumerateArray().Select(DeserializePoint),
            id) with
        { Bounds = bounds };

    private static PointD DeserializePoint(JsonElement element) =>
        new(element.GetProperty("x").GetDouble(), element.GetProperty("y").GetDouble());

    private static RectD DeserializeBounds(JsonElement element) =>
        new(
            element.GetProperty("x").GetDouble(),
            element.GetProperty("y").GetDouble(),
            element.GetProperty("width").GetDouble(),
            element.GetProperty("height").GetDouble());

    private static Dictionary<string, string> DeserializeStringMap(JsonElement element) =>
        element.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString()!, StringComparer.Ordinal);

    private static Dictionary<string, BindingDefinition> DeserializeBindings(JsonElement element) =>
        element.EnumerateObject().ToDictionary(
            property => property.Name,
            property => new BindingDefinition(
                property.Value.GetProperty("variableKey").GetString()!,
                property.Value.GetProperty("targetProperty").GetString()!),
            StringComparer.Ordinal);

    private static Dictionary<string, InteractionDefinition> DeserializeInteractions(JsonElement element) =>
        element.EnumerateObject().ToDictionary(
            property => property.Name,
            property => new InteractionDefinition(
                property.Value.GetProperty("eventName").GetString()!,
                property.Value.GetProperty("actionName").GetString()!),
            StringComparer.Ordinal);

    private static double? ReadNullableDouble(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetDouble()
            : null;

    private static JsonSchema LoadProjectSchema()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("project-v1.schema.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded project schema resource was not found.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    #pragma warning restore CA1822

    private static string ToWire(ProjectStatus status) => status switch
    {
        ProjectStatus.Draft => "draft",
        ProjectStatus.Published => "published",
        ProjectStatus.Archived => "archived",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static string ToWire(VariableDataType dataType) => dataType switch
    {
        VariableDataType.Bool => "bool",
        VariableDataType.Int32 => "int32",
        VariableDataType.UInt32 => "uint32",
        VariableDataType.Float64 => "float64",
        VariableDataType.String => "string",
        _ => throw new ArgumentOutOfRangeException(nameof(dataType))
    };

    private static string ToWire(VariableDirection direction) => direction switch
    {
        VariableDirection.Feedback => "feedback",
        VariableDirection.Command => "command",
        VariableDirection.Parameter => "parameter",
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    private static ProjectStatus FromWireStatus(string value) => value switch
    {
        "draft" => ProjectStatus.Draft,
        "published" => ProjectStatus.Published,
        "archived" => ProjectStatus.Archived,
        _ => throw new JsonException($"Unsupported project status '{value}'.")
    };

    private static VariableDataType FromWireDataType(string value) => value switch
    {
        "bool" => VariableDataType.Bool,
        "int32" => VariableDataType.Int32,
        "uint32" => VariableDataType.UInt32,
        "float64" => VariableDataType.Float64,
        "string" => VariableDataType.String,
        _ => throw new JsonException($"Unsupported variable data type '{value}'.")
    };

    private static VariableDirection FromWireDirection(string value) => value switch
    {
        "feedback" => VariableDirection.Feedback,
        "command" => VariableDirection.Command,
        "parameter" => VariableDirection.Parameter,
        _ => throw new JsonException($"Unsupported variable direction '{value}'.")
    };
}
