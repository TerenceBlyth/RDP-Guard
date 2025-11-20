using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace RDPGuard.Data
{
    /// <summary>
    /// Repository class for managing LUFSAnalysisQueue table operations
    /// </summary>
    public class LUFSAnalysisQueueRepository
    {
        private readonly string _connectionString;

        public LUFSAnalysisQueueRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Deletes all records from the LUFSAnalysisQueue table synchronously
        /// </summary>
        /// <returns>Number of records deleted</returns>
        public int DeleteAllRecords()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("DELETE FROM LUFSAnalysisQueue", connection))
                {
                    command.CommandType = CommandType.Text;
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }

        /// <summary>
        /// Deletes all records from the LUFSAnalysisQueue table asynchronously
        /// </summary>
        /// <returns>Number of records deleted</returns>
        public async Task<int> DeleteAllRecordsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("DELETE FROM LUFSAnalysisQueue", connection))
                {
                    command.CommandType = CommandType.Text;
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected;
                }
            }
        }

        /// <summary>
        /// Deletes all records from the LUFSAnalysisQueue table with transaction support
        /// </summary>
        /// <returns>Number of records deleted</returns>
        public int DeleteAllRecordsWithTransaction()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = new SqlCommand("DELETE FROM LUFSAnalysisQueue", connection, transaction))
                        {
                            command.CommandType = CommandType.Text;
                            int rowsAffected = command.ExecuteNonQuery();
                            transaction.Commit();
                            return rowsAffected;
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes all records from the LUFSAnalysisQueue table asynchronously with transaction support
        /// </summary>
        /// <returns>Number of records deleted</returns>
        public async Task<int> DeleteAllRecordsWithTransactionAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = new SqlCommand("DELETE FROM LUFSAnalysisQueue", connection, transaction))
                        {
                            command.CommandType = CommandType.Text;
                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            transaction.Commit();
                            return rowsAffected;
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Truncates the LUFSAnalysisQueue table (faster than DELETE for large tables)
        /// Note: TRUNCATE cannot be used with foreign key constraints
        /// </summary>
        public void TruncateTable()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("TRUNCATE TABLE LUFSAnalysisQueue", connection))
                {
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Truncates the LUFSAnalysisQueue table asynchronously
        /// </summary>
        public async Task TruncateTableAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("TRUNCATE TABLE LUFSAnalysisQueue", connection))
                {
                    command.CommandType = CommandType.Text;
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }

    /// <summary>
    /// Helper class with static methods for quick operations
    /// </summary>
    public static class LUFSAnalysisQueueHelper
    {
        /// <summary>
        /// Deletes all records from the LUFSAnalysisQueue table
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <returns>Number of records deleted</returns>
        public static int DeleteAllRecords(string connectionString)
        {
            var repository = new LUFSAnalysisQueueRepository(connectionString);
            return repository.DeleteAllRecords();
        }

        /// <summary>
        /// Deletes all records from the LUFSAnalysisQueue table asynchronously
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <returns>Number of records deleted</returns>
        public static async Task<int> DeleteAllRecordsAsync(string connectionString)
        {
            var repository = new LUFSAnalysisQueueRepository(connectionString);
            return await repository.DeleteAllRecordsAsync();
        }

        /// <summary>
        /// Truncates the LUFSAnalysisQueue table
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        public static void TruncateTable(string connectionString)
        {
            var repository = new LUFSAnalysisQueueRepository(connectionString);
            repository.TruncateTable();
        }

        /// <summary>
        /// Truncates the LUFSAnalysisQueue table asynchronously
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        public static async Task TruncateTableAsync(string connectionString)
        {
            var repository = new LUFSAnalysisQueueRepository(connectionString);
            await repository.TruncateTableAsync();
        }
    }
}
