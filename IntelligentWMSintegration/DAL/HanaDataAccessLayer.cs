using Newtonsoft.Json;
using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.DAL
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

        public async Task<int> ExecuteCountQueryAsync(string query, List<HanaParameter> parameters = null, bool isStoredProcedure = false)
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

                    object result = await command.ExecuteScalarAsync();
                    int count = Convert.ToInt32(result);
                    return count;
                }
            }
        }


        public int ExecuteNonQuery(string query, List<HanaParameter> parameters = null, bool isStoredProcedure = false)
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
                    return command.ExecuteNonQuery();
                }
            }
        }


        public async Task<int> ExecuteNonQueryAsync(string query, List<HanaParameter> parameters = null, bool isStoredProcedure = false)
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
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
