using System.Data;
using System.Data.SqlClient;
using Wbskt.Server.Services;

namespace Wbskt.Server.Database.Providers
{
    public class ClientProvider : IClientProvider
    {
        private readonly ILogger<ClientProvider> logger;
        private readonly string _connectionString;

        public ClientProvider(ILogger<ClientProvider> logger, IConnectionStringProvider connectionStringProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionStringProvider?.ConnectionString ?? throw new ArgumentNullException(nameof(connectionStringProvider));
        }

        public ClientConenction GetClientConenctionById(int clientId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.Clients_GetBy_Id";

            command.Parameters.Add(new SqlParameter("@Id", clientId));

            using var reader = command.ExecuteReader();
            var mapping = GetColumnMapping(reader);
            reader.Read();
            return ParseData(reader, mapping);
        }

        public void InvalidateToken(int clientId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.Clients_InvalidateToken";

            command.Parameters.Add(new SqlParameter("@Id", clientId));
            command.ExecuteNonQuery();
        }

        private static ClientConenction ParseData(IDataRecord reader, OrdinalColumnMapping mapping)
        {
            var data = new ClientConenction
            {
                TokenId = reader.GetGuid(mapping.TokenId),
                Disabled = reader.GetBoolean(mapping.Disabled),
                ClientId = reader.GetInt32(mapping.ClientId),
                ClientName = reader.GetString(mapping.ClientName),
                ClientUniqueId = reader.GetString(mapping.ClientUniqueId),
                ChannelSubscriberId = reader.GetGuid(mapping.ChannelSubscriberId),
            };

            return data;
        }

        private static OrdinalColumnMapping GetColumnMapping(IDataRecord reader)
        {
            var mapping = new OrdinalColumnMapping();

            mapping.ClientId = reader.GetOrdinal("Id");
            mapping.TokenId = reader.GetOrdinal("TokenId");
            mapping.Disabled = reader.GetOrdinal("Disabled");
            mapping.ClientName = reader.GetOrdinal("ClientName");
            mapping.ClientUniqueId = reader.GetOrdinal("ClientUniqueId");
            mapping.ChannelSubscriberId = reader.GetOrdinal("ChannelSubscriberId");

            return mapping;
        }

        private class OrdinalColumnMapping
        {
            public int TokenId;
            public int Disabled;
            public int ClientId;
            public int ClientName;
            public int ClientUniqueId;
            public int ChannelSubscriberId;
        }
    }
}
