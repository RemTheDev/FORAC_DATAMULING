using Microsoft.Extensions.Configuration;

namespace DataMulingFilePackager
{
    public class ConfigurationReader
    {
        public T ReadSection<T>(string sectionName)
        {
            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();
            var config = builder.Build();

            return config.GetSection(sectionName).Get<T>();
        }
    }
}
