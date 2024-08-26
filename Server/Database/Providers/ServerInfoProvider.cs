using System.Data;
using System.Data.SqlClient;
using Wbskt.Server.Services;

namespace Wbskt.Server.Database.Providers
{
    public class ServerInfoProvider : IServerInfoProvider
    {
        private readonly ILogger<ServerInfoProvider> logger;
        private readonly string _connectionString;

        public ServerInfoProvider(ILogger<ServerInfoProvider> logger, IConnectionStringProvider connectionStringProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionStringProvider?.ConnectionString ?? throw new ArgumentNullException(nameof(connectionStringProvider));
        }

        public int RegisterServer(ServerInfo serverInfo)
        {
            if (serverInfo == null)
                throw new ArgumentNullException(nameof(serverInfo));

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.Servers_Insert";

            command.Parameters.Add(new SqlParameter("@IPAddress", ProviderExtensions.ReplaceDbNulls(serverInfo.Address.Host)));
            command.Parameters.Add(new SqlParameter("@Port", ProviderExtensions.ReplaceDbNulls(serverInfo.Address.Port)));
            command.Parameters.Add(new SqlParameter("@Active", ProviderExtensions.ReplaceDbNulls(serverInfo.Active)));

            var id = new SqlParameter("@Id", SqlDbType.Int) { Size = int.MaxValue };
            id.Direction = ParameterDirection.Output;
            command.Parameters.Add(id);
            command.ExecuteNonQuery();

            return serverInfo.ServerId = (int)(ProviderExtensions.ReplaceDbNulls(id.Value) ?? 0);
        }
    }
}
