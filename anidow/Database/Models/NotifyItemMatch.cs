using System;
using System.ComponentModel.DataAnnotations.Schema;
using Anidow.Enums;
using Anidow.Model;

namespace Anidow.Database.Models
{
    public class NotifyItemMatch : ObservableObject
    {
        public int Id { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public NotifySite Site { get; set; }
        public bool Seen { get; set; }
        public string Name { get; set; }
        public string DownloadLink { get; set; }
        public string Link { get; set; }
        public string Json { get; set; }
        public bool UserNotified { get; set; }
        public bool Downloaded { get; set; }
        public string[] Keywords { get; set; } = Array.Empty<string>();

        public int NotifyItemId { get; set; }
        public virtual NotifyItem NotifyItem { get; set; }
    }
}