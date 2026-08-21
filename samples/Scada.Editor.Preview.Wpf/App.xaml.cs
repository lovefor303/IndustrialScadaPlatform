using System.Windows;
using Scada.Controls;
using Scada.Core;
using Scada.Editor.Wpf;
using Scada.Scene;

namespace Scada.Editor.Preview.Wpf;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var level = VariableDefinition.Number(
            "Tank.Level",
            VariableDataType.Float64,
            VariableDirection.Feedback,
            "%",
            0,
            100);
        var running = VariableDefinition.Bool("Pump.Running", VariableDirection.Feedback);
        var pump = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(240, 220, 120, 72)) with
        {
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["RunFeedback"] = new(running.Key, "RunFeedback")
            }
        };
        var vessel = ControlObject.Create(ControlTypeIds.Vessel, new RectD(520, 160, 180, 240)) with
        {
            Bindings = new Dictionary<string, BindingDefinition>
            {
                ["LevelValue"] = new(level.Key, "LevelValue")
            }
        };
        var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(760, 240, 100, 80));
        var pipe = PipeObject.Create(
            new PointD(120, 256),
            new PointD(860, 280),
            new[] { new PointD(240, 256), new PointD(240, 280), new PointD(520, 280) });
        var title = TextObject.Create("离线编辑预览", new RectD(80, 70, 260, 36));
        var screen = ScreenDocument.Create("Main", new SceneObject[] { pipe, pump, vessel, valve, title });
        var session = new EditorSession(
            ProjectDocument.Create("离线编辑预览", new[] { level, running }, new[] { screen }),
            "Main");

        var window = new EditorShellWindow(EditorRole.Developer, session);
        MainWindow = window;
        window.Show();
    }
}
