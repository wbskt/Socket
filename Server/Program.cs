using Server;

namespace Wbskt.Server
{
    public class Program
    {
        private static readonly CancellationTokenSource Cts = new();

        public static async void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.Lifetime.ApplicationStopping.Register(Cts.Cancel);

            app.MapControllers();

            var tasks = new[] { app.RunAsync(), TaskExcecuter.GetInstance().Run(Cts.Token) };

            await Task.WhenAll(tasks);
        }
    }
}
