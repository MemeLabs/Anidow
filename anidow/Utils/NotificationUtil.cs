using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Anidow.Pages.Components.Notification;
using Notifications.Wpf.Core;

#nullable enable

namespace Anidow.Utils;

public static class NotificationUtil
{
    private const string WindowArea = "WindowArea";
    private static readonly NotificationManager NotificationManager = new();

    public static async Task ShowAsync(
        string title,
        string message,
        NotificationType type = NotificationType.Information,
        TimeSpan? duration = null,
        Action? onClick = null)
    {
        try
        {
            await NotificationManager.ShowAsync(
                new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = type,
                },
                WindowArea,
                duration,
                onClick);
        }
        catch (Exception e)
        {
            // ignore
            Debug.WriteLine(e);
        }
    }

    public static async Task ShowUndoAsync(string title, string message, Func<Task>? onUndo, Action? onClose = null,
        TimeSpan? expiration = default)
    {
        var content = new NotificationUndoViewModel(NotificationManager)
        {
            Title = title,
            Message = message,
            OnUndo = onUndo,
        };

        await NotificationManager.ShowAsync(content, WindowArea, expiration, onClose: onClose);
    }
}