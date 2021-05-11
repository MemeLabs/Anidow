using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Anidow.Annotations;
using Anidow.Enums;
using Anidow.Model;
using Anidow.Utils;
using Humanizer;

namespace Anidow.Database.Models
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class Anime : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Site Site { get; set; }
        public string Folder { get; set; }
        public string Resolution { get; set; }
        public DateTime Released { get; set; }
        public string Cover { get; set; }
        public virtual Cover CoverData { get; set; }
        public string GroupId { get; set; }
        public string GroupUrl { get; set; }
        public string Group { get; set; }
        public AnimeStatus Status { get; set; }

        [NotMapped] public string ReleasedString => Released.Humanize();
        [NotMapped] public bool IsAiring => Status == AnimeStatus.Watching;
        [NotMapped] public bool IsFinished => Status == AnimeStatus.Finished;
        [NotMapped] public int Episodes { get; set; }
        [NotMapped] public bool TrackedViewSelected { get; set; }
        [NotMapped] public string Notification { get; set; }
    }
}