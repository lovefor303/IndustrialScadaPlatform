namespace Scada.Phase2.Acceptance.Tests;

internal static class StaThread
{
    public static Task<T> RunAsync<T>(Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(action());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return completion.Task.ContinueWith(task =>
        {
            thread.Join();
            return task.GetAwaiter().GetResult();
        }, TaskScheduler.Default);
    }
}
