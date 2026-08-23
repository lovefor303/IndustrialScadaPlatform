namespace Scada.Gateway;

public sealed record RuntimeSubscription(
    string ConnectionId,
    string UserId,
    string Screen,
    IReadOnlyList<string> Variables);

public sealed record SubscriptionResult(
    bool Accepted,
    string? Code = null,
    string? Message = null,
    RuntimeSubscription? Subscription = null);

public sealed class RuntimeSubscriptionRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, RuntimeSubscription> _subscriptions = new(StringComparer.Ordinal);
    private readonly int _maximumVariables;

    public RuntimeSubscriptionRegistry(int maximumVariables = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumVariables);

        _maximumVariables = maximumVariables;
    }

    public SubscriptionResult Subscribe(
        string connectionId,
        string userId,
        string screen,
        IReadOnlyList<string> variables,
        IReadOnlySet<string> allowedVariables)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(screen);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(allowedVariables);

        var normalized = variables
            .Where(variable => !string.IsNullOrWhiteSpace(variable))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length == 0 || normalized.Length > _maximumVariables
            || normalized.Any(variable => !allowedVariables.Contains(variable)))
        {
            return new SubscriptionResult(
                false,
                "SUBSCRIPTION_REJECTED",
                "订阅画面或变量无效，实时订阅已拒绝。");
        }

        var subscription = new RuntimeSubscription(connectionId, userId, screen, normalized);
        lock (_gate)
        {
            _subscriptions[connectionId] = subscription;
        }

        return new SubscriptionResult(true, Subscription: subscription);
    }

    public RuntimeSubscription? Get(string connectionId)
    {
        lock (_gate)
        {
            return _subscriptions.GetValueOrDefault(connectionId);
        }
    }

    public IReadOnlyList<RuntimeSubscription> GetAll()
    {
        lock (_gate)
        {
            return _subscriptions.Values.ToArray();
        }
    }

    public bool Remove(string connectionId)
    {
        lock (_gate)
        {
            return _subscriptions.Remove(connectionId);
        }
    }
}
