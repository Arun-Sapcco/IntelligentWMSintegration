using Newtonsoft.Json;
using Sap.Data.Hana;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IntelligentWMSintegration.DAL
{
    public class HanaDataAccessLayer
    {
        private readonly string _connectionString;

        public HanaDataAccessLayer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DataTable ExecuteQuery(string query, List<HanaParameter> parameters = null, bool isStoredProcedure = false)
        {
            using (HanaConnection connection = new HanaConnection(_connectionString))
            {
                using (HanaCommand command = new HanaCommand(query, connection))
                {
                    if (isStoredProcedure)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }

                    if (parameters != null && parameters.Count > 0)
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                    }
                    connection.Open();
                    using (HanaDataAdapter adapter = new HanaDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
        }

        public T ExecuteQuery<T>(string query, List<HanaParameter> parameters = null, bool isStoredProcedure = false)
        {
            using (HanaConnection connection = new HanaConnection(_connectionString))
            {
                using (HanaCommand command = new HanaCommand(query, connection))
                {
                    if (isStoredProcedure)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }
                    if (parameters != null && parameters.Count > 0)
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                    }
                    connection.Open();
                    using (HanaDataAdapter adapter = new HanaDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        var result = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(dataTable));
                        return result;
                    }
                }
            }
        }


        public async Task<T> ExecuteQueryAsync<T>(string query, List<HanaParameter> parameters = null, bool isStoredProcedure = false)
        {
            using (HanaConnection connection = new HanaConnection(_connectionString))
            {
                using (HanaCommand command = new HanaCommand(query, connection))
                {
                    if (isStoredProcedure)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }
                    if (parameters != null && parameters.Count > 0)
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                    }
                    await connection.OpenAsync();
                    using (HanaDataReader reader = await command.ExecuteReaderAsync())
                    {
                        DataTable dataTable = new DataTable();
                        dataTable.Load(reader);
                        var result = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(dataTable));
                        return result;
                    }
                }
            }
        }

        public void ExecuteNonQuery(string query, List<HanaParameter> parameters = null, bool isStoredProcedure = false)
        {
            using (HanaConnection connection = new HanaConnection(_connectionString))
            {
                using (HanaCommand command = new HanaCommand(query, connection))
                {
                    if (isStoredProcedure)
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    }
                    if (parameters != null && parameters.Count > 0)
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                    }
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
