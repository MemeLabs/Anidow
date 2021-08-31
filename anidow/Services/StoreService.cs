using System;
using System.IO;
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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Save<T>(T value, string path)
        {
            if (value == null)
            {
                return;
            }


            await using var createStream = File.Create(path);
            await JsonSerializer.SerializeAsync(createStream, value, new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true,
                IgnoreReadOnlyProperties = true,
            });
            await createStream.DisposeAsync();
        }

        public async Task<T> Load<T>(string path)
        {
            if (!File.Exists(path))
            {
                _logger.Warning("file '{0}' doesn't exist", path);
                return default;
            }

            try
            {
                await using var openStream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<T>(openStream);
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "failed parsing {0}", path);
                return default;
            }
        }
    }
}