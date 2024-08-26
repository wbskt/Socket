using System.Data;

namespace Wbskt.Server.Database.Providers
{
    internal static class ProviderExtensions
    {
        public static object? ReplaceDbNulls(object? value)
        {
            if (value is DateTime time && time == DateTime.MinValue)
                return DBNull.Value;

            if (value == null)
                return DBNull.Value;

            if (value == DBNull.Value)
                return null;

            return value;
        }

        public static DataTable Int32ListToDataTable(IEnumerable<int> identifiers)
        {
            var dataTable = new DataTable();

            dataTable.Columns.Add(new DataColumn("Id", typeof(int)) { AllowDBNull = false });

            foreach (var id in identifiers)
            {
                var row = dataTable.NewRow();
                row["Id"] = id;
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}
