using Scada.Controls;
using Scada.Scene;

namespace Scada.Editor.Wpf;

public sealed class EventsPanelViewModel
{
    private readonly EditorSession _session;
    private readonly IReadOnlyList<LocalizedCapability> _events;
    private readonly IReadOnlyList<LocalizedCapability> _actions;

    public EventsPanelViewModel(EditorSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _events = InteractionCatalog.AllEvents;
        _actions = InteractionCatalog.AllActions;
    }

    public IReadOnlyList<LocalizedCapability> Events => _events;

    public IReadOnlyList<LocalizedCapability> Actions => _actions;

    public string GetEventLabel(string eventId) => _events.Single(item => item.Id == eventId).ChineseLabel;

    public string GetActionLabel(string actionId) => _actions.Single(item => item.Id == actionId).ChineseLabel;

    public bool TrySet(
        string eventId,
        string actionId,
        IReadOnlyDictionary<string, string>? parameters,
        out IReadOnlyList<string> errors)
    {
        var validation = new List<string>();
        var selected = _session.SelectedObjectIds.Count == 1
            ? _session.ActiveScreen.FindObject(_session.SelectedObjectIds.Single())
            : null;
        if (selected is null)
        {
            validation.Add("对象选择 / interactions: 请选择一个画面对象。");
        }

        try
        {
            InteractionCatalog.Event(eventId);
        }
        catch (KeyNotFoundException)
        {
            validation.Add($"对象 {selected?.Id} / interactions.{eventId}.event: 事件“{eventId}”不存在。");
        }

        LocalizedCapability? action = null;
        try
        {
            action = InteractionCatalog.Action(actionId);
        }
        catch (KeyNotFoundException)
        {
            validation.Add($"对象 {selected?.Id} / interactions.{eventId}.action: 动作“{actionId}”不存在。");
        }

        if (action is not null)
        {
            foreach (var required in action.RequiredParameters)
            {
                if (parameters is null || !parameters.ContainsKey(required))
                {
                    validation.Add($"对象 {selected?.Id} / interactions.{eventId}.parameters: 动作“{action.ChineseLabel}”缺少参数“{required}”。");
                }
            }
        }

        if (validation.Count == 0)
        {
            _session.UpdateSelectedObject(sceneObject => sceneObject with
            {
                Interactions = sceneObject.Interactions.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal)
                    .SetItem(
                        eventId,
                        new InteractionDefinition(eventId, actionId, parameters))
            });
        }

        errors = validation;
        return validation.Count == 0;
    }
}
