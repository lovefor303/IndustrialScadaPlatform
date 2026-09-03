using Scada.Controls.Geometry;
using Scada.Controls.Rendering;
using Scada.Core;
using Scada.Scene;

namespace Scada.Controls.SampleProject;

/// <summary>
/// Supplies a stable, PLC-independent review scene for renderer and control verification.
/// It intentionally describes generic equipment only, not a verified customer process topology.
/// </summary>
public static class SampleProjectFactory
{
    public const string GenericProcessScreenName = "generic-process-review";
    public const string DenseLayoutScreenName = "dense-layout-validation";
    public static readonly Guid GenericProcessCanvasId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public static ProjectDocument Create() => ProjectDocument.FromStorage(
        Guid.Parse("10000000-0000-0000-0000-000000000099"),
        ProjectFormat.CurrentVersion,
        "通用工业控件离线样板",
        SampleScenarios.ReviewTimestamp,
        SampleScenarios.ReviewTimestamp,
        ProjectStatus.Draft,
        Variables(),
        [CreateGenericProcessScreen(), CreateDenseLayoutScreen()]);

    public static IReadOnlyList<SampleRenderableControl> BuildRenderables(
        string scenarioName,
        string screenName = GenericProcessScreenName,
        bool reducedMotion = false)
    {
        var scenario = SampleScenarios.Get(scenarioName);
        var screen = Create().Screens.SingleOrDefault(item => item.Name == screenName)
            ?? throw new KeyNotFoundException($"Sample screen '{screenName}' was not found.");

        return screen.Objects
            .Where(sceneObject => ControlTypeIds.All.Contains(sceneObject.Type, StringComparer.Ordinal))
            .OrderBy(control => control.ZIndex)
            .ThenBy(control => control.Id)
            .Select(control => new SampleRenderableControl(
                control,
                ControlGeometryFactory.Build(control.Type, scenario.CreateRenderContext(control, reducedMotion))))
            .ToArray();
    }

    private static IReadOnlyList<VariableDefinition> Variables() =>
    [
        VariableDefinition.Bool("sample.sourceValve.openCommand", VariableDirection.Command),
        VariableDefinition.Bool("sample.sourceValve.closeCommand", VariableDirection.Command),
        VariableDefinition.Bool("sample.sourceValve.openFeedback", VariableDirection.Feedback),
        VariableDefinition.Bool("sample.sourceValve.closedFeedback", VariableDirection.Feedback),
        VariableDefinition.Bool("sample.sourceValve.faultFeedback", VariableDirection.Feedback),
        VariableDefinition.Number("sample.sourceValve.position", VariableDataType.Float64, VariableDirection.Feedback, "%", 0, 100),
        VariableDefinition.Bool("sample.transferPump.startCommand", VariableDirection.Command),
        VariableDefinition.Bool("sample.transferPump.runFeedback", VariableDirection.Feedback),
        VariableDefinition.Bool("sample.transferPump.stopFeedback", VariableDirection.Feedback),
        VariableDefinition.Bool("sample.transferPump.faultFeedback", VariableDirection.Feedback),
        VariableDefinition.Bool("sample.receivingAgitator.runFeedback", VariableDirection.Feedback),
        VariableDefinition.Bool("sample.receivingAgitator.faultFeedback", VariableDirection.Feedback),
        VariableDefinition.Bool("sample.filter.faultFeedback", VariableDirection.Feedback),
        VariableDefinition.Number("sample.filter.differentialPressure", VariableDataType.Float64, VariableDirection.Feedback, "bar", 0, 10),
        VariableDefinition.Number("sample.source.level", VariableDataType.Float64, VariableDirection.Feedback, "%", 0, 100),
        VariableDefinition.Number("sample.source.temperature", VariableDataType.Float64, VariableDirection.Feedback, "°C", -20, 180),
        VariableDefinition.Number("sample.receiving.level", VariableDataType.Float64, VariableDirection.Feedback, "%", 0, 100),
        VariableDefinition.Number("sample.receiving.temperature", VariableDataType.Float64, VariableDirection.Feedback, "°C", -20, 180),
        VariableDefinition.Number("sample.measurement.numeric", VariableDataType.Float64, VariableDirection.Feedback, "kg", 0, 1000),
        VariableDefinition.Number("sample.measurement.level", VariableDataType.Float64, VariableDirection.Feedback, "%", 0, 100),
        VariableDefinition.Number("sample.measurement.temperature", VariableDataType.Float64, VariableDirection.Feedback, "°C", -20, 180),
        VariableDefinition.Number("sample.measurement.pressure", VariableDataType.Float64, VariableDirection.Feedback, "bar", 0, 10),
        VariableDefinition.Number("sample.measurement.flow", VariableDataType.Float64, VariableDirection.Feedback, "L/min", 0, 200),
        VariableDefinition.Bool("sample.operator.startCommand", VariableDirection.Command)
    ];

