using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.DAL
{
    public class SqlDataAccessLayer
    {
        private readonly string _connectionString;

        public SqlDataAccessLayer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DataTable ExecuteQuery(string query, List<SqlParameter> parameters = null, bool isStoredProcedure = false)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
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
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
        }

        public T ExecuteQuery<T>(string query, List<SqlParameter> parameters = null, bool isStoredProcedure = false)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
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
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        var result = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(dataTable));
                        return result;
                    }
                }
            }
        }

        public async Task<T> ExecuteQueryAsync<T>(string query, List<SqlParameter> parameters = null, bool isStoredProcedure = false)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
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
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        DataTable dataTable = new DataTable();
                        dataTable.Load(reader);
                        var result = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(dataTable));
                        return result;
                    }
                }
            }
        }

        public void ExecuteNonQuery(string query, List<SqlParameter> parameters = null, bool isStoredProcedure = false)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
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

        public void ExecuteNonQueryWithTransaction(string query1, SqlParameter[] parameters1, string query2, SqlParameter[] parameters2)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        SqlCommand command1 = new SqlCommand(query1, connection, transaction);
                        command1.Parameters.AddRange(parameters1);
                        command1.ExecuteNonQuery();

                        SqlCommand command2 = new SqlCommand(query2, connection, transaction);
                        command2.Parameters.AddRange(parameters2);
                        command2.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public void SqlBulkInsert(DataTable dt)
        {
            using (SqlConnection dbConn = new SqlConnection(_connectionString))
            {
                dbConn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(dbConn))
                {
                    bulkCopy.DestinationTableName = "dbo.OITW";
                    try
                    {
                        foreach (DataColumn clmn in dt.Columns)
                        {
                            bulkCopy.ColumnMappings.Add(clmn.ColumnName, clmn.ColumnName);
                        }

                        bulkCopy.WriteToServer(dt);
                    }
                    catch (Exception)
                    {
                        //myLogger.Error("Fail to upload session data. ", ex);
                    }
                }
            }
        }

        public bool ExecuteNonQueryWithTransaction(List<string> queryList)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var query in queryList)
                        {
                            SqlCommand command1 = new SqlCommand(query, connection, transaction);
                            command1.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }



    }
}
