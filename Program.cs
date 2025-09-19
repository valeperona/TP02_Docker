using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// In-memory storage for notes
var notes = new List<Note>();

// Health endpoint
app.MapGet("/health", () =>
{
    var appEnv = Environment.GetEnvironmentVariable("APP_ENV") ?? "dev";
    return Results.Ok(new { status = "ok", env = appEnv });
});

// Echo endpoint
app.MapGet("/echo", (string? msg) =>
{
    if (string.IsNullOrEmpty(msg))
    {
        return Results.BadRequest(new { error = "Parameter 'msg' is required" });
    }
    
    return Results.Ok(new { message = msg });
});

// Get all notes
app.MapGet("/notes", () =>
{
    return Results.Ok(notes);
});

// Create a new note
app.MapPost("/notes", (Note note) =>
{
    if (string.IsNullOrEmpty(note.Content))
    {
        return Results.BadRequest(new { error = "Note content is required" });
    }

    note.Id = notes.Count > 0 ? notes.Max(n => n.Id) + 1 : 1;
    note.CreatedAt = DateTime.UtcNow;
    
    notes.Add(note);
    
    return Results.Created($"/notes/{note.Id}", note);
});

app.Run();

// Note model
public class Note
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
