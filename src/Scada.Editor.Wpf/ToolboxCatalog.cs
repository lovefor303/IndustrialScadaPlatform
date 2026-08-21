using Scada.Controls;
using Scada.Scene;

namespace Scada.Editor.Wpf;

public sealed record ToolboxEntry(
    string TypeId,
    string DisplayName,
    RectD DefaultBounds,
    bool IsPipe);

public sealed class ToolboxCatalog
{
    private ToolboxCatalog(IReadOnlyList<ToolboxEntry> entries)
    {
        Entries = entries;
    }

    public IReadOnlyList<ToolboxEntry> Entries { get; }

    public static ToolboxCatalog CreateDefault()
    {
        var catalog = ControlCatalog.CreateDefault();
        var entries = ControlTypeIds.All.Select(typeId =>
        {
            var definition = catalog.Get(typeId, 1);
            return new ToolboxEntry(
                typeId,
                ChineseName(typeId),
                new RectD(0, 0, definition.DefaultSize.Width, definition.DefaultSize.Height),
                typeId.StartsWith("pipe.", StringComparison.Ordinal));
        }).ToList();
        entries.Add(new ToolboxEntry("text", "文本标签", new RectD(0, 0, 160, 32), false));
        return new ToolboxCatalog(entries);
    }

    public ToolboxEntry Get(string typeId) =>
        Entries.Single(entry => string.Equals(entry.TypeId, typeId, StringComparison.Ordinal));

    private static string ChineseName(string typeId) => typeId switch
    {
        ControlTypeIds.CentrifugalPump => "离心泵",
        ControlTypeIds.AutomatedValve => "自动阀",
        ControlTypeIds.Vessel => "容器",
        ControlTypeIds.Agitator => "搅拌器",
        ControlTypeIds.Filter => "过滤器",
        ControlTypeIds.StraightPipe => "直管",
        ControlTypeIds.PipeElbow => "弯头",
        ControlTypeIds.PipeTee => "三通",
        ControlTypeIds.NumericDisplay => "数值显示",
        ControlTypeIds.LevelBar => "液位柱",
        ControlTypeIds.TemperatureIndicator => "温度仪表",
        ControlTypeIds.PressureIndicator => "压力仪表",
        ControlTypeIds.FlowIndicator => "流量仪表",
        ControlTypeIds.CommandButton => "操作按钮",
        _ => typeId
    };
}
