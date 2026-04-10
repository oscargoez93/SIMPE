using SIMPE.Agent.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Register Custom Services
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddHostedService<HardwareCollectorService>();

// Configure CORS if needed later
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

// Serve static files (the Frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

// Register minimal APIs
app.MapGet("/api/equipos", async (DatabaseService db) =>
{
    var equipos = await db.GetAllEquiposAsync();
    return Results.Ok(equipos);
})
.WithName("GetEquipos");

app.MapGet("/api/equipo/current", async (DatabaseService db) =>
{
    // A simplification to get the most recent or the single agent row
    var equipos = await db.GetAllEquiposAsync();
    var current = equipos.OrderByDescending(e => e.ultima_actualizacion).FirstOrDefault();
    if(current == null) return Results.NotFound();
    return Results.Ok(current);
})
.WithName("GetCurrentEquipo");

app.Run();
