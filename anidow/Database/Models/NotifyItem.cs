using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Anidow.Enums;
using Anidow.Model;
using Stylet;

namespace Anidow.Database.Models
{
    public class NotifyItem : ObservableObject
    {
        public int Id { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public string Name { get; set; }
        public NotifySite Site { get; set; } = NotifySite.All;
        public bool MatchAll { get; set; }
        public ICollection<NotifyItemKeyword> Keywords { get; set; } = new BindableCollection<NotifyItemKeyword>();
        public ICollection<NotifyItemMatch> Matches { get; set; } = new BindableCollection<NotifyItemMatch>();

        [NotMapped]
        public string KeywordsString =>
            Keywords is not null ? string.Join(", ", Keywords?.Select(k => k.Word)) : string.Empty;
        [NotMapped] public bool Matched => Matches?.Count > 0;
        [NotMapped] public int MatchesUnseen => Matches?.Count(m => !m.Seen) ?? 0;
        [NotMapped] public bool CanCheckNow { get; set; } = true;
    }
}