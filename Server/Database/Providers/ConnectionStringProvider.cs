namespace Wbskt.Server.Database.Providers
{
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        public ConnectionStringProvider(IConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            ConnectionString = configuration["ConnectionStrings:Database"]!;
        }

        public string ConnectionString { get; }
    }
}
