using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(new HealthOptions(
    builder.Configuration.GetConnectionString("Default") ?? string.Empty));

var app = builder.Build();

app.MapGet("/health", async (HealthOptions options, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    try
    {
        await using var connection = new MySqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
        return Results.Ok(new { status = "ok", database = "ok" });
    }
    catch (Exception exception) when (exception is MySqlException or InvalidOperationException or ArgumentException)
    {
        logger.LogWarning("Health check database connection failed ({ExceptionType}).", exception.GetType().Name);
        return Results.Json(
            new { status = "degraded", database = "unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

public sealed record HealthOptions(string ConnectionString);
