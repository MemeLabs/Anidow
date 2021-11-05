using System;
using System.IO;
using Anidow.Utils;
using Humanizer;

namespace Anidow.Model;

public class FolderFilesModel : ObservableObject
{
    public FolderFilesModel(FileInfo file)
    {
        Name = file.Name;
        Attributes = file.Attributes;
        if (!IsDirectory)
        {
            Length = file.Length;
        }

        LastWriteTime = file.LastWriteTime;
        Extension = file.Extension;
        Path = file.FullName;
    }

    public string Extension { get; set; }
    public FileAttributes Attributes { get; set; }
    public DateTime LastWriteTime { get; set; }
    public long Length { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public bool Highlight { get; set; }

    public string SizeString => IsDirectory ? string.Empty : Length.Bytes().Humanize("#.##");
    public string ModifiedLocalString => LastWriteTime.ToString("g");
    public bool IsDirectory => Attributes.HasFlag(FileAttributes.Directory);
    public bool CanOpenFile => !IsDirectory && ProcessUtil.IsAllowedFile(Extension);
    public bool CanSetFolder => IsDirectory;
    public bool ShowInList { get; set; } = true;
}