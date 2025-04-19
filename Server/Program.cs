using Wbskt.Common;
using Wbskt.Common.Extensions;
using Wbskt.Server.Services;
using Wbskt.Server.Services.Implementation;

namespace Wbskt.Server;

public static class Program
{
    private static readonly CancellationTokenSource Cts = new();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.ConfigureCommonServices();

        builder.Services.AddSingleton<IClientService, ClientService>();
        builder.Services.AddSingleton<IWebSocketContainer, WebSocketContainer>();
        builder.Services.AddSingleton<IServerInfoService, ServerInfoService>();

        builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = Constants.AuthSchemes.ClientScheme;
                opt.DefaultChallengeScheme = Constants.AuthSchemes.ClientScheme;
            })
            .AddClientAuthScheme(builder.Configuration)
            .AddCoreServerAuthScheme(builder.Configuration)
            .AddSocketServerAuthScheme(builder.Configuration);

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.Lifetime.ApplicationStopping.Register(Cts.Cancel);
        app.Lifetime.ApplicationStarted.Register(app.Services.GetRequiredService<IServerInfoService>().RegisterServer);

        app.MapControllers();
        app.UseWebSockets();

        app.RunAsync();

        TaskProcessor.GetInstance().Run(Cts.Token);
    }
}
