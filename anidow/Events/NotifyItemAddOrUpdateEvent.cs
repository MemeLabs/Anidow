// // Created: 26-08-2021 13:48

using Anidow.Database.Models;
using Anidow.Pages;

namespace Anidow.Events
{
    public class NotifyItemAddOrUpdateEvent
    {
        public NotifyItem Item { get; init; }
        public bool IsUpdate { get; set; }
    }
}