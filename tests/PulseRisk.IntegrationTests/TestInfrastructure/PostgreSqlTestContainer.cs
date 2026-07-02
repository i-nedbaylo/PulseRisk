using Testcontainers.PostgreSql;

namespace PulseRisk.IntegrationTests.TestInfrastructure;

internal static class PostgreSqlTestContainer
{
    private const string Image = "postgres:17-alpine";

    public static void SkipIfUnavailable()
    {
        Skip.IfNot(
            DockerAvailability.IsDockerAvailable(),
            "Docker is not available; skipping PostgreSQL Testcontainers scenario.");

        if (!IsContinuousIntegration())
        {
            Skip.IfNot(
                DockerAvailability.IsDockerImageAvailable(Image),
                $"Docker image {Image} is not available locally; skipping PostgreSQL Testcontainers scenario.");
        }
    }

    public static PostgreSqlContainer Create()
    {
        return new PostgreSqlBuilder(Image)
            .WithDatabase("pulserisk_tests")
            .WithUsername("pulserisk")
            .WithPassword("pulserisk")
            .Build();
    }

    public static async Task StartOrSkipAsync(PostgreSqlContainer container)
    {
        try
        {
            await container.StartAsync();
        }
        catch (Exception exception) when (!IsContinuousIntegration() && IsDockerImageUnavailable(exception))
        {
            Skip.If(
                true,
                $"Docker image {Image} is not available; skipping PostgreSQL Testcontainers scenario.");
            throw;
        }
    }

    private static bool IsDockerImageUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (string.Equals(
                current.GetType().Name,
                "DockerImageNotFoundException",
                StringComparison.Ordinal))
            {
                return true;
            }

            if (current.Message.Contains("No such image:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsContinuousIntegration()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
