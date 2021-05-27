using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anidow.Database.Models;

namespace Anidow.Events
{
    public class OpenAnimeEditEvent
    {
        public Anime Anime { get; set; }
    }
}
