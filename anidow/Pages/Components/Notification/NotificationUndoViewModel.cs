using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Notifications.Wpf.Core;
using Stylet;

#nullable enable

namespace Anidow.Pages.Components.Notification
{
    public class NotificationUndoViewModel : Screen, INotificationViewModel
    {
        private readonly INotificationManager _manager;

        private Guid? _notificationIdentifier;

        public string? Title { get; init; }
        public string? Message { get; init; }
        public Action? OnUndo { get; init; }

        public NotificationUndoViewModel(INotificationManager manager)
        {
            _manager = manager;
        }

        // This method is called when the notification with this view/view model is
        // shown. It can be used to receive the identifier of the notification
        public void SetNotificationIdentifier(Guid identifier)
        {
            _notificationIdentifier = identifier;
        }

        public void Undo()
        {
            OnUndo?.Invoke();
        }
    }
}
