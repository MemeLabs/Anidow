using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Anidow.Enums
{
    public enum AnimeStatus
    {
        [Description("Watching")]
        Watching = 1,
        Finished = 2,
        Dropped = 4,
        All = Watching | Finished | Dropped
    }
}
