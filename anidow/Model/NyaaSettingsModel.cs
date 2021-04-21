using System;
using System.Collections.Generic;
using System.Text;

namespace Anidow.Model
{
    public class NyaaSettingsModel : ObservableObject
    {
        public int HideTorrentsBelowSeeders { get; set; } = -1;
    }
}
