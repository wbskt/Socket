using Serilog;
using Wbskt.Common;
using Wbskt.Common.Extensions;
using Wbskt.Server.Services;
using Wbskt.Server.Services.Implementation;

namespace Wbskt.Server;

public static class Program
{
    public static readonly CancellationTokenSource Cts = new();
    private static readonly string ProgramDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Wbskt");

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Detect if we are running as a service
        var isWindowsService = !(Environment.UserInteractive || args.Contains("--console"));
        if (isWindowsService)
        {
            builder.Host.UseWindowsService();
        }

        Environment.SetEnvironmentVariable("LogPath", ProgramDataPath);
        if (!Directory.Exists(ProgramDataPath))
        {
            Directory.CreateDirectory(ProgramDataPath);
        }

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog(Log.Logger);

        // Add services to the container.
        builder.Services.ConfigureCommonServices();

        builder.Services.AddSingleton<IClientService, ClientService>();
        builder.Services.AddSingleton<IWebSocketContainer, WebSocketContainer>();
        builder.Services.AddSingleton<IServerInfoService, ServerInfoService>();

        // Authentication & Authorization
        builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = Constants.AuthSchemes.ClientScheme;
                opt.DefaultChallengeScheme = Constants.AuthSchemes.ClientScheme;
            })
            .AddClientAuthScheme(builder.Configuration)
            .AddCoreServerAuthScheme(builder.Configuration)
            .AddSocketServerAuthScheme(builder.Configuration);
        builder.Services.AddAuthorization();

        builder.Services.AddControllers();

        // Register TaskProcessor as a Background Service
        builder.Services.AddHostedService<TaskProcessorHostedService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebSockets();

        app.Lifetime.ApplicationStopping.Register(Cts.Cancel);
        app.Lifetime.ApplicationStarted.Register(app.Services.GetRequiredService<IServerInfoService>().RegisterServer);

        app.MapControllers();

        await app.RunAsync(Cts.Token);
    }
}
