using Serilog;
using Wbskt.Common;
using Wbskt.Common.Extensions;
using Wbskt.Common.Services;
using Wbskt.Socket.Service.Services;
using Wbskt.Socket.Service.Services.Implementation;

namespace Wbskt.Socket.Service;

public static class Program
{
    private static readonly string ProgramDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Wbskt");

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Environment.SetEnvironmentVariable(Constants.LoggingConstants.LogPath, ProgramDataPath);
        Environment.SetEnvironmentVariable(nameof(Constants.ServerType), Constants.ServerType.SocketServer.ToString());

        // Detect if we are running as a service
        var isWindowsService = !(Environment.UserInteractive || args.Contains("--console"));
        if (isWindowsService)
        {
            builder.Host.UseWindowsService();
        }

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

        builder.Services.AddSingleton<ICancellationService, CancellationService>();
        builder.Services.AddSingleton<CoreServerConnection>();
        builder.Services.AddSingleton<IWebSocketContainer, WebSocketContainer>();
        builder.Services.AddSingleton<IServerInfoService, ServerInfoService>();
        builder.Services.Configure<SocketServerConfiguration>(builder.Configuration.GetSection("Wbskt.Socket"));

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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebSockets();

        app.MapControllers();

        var cancellationService = app.Services.GetRequiredService<ICancellationService>();

        // register server
        app.Lifetime.ApplicationStarted.Register(() => app.Services.GetRequiredService<IServerInfoService>().RegisterServer().Wait());

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            cancellationService.Cancel().Wait();
        });

        await app.RunAsync(cancellationService.GetToken());
    }
}
