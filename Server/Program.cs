using Wbskt.Server.Database;
using Wbskt.Server.Database.Providers;
using Wbskt.Server.Services;
using Wbskt.Server.Services.Implementation;

namespace Wbskt.Server
{
    public class Program
    {
        private static readonly CancellationTokenSource Cts = new();

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddSingleton<IClientProvider, ClientProvider>();
            builder.Services.AddSingleton<IServerInfoProvider, ServerInfoProvider>();
            builder.Services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

            builder.Services.AddSingleton<IClientService, ClientService>();
            builder.Services.AddSingleton<IWebSocketContainer, WebSocketContainer>();
            builder.Services.AddSingleton<IServerInfoService, ServerInfoService>();

            builder.Services.AddJwtAuthentication(builder.Configuration);

            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.Lifetime.ApplicationStopping.Register(Cts.Cancel);
            app.Lifetime.ApplicationStarted.Register(app.Services.GetRequiredService<IServerInfoService>().RegisterServer);

            app.MapControllers();

            app.RunAsync();

            TaskExcecuter.GetInstance().Run(Cts.Token);
        }
    }
}
