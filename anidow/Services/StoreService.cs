using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StoreService
    {
        private readonly ILogger _logger;

        public StoreService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Save<T>(T value, string path)
        {
            if (value == null)
            {
                return;
            }

            var data = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true,
                IgnoreReadOnlyProperties = true,
            });
            await File.WriteAllTextAsync(path, data, Encoding.UTF8);
        }

        public async Task<T> Load<T>(string path)
        {
            if (!File.Exists(path))
            {
                _logger.Warning("file '{0}' doesn't exist", path);
                return default;
            }

            var data = await File.ReadAllTextAsync(path);

            try
            {
                return JsonSerializer.Deserialize<T>(data);
            }
            catch (Exception e)
            {
                _logger?.Fatal(e, $"failed parsing {path}");
                return default;
            }
        }
    }
}