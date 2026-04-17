using Microsoft.EntityFrameworkCore;
using SIMPE.Dashboard.Data;
using SIMPE.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=simpe_dashboard.db"));

builder.Services.AddScoped<GraphEmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Servir archivos estáticos como HTML, CSS y JS desde la carpeta wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Mapeo de Controladores API
app.MapControllers();

// Crear base de datos si no existe al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
