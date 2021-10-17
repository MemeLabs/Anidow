using System;

namespace Anidow.Database.Models;

public class NotifyItemKeyword
{
    public int Id { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;

    public string Word { get; set; }
    public bool IsRegex { get; set; }
    public bool IsCaseSensitive { get; set; }
    public bool MustMatch { get; set; }

    public int NotifyItemId { get; set; }
    public virtual NotifyItem NotifyItem { get; set; }
}