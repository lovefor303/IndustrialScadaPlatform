namespace Scada.Controls;

public sealed record LocalizedCapability(
    string Id,
    string ChineseLabel,
    IReadOnlySet<string> RequiredParameters);

public static class InteractionCatalog
{
    private static readonly IReadOnlyDictionary<string, LocalizedCapability> Events =
        Create(
            ("pointer.left.press", "左键按下"),
            ("pointer.left.release", "左键释放"),
            ("pointer.double-click", "双击"),
            ("pointer.right-click", "右键"),
            ("pointer.enter", "鼠标进入"),
            ("pointer.leave", "鼠标离开"),
            ("variable.changed", "变量变化"),
            ("fault.activated", "故障出现"),
            ("fault.recovered", "故障恢复"));

    private static readonly IReadOnlyDictionary<string, LocalizedCapability> Actions =
        Create(
            ("command.write", "写入命令"),
            ("command.toggle-bool", "切换布尔值"),
            ("panel.open-equipment", "打开设备面板"),
            ("trend.open", "打开趋势"),
            ("alarm.acknowledge", "确认报警"),
            ("condition.reset", "复位"),
            ("screen.navigate", "导航到画面"),
            ("message.show", "显示提示"));

    public static LocalizedCapability Event(string id) => Get(Events, id, "event");

    public static LocalizedCapability Action(string id) => Get(Actions, id, "action");

    private static LocalizedCapability Get(
        IReadOnlyDictionary<string, LocalizedCapability> values,
        string id,
        string kind) =>
        values.TryGetValue(id, out var capability)
            ? capability
            : throw new KeyNotFoundException($"Unknown {kind} capability '{id}'.");

    private static Dictionary<string, LocalizedCapability> Create(params (string Id, string Label)[] values) =>
        values.ToDictionary(
            value => value.Id,
            value => new LocalizedCapability(value.Id, value.Label, new HashSet<string>(StringComparer.Ordinal)),
            StringComparer.Ordinal);
}
