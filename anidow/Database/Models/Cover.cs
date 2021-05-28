using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Anidow.Model;

namespace Anidow.Database.Models
{
    public class Cover : ObservableObject
    {
        public int Id { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public string File { get; set; }

        [NotMapped] public string FilePath => Path.Combine(Directory.GetCurrentDirectory(), File);
    }
}