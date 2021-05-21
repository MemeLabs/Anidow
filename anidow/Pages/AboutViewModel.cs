using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Anidow.Model;
using Anidow.Utils;
using Newtonsoft.Json;
using Stylet;

namespace Anidow.Pages
{
    public class AboutViewModel : Screen
    {
        public AboutViewModel(Assembly assembly)
        {
            var licenses = assembly.GetManifestResourceNames().Single(p => p.EndsWith("licenses.json"));
            using var stream = assembly.GetManifestResourceStream(licenses);
            using var reader = new StreamReader(stream!);
            var json = reader.ReadToEnd();
            Packages.AddRange(JsonConvert.DeserializeObject<Package[]>(json));
        }

        public IObservableCollection<Package> Packages { get; } = new BindableCollection<Package>();

        public string Copyright => $"© 2020-{DateTime.Now.Year} MemeLabs";
        public string ProjectUrl => "https://github.com/MemeLabs/Anidow";
        public string AssemblyVersionString => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
        public string Product => "Anidow";

        public void OpenProjectUrl()
        {
            LinkUtil.Open(ProjectUrl);
        }
    }
}
