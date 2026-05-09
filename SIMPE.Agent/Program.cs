using SIMPE.Agent.Services;

namespace SIMPE.Agent;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var cts = new CancellationTokenSource();

        // Start Kestrel in background thread (MTA)
        var hostTask = Task.Run(async () =>
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://localhost:5073");
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            builder.Services.AddOpenApi();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<SecurityCollectorService>();
            builder.Services.AddSingleton<PerformanceCollectorService>();
            builder.Services.AddSingleton<NavigationHistoryCollectorService>();
            builder.Services.AddHostedService<HardwareCollectorService>();
            builder.Services.AddHostedService<PerformanceAutoCollector>();
            builder.Services.AddHostedService<SecurityAutoCollector>();
            builder.Services.AddHostedService<NavigationAutoCollector>();
            builder.Services.AddHostedService<DataRetentionService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            var app = builder.Build();
            app.UseCors("AllowAll");
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapGet("/api/equipos", async (DatabaseService db) => Results.Ok(await db.GetAllEquiposAsync()));
            app.MapGet("/api/equipo/current", async (DatabaseService db) =>
            {
                var equipos = await db.GetAllEquiposAsync();
                var equipo = equipos.FirstOrDefault();
                return equipo is not null ? Results.Ok(equipo) : Results.NotFound();
            });
            app.MapGet("/api/security/current", (SecurityCollectorService s) => Results.Ok(s.GatherSecurityInfo()));
            app.MapGet("/api/performance/current", (PerformanceCollectorService p) => Results.Ok(p.GatherPerformanceMetrics()));
            app.MapGet("/api/navigation/current", (NavigationHistoryCollectorService n, int? limit) => Results.Ok(n.GatherNavigationHistory(limit ?? 2000)));

            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            var serverReady = new TaskCompletionSource();
            lifetime.ApplicationStarted.Register(() => serverReady.TrySetResult());

            using var reg = cts.Token.Register(() => lifetime.StopApplication());
            var runTask = app.RunAsync(cts.Token);
            await serverReady.Task;
            await runTask;
        });

        // Run WinForms in a dedicated STA thread so WebView2 gets the correct COM apartment
        var uiThread = new Thread(() =>
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(cts));
        });
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();
        uiThread.Join();

        // When the UI closes, stop Kestrel
        cts.Cancel();
        try { hostTask.Wait(TimeSpan.FromSeconds(10)); } catch { /* ignore shutdown errors */ }
    }
}
