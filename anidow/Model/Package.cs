namespace Anidow.Model;

public sealed class Package : ObservableObject
{
    public string PackageName { get; set; }

    public string PackageVersion { get; set; }

    public string PackageUrl { get; set; }

    public string Copyright { get; set; }
    public string[] Authors { get; set; }
    public string AuthorsString => string.Join(", ", Authors);

    public string Description { get; set; }
    public string LicenseUrl { get; set; }
    public string LicenseType { get; set; }
}