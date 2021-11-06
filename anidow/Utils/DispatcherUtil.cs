using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Anidow.Utils;

public class DispatcherUtil
{
    public static async Task DispatchAsync(Func<Task> action)
    {
        if (Application.Current is not null)
        {
            await Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);
            return;
        }

        await action();
    }

    public static async Task DispatchAsync(Action action)
    {
        if (Application.Current is not null)
        {
            await Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);
            return;
        }

        action();
    }

    public static void DispatchSync(Action action)
    {
        try
        {
            if (Application.Current is not null)
            {
                Application.Current?.Dispatcher.Invoke(DispatcherPriority.Background, action);
                return;
            }

            action();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}