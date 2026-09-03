using Scada.Controls;
using Scada.Controls.Rendering;
using Scada.Core;

namespace Scada.Controls.Preview.Wpf;

internal static class PreviewPlans
{
    public static ControlRenderContext CreateContext(ControlState state, bool reducedMotion) =>
        ControlRenderContext.ForState(state) with
        {
            ReducedMotion = reducedMotion,
            Quality = state == ControlState.Unknown ? VariableQuality.Bad : VariableQuality.Good,
            NumericValues = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["LevelValue"] = 67,
                ["ValvePosition"] = 58,
                ["ProcessValue"] = 42.5,
                ["Minimum"] = 0,
                ["Maximum"] = 100
            },
            TextValues = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Label"] = "启动设备",
                ["ActionKind"] = "write-command",
                ["Unit"] = "bar"
            }
        };
}
