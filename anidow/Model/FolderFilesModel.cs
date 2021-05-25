using System;
using System.IO;
using Anidow.Utils;
using Humanizer;

namespace Anidow.Model
{
    public class FolderFilesModel : ObservableObject
    {
        public FileInfo File { get; init; }
        public bool Highlight { get; set; }

        public string SizeString
        {
            get
            {
                try
                {
                    return File.Length.Bytes().Humanize("#.##");
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        public string ModifiedLocalString => File.LastWriteTime.ToString("g");
        public bool IsDirectory => File.Attributes.HasFlag(FileAttributes.Directory);
        public bool CanOpenFile => !IsDirectory && ProcessUtil.IsAllowedFile(File);
    }
}