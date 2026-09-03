namespace Scada.Gateway;

public sealed record GatewayOptions(
    string ProjectName = "SCADA Runtime",
    string? ProjectPath = null,
    string? AuthDatabasePath = null,
    bool AnonymousDevelopment = false,
    string? SetupToken = null,
    TimeSpan? UncertainAfter = null,
    TimeSpan? BadAfter = null,
    int SubscriptionLimit = 256);