    private static ScreenDocument CreateGenericProcessScreen()
    {
        var objects = new List<SceneObject>
        {
            TextObject.Create(string.Empty, new RectD(0, 0, 1920, 1080), GenericProcessCanvasId) with { IsVisible = false },
            Control("10000000-0000-0000-0000-000000000010", ControlTypeIds.Vessel, 220, 260, 180, 240, "源罐", VesselBindings("sample.source"), 10),
            Control("10000000-0000-0000-0000-000000000011", ControlTypeIds.AutomatedValve, 475, 390, 100, 80, "源罐出口阀", ValveBindings("sample.sourceValve"), 20),
            Control("10000000-0000-0000-0000-000000000012", ControlTypeIds.CentrifugalPump, 665, 405, 120, 72, "输送泵", PumpBindings("sample.transferPump"), 20),
            Control("10000000-0000-0000-0000-000000000013", ControlTypeIds.Filter, 905, 300, 100, 160, "过滤器", FilterBindings(), 20),
            Control("10000000-0000-0000-0000-000000000014", ControlTypeIds.Vessel, 1190, 260, 180, 240, "接收罐", VesselBindings("sample.receiving"), 10),
            Control("10000000-0000-0000-0000-000000000015", ControlTypeIds.Agitator, 1230, 395, 100, 160, "搅拌器", AgitatorBindings(), 30),
            Pipe("10000000-0000-0000-0000-000000000016", ControlTypeIds.StraightPipe, new(575, 435), new(665, 435)),
            Pipe("10000000-0000-0000-0000-000000000017", ControlTypeIds.PipeElbow, new(790, 435), new(850, 435), new PointD(820, 435), new PointD(820, 405)),
            Pipe("10000000-0000-0000-0000-000000000018", ControlTypeIds.PipeTee, new(1005, 400), new(1120, 435), new PointD(1040, 435)),
            Control("10000000-0000-0000-0000-000000000019", ControlTypeIds.NumericDisplay, 155, 650, 120, 48, "重量", OneBinding("ProcessValue", "sample.measurement.numeric"), 20),
            Control("10000000-0000-0000-0000-000000000020", ControlTypeIds.LevelBar, 350, 650, 120, 48, "液位", OneBinding("ProcessValue", "sample.measurement.level"), 20),
            Control("10000000-0000-0000-0000-000000000021", ControlTypeIds.TemperatureIndicator, 545, 650, 120, 48, "温度", OneBinding("ProcessValue", "sample.measurement.temperature"), 20),
            Control("10000000-0000-0000-0000-000000000022", ControlTypeIds.PressureIndicator, 740, 650, 120, 48, "压力", OneBinding("ProcessValue", "sample.measurement.pressure"), 20),
            Control("10000000-0000-0000-0000-000000000023", ControlTypeIds.FlowIndicator, 935, 650, 120, 48, "流量", OneBinding("ProcessValue", "sample.measurement.flow"), 20),
            Control("10000000-0000-0000-0000-000000000024", ControlTypeIds.CommandButton, 1180, 650, 120, 40, "启动输送", OneBinding("Command", "sample.operator.startCommand"), 20)
        };

        return ScreenDocument.Create(GenericProcessScreenName, objects);
    }

