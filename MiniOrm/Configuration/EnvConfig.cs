using DotNetEnv;

namespace MiniOrm.Configuration;

/// <summary>
/// Loads MINIORM_CONN from a .env file when it is not already set in the environment.
/// Searches the current directory and parent folders (repo root).
/// </summary>
public static class EnvConfig
{
    public static void Load()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MINIORM_CONN")))
            return;

        Env.TraversePath().Load();
    }

    public static string GetConnectionString()
        => Environment.GetEnvironmentVariable("MINIORM_CONN")
           ?? throw new InvalidOperationException(
               "MINIORM_CONN is not set. Create a .env file in the repo root (see .env.example) " +
               "or set the environment variable in your shell.");
}
