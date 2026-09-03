namespace Scada.Controls.Wpf.Tests;

internal static class StaThread
{
    public static Task RunAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                action();
                completion.SetResult();
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
            task.GetAwaiter().GetResult();
        }, TaskScheduler.Default);
    }
}
