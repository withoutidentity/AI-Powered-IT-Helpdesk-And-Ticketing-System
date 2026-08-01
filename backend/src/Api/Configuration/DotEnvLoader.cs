namespace Api.Configuration;

internal static class DotEnvLoader
{
    public static void LoadBackendEnvironment()
    {
        var envPath = FindBackendEnvFile();
        if (envPath is null)
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line[7..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is not null)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, Unquote(value));
        }
    }

    private static string? FindBackendEnvFile()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                var backendEnv = Path.Combine(directory.FullName, "backend", ".env");
                if (File.Exists(backendEnv))
                {
                    return backendEnv;
                }

                if (string.Equals(directory.Name, "backend", StringComparison.OrdinalIgnoreCase))
                {
                    var env = Path.Combine(directory.FullName, ".env");
                    if (File.Exists(env))
                    {
                        return env;
                    }
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        return value;
    }
}