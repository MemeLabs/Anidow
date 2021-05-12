using System.IO;

namespace Anidow.Model
{
    public class FolderFilesModel : ObservableObject
    {
        public FileInfo File { get; set; }
        public bool Highlight { get; set; }
    }
}