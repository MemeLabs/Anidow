using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Anidow.Model
{
    public class FolderFilesModel: ObservableObject
    {
        public FileInfo File { get; set; }
        public bool Highlight { get; set; }
    }
}