    private static ScreenDocument CreateDenseLayoutScreen()
    {
        var objects = new List<SceneObject>
        {
            TextObject.Create("布局验证：通用三罐式密集排布，非已核实工艺拓扑", new RectD(40, 30, 900, 30), Guid.Parse("10000000-0000-0000-0000-000000000101"))
        };

        for (var index = 0; index < 3; index++)
        {
            var prefix = index == 0 ? "sample.source" : "sample.receiving";
            objects.Add(Control($"10000000-0000-0000-0000-0000000001{index + 10}", ControlTypeIds.Vessel, 140 + (index * 280), 220, 150, 210, $"通用罐 {index + 1}", VesselBindings(prefix), 10));
            objects.Add(Pipe($"10000000-0000-0000-0000-0000000001{index + 20}", ControlTypeIds.StraightPipe, new(270 + (index * 280), 460), new(390 + (index * 280), 460)));
        }

        return ScreenDocument.Create(DenseLayoutScreenName, objects);
    }

    private static ControlObject Control(
        string id,
        string typeId,
        double x,
        double y,
        double width,
        double height,
        string label,
        IReadOnlyDictionary<string, BindingDefinition> bindings,
        int zIndex) =>
        ControlObject.Create(typeId, new RectD(x, y, width, height), Guid.Parse(id)) with
        {
            ZIndex = zIndex,
            Properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["Label"] = label },
            Bindings = bindings
        };

    private static PipeObject Pipe(string id, string typeId, PointD start, PointD end, params PointD[] bends) =>
        PipeObject.Create(start, end, bends, Guid.Parse(id), typeId) with
        {
            ZIndex = 5,
            Properties = new Dictionary<string, string>(StringComparer.Ordinal),
            Bindings = EmptyBindings()
        };

    private static Dictionary<string, BindingDefinition> PumpBindings(string prefix) =>
        Bindings(("StartCommand", $"{prefix}.startCommand"), ("RunFeedback", $"{prefix}.runFeedback"), ("StopFeedback", $"{prefix}.stopFeedback"), ("FaultFeedback", $"{prefix}.faultFeedback"));

    private static Dictionary<string, BindingDefinition> ValveBindings(string prefix) =>
        Bindings(("OpenCommand", $"{prefix}.openCommand"), ("CloseCommand", $"{prefix}.closeCommand"), ("OpenFeedback", $"{prefix}.openFeedback"), ("ClosedFeedback", $"{prefix}.closedFeedback"), ("FaultFeedback", $"{prefix}.faultFeedback"), ("Position", $"{prefix}.position"));

    private static Dictionary<string, BindingDefinition> VesselBindings(string prefix) =>
        Bindings(("LevelValue", $"{prefix}.level"), ("TemperatureValue", $"{prefix}.temperature"));

    private static Dictionary<string, BindingDefinition> AgitatorBindings() =>
        Bindings(("RunFeedback", "sample.receivingAgitator.runFeedback"), ("FaultFeedback", "sample.receivingAgitator.faultFeedback"));

    private static Dictionary<string, BindingDefinition> FilterBindings() =>
        Bindings(("DifferentialPressure", "sample.filter.differentialPressure"), ("FaultFeedback", "sample.filter.faultFeedback"));

    private static Dictionary<string, BindingDefinition> OneBinding(string role, string variableKey) =>
        Bindings((role, variableKey));

    private static Dictionary<string, BindingDefinition> EmptyBindings() =>
        new Dictionary<string, BindingDefinition>(StringComparer.Ordinal);

    private static Dictionary<string, BindingDefinition> Bindings(params (string Role, string VariableKey)[] items) =>
        items.ToDictionary(item => item.Role, item => new BindingDefinition(item.VariableKey, item.Role), StringComparer.Ordinal);
}

public sealed record SampleRenderableControl(SceneObject Control, ControlRenderPlan Plan);
