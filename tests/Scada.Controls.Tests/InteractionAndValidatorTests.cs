using Scada.Controls;
using Scada.Core;
using Scada.Scene;
using Xunit;

namespace Scada.Controls.Tests;

public sealed class InteractionAndValidatorTests
{
    [Fact]
    public void EditorLabelsAreChineseButIdsAreStable()
    {
        Assert.Equal("左键按下", InteractionCatalog.Event("pointer.left.press").ChineseLabel);
        Assert.Equal("写入命令", InteractionCatalog.Action("command.write").ChineseLabel);
    }

    [Fact]
    public void NumericLevelRoleRejectsBoolFeedback()
    {
        var instance = ControlObject.Create(ControlTypeIds.LevelBar, new RectD(0, 0, 40, 120)) with
        {
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["ProcessValue"] = new("Tank.Level", "ProcessValue")
            }
        };
        var errors = ControlValidator.Validate(
            instance,
            ControlCatalog.CreateDefault().Get(ControlTypeIds.LevelBar, 1),
            [VariableDefinition.Bool("Tank.Level", VariableDirection.Feedback)],
            new HashSet<string>());

        Assert.Contains(errors, error => error.Code == "control.binding.type" && error.Path.Contains("ProcessValue", StringComparison.Ordinal));
    }

    [Fact]
    public void CommandRoleRejectsFeedbackDirection()
    {
        var instance = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(0, 0, 120, 72)) with
        {
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["StartCommand"] = new("Pump.Start", "StartCommand")
            }
        };
        var errors = ControlValidator.Validate(
            instance,
            ControlCatalog.CreateDefault().Get(ControlTypeIds.CentrifugalPump, 1),
            [VariableDefinition.Bool("Pump.Start", VariableDirection.Feedback)],
            new HashSet<string>());

        Assert.Contains(errors, error => error.Code == "control.binding.direction");
    }

    [Fact]
    public void MissingRequiredBindingIsReported()
    {
        var instance = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(0, 0, 120, 72));
        var errors = ControlValidator.Validate(
            instance,
            ControlCatalog.CreateDefault().Get(ControlTypeIds.CentrifugalPump, 1),
            [],
            new HashSet<string>());

        Assert.Contains(errors, error => error.Code == "control.binding.required");
    }

    [Fact]
    public void UnavailableLaterPhaseActionIsDiagnosed()
    {
        var instance = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(0, 0, 120, 72)) with
        {
            Interactions = new Dictionary<string, InteractionDefinition>
            {
                ["double"] = new("pointer.double-click", "trend.open")
            }
        };
        var errors = ControlValidator.Validate(
            instance,
            ControlCatalog.CreateDefault().Get(ControlTypeIds.CentrifugalPump, 1),
            [],
            new HashSet<string>());

        Assert.Contains(errors, error => error.Code == "control.action.capability-unavailable");
    }
}
