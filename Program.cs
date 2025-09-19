using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Health: muestra entorno y log level
app.MapGet("/health", () =>
{
    var env = Environment.GetEnvironmentVariable("APP_ENV") ?? "dev";
    var log = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "info";
    return Results.Ok(new { status = "ok", env, log_level = log });
});

// Verifica conexión a la DB (SELECT 1)
app.MapGet("/dbcheck", async () =>
{
    var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
    if (string.IsNullOrWhiteSpace(cs))
        return Results.BadRequest(new { ok = false, error = "ConnectionStrings__Default not set" });

    await using var conn = new NpgsqlConnection(cs);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT 1", conn);
    var r = await cmd.ExecuteScalarAsync();
    return Results.Ok(new { ok = true, ping = r });
});

// Crea tabla si no existe
static async Task EnsureSchemaAsync(NpgsqlConnection conn)
{
    const string sql = @"
        CREATE TABLE IF NOT EXISTS notes (
            id SERIAL PRIMARY KEY,
            content TEXT NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        );";
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
}

// POST /notes?content=...
app.MapPost("/notes", async (string content) =>
{
    var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
    if (string.IsNullOrWhiteSpace(cs))
        return Results.BadRequest(new { ok = false, error = "ConnectionStrings__Default not set" });

    await using var conn = new NpgsqlConnection(cs);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);

    const string insert = "INSERT INTO notes(content) VALUES (@c) RETURNING id, content, created_at";
    await using var cmd = new NpgsqlCommand(insert, conn);
    cmd.Parameters.AddWithValue("c", content);
    await using var rd = await cmd.ExecuteReaderAsync();
    if (await rd.ReadAsync())
    {
        return Results.Created($"/notes/{rd.GetInt32(0)}", new {
            id = rd.GetInt32(0),
            content = rd.GetString(1),
            created_at = rd.GetDateTime(2)
        });
    }
    return Results.Problem("insert failed");
});

// GET /notes
app.MapGet("/notes", async () =>
{
    var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
    if (string.IsNullOrWhiteSpace(cs))
        return Results.BadRequest(new { ok = false, error = "ConnectionStrings__Default not set" });

    await using var conn = new NpgsqlConnection(cs);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);

    const string q = "SELECT id, content, created_at FROM notes ORDER BY id DESC";
    await using var cmd = new NpgsqlCommand(q, conn);
    await using var rd = await cmd.ExecuteReaderAsync();

    var list = new List<object>();
    while (await rd.ReadAsync())
    {
        list.Add(new {
            id = rd.GetInt32(0),
            content = rd.GetString(1),
            created_at = rd.GetDateTime(2)
        });
    }
    return Results.Ok(list);
});

app.Run();
