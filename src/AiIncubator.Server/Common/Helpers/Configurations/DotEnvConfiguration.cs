namespace AiIncubator.Server.Common.Helpers.Configurations;

/// <summary>
/// Loads a local <c>.env</c> file into configuration as the highest-priority source so that
/// secrets (API keys, issuer URLs) live only in the gitignored <c>.env</c> file and never in
/// committed appsettings. Keys use the standard double-underscore section separator,
/// e.g. <c>Chat__ApiKey</c> maps to <c>Chat:ApiKey</c>.
/// </summary>
public static class DotEnvConfiguration
{
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, string path)
    {
        if (!File.Exists(path))
        {
            return builder;
        }

        var values = new Dictionary<string, string?>();
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            string key = line[..separator].Trim().Replace("__", ":");
            string value = line[(separator + 1)..].Trim().Trim('"');
            values[key] = value;
        }

        builder.AddInMemoryCollection(values);
        return builder;
    }
}
