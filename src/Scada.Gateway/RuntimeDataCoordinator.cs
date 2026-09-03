using Scada.Runtime;

namespace Scada.Gateway;

public sealed class RuntimeDataCoordinator : IAsyncDisposable
{
    private readonly IRuntimeDataProvider _provider;
    private readonly RuntimeSnapshotCache _cache;
    private bool _started;
    private bool _disposed;

    public RuntimeDataCoordinator(
        IRuntimeDataProvider provider,
        RuntimeSnapshotCache cache)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(cache);
        _provider = provider;
        _cache = cache;
        _provider.Updated += OnProviderUpdated;
        _cache.SetStatus(provider.Status);
    }

    public RuntimeSourceStatus Status => _cache.Status;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_started)
        {
            return;
        }

        try
        {
            await _provider.StartAsync(cancellationToken).ConfigureAwait(false);
            _cache.SetStatus(_provider.Status);
            _started = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            SetDisconnected(exception);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!_started)
        {
            _cache.SetStatus(_provider.Status);
            return;
        }

        try
        {
            await _provider.StopAsync(cancellationToken).ConfigureAwait(false);
            _cache.SetStatus(_provider.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            SetDisconnected(exception);
        }
        finally
        {
            _started = false;
        }
    }

    public RuntimeSnapshot GetFullSnapshot(
        IEnumerable<string> keys,
        DateTimeOffset? now = null) =>
        _cache.GetFullSnapshot(keys, (now ?? DateTimeOffset.UtcNow).ToUniversalTime());

    public RuntimeSnapshot GetIncrementalSnapshot(
        IEnumerable<string> keys,
        long afterSequence,
        DateTimeOffset? now = null) =>
        _cache.GetIncrementalSnapshot(
            keys,
            afterSequence,
            (now ?? DateTimeOffset.UtcNow).ToUniversalTime());

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _provider.Updated -= OnProviderUpdated;
        try
        {
            await _provider.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // Disposal must not take down the host.
        }
    }

    private void OnProviderUpdated(object? sender, RuntimeVariableUpdate update)
    {
        if (!_disposed && _cache.Apply(update))
        {
            _cache.SetStatus(_provider.Status);
        }
    }

    private void SetDisconnected(Exception exception)
    {
        _cache.SetStatus(new RuntimeSourceStatus(
            RuntimeSourceState.Disconnected,
            _provider.Status.Source,
            DateTimeOffset.UtcNow,
            _provider.Status.LastSuccessfulRead,
            [RuntimeDiagnostics.Create(
                RuntimeDiagnostics.SourceDisconnected,
                "数据源已断开，实时数据暂不可用。",
                severity: RuntimeDiagnosticSeverity.Error)]));
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
