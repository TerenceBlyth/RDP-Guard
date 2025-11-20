using System;
using System.Configuration;
using System.Threading.Tasks;
using RDPGuard.Data;

namespace RDPGuard.Examples
{
    /// <summary>
    /// Examples of how to use the LUFSAnalysisQueue delete functions
    /// </summary>
    public class LUFSAnalysisQueueUsageExamples
    {
        // Connection string - typically stored in app.config or appsettings.json
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
            ?? "Server=localhost;Database=YourDatabase;Integrated Security=true;";

        /// <summary>
        /// Example 1: Delete all records using the repository class (synchronous)
        /// </summary>
        public static void Example1_DeleteAllRecordsSync()
        {
            try
            {
                var repository = new LUFSAnalysisQueueRepository(ConnectionString);
                int deletedCount = repository.DeleteAllRecords();
                Console.WriteLine($"Successfully deleted {deletedCount} records from LUFSAnalysisQueue table.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting records: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 2: Delete all records using the repository class (asynchronous)
        /// </summary>
        public static async Task Example2_DeleteAllRecordsAsync()
        {
            try
            {
                var repository = new LUFSAnalysisQueueRepository(ConnectionString);
                int deletedCount = await repository.DeleteAllRecordsAsync();
                Console.WriteLine($"Successfully deleted {deletedCount} records from LUFSAnalysisQueue table.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting records: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 3: Delete all records with transaction support
        /// </summary>
        public static void Example3_DeleteAllRecordsWithTransaction()
        {
            try
            {
                var repository = new LUFSAnalysisQueueRepository(ConnectionString);
                int deletedCount = repository.DeleteAllRecordsWithTransaction();
                Console.WriteLine($"Successfully deleted {deletedCount} records from LUFSAnalysisQueue table (with transaction).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting records (transaction rolled back): {ex.Message}");
            }
        }

        /// <summary>
        /// Example 4: Delete all records asynchronously with transaction support
        /// </summary>
        public static async Task Example4_DeleteAllRecordsWithTransactionAsync()
        {
            try
            {
                var repository = new LUFSAnalysisQueueRepository(ConnectionString);
                int deletedCount = await repository.DeleteAllRecordsWithTransactionAsync();
                Console.WriteLine($"Successfully deleted {deletedCount} records from LUFSAnalysisQueue table (with transaction).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting records (transaction rolled back): {ex.Message}");
            }
        }

        /// <summary>
        /// Example 5: Truncate table (fastest for large tables)
        /// </summary>
        public static void Example5_TruncateTable()
        {
            try
            {
                var repository = new LUFSAnalysisQueueRepository(ConnectionString);
                repository.TruncateTable();
                Console.WriteLine("Successfully truncated LUFSAnalysisQueue table.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error truncating table: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 6: Truncate table asynchronously
        /// </summary>
        public static async Task Example6_TruncateTableAsync()
        {
            try
            {
                var repository = new LUFSAnalysisQueueRepository(ConnectionString);
                await repository.TruncateTableAsync();
                Console.WriteLine("Successfully truncated LUFSAnalysisQueue table.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error truncating table: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 7: Using static helper methods
        /// </summary>
        public static void Example7_UsingHelperMethods()
        {
            try
            {
                // Quick synchronous delete
                int deletedCount = LUFSAnalysisQueueHelper.DeleteAllRecords(ConnectionString);
                Console.WriteLine($"Deleted {deletedCount} records using helper method.");

                // Quick truncate
                LUFSAnalysisQueueHelper.TruncateTable(ConnectionString);
                Console.WriteLine("Truncated table using helper method.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 8: Using static helper methods asynchronously
        /// </summary>
        public static async Task Example8_UsingHelperMethodsAsync()
        {
            try
            {
                // Quick asynchronous delete
                int deletedCount = await LUFSAnalysisQueueHelper.DeleteAllRecordsAsync(ConnectionString);
                Console.WriteLine($"Deleted {deletedCount} records using helper method.");

                // Quick asynchronous truncate
                await LUFSAnalysisQueueHelper.TruncateTableAsync(ConnectionString);
                Console.WriteLine("Truncated table using helper method.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Main entry point to run examples
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== LUFSAnalysisQueue Delete Examples ===\n");

            // Example 1: Synchronous delete
            Console.WriteLine("Example 1: Synchronous delete");
            Example1_DeleteAllRecordsSync();
            Console.WriteLine();

            // Example 2: Asynchronous delete
            Console.WriteLine("Example 2: Asynchronous delete");
            await Example2_DeleteAllRecordsAsync();
            Console.WriteLine();

            // Example 3: Delete with transaction
            Console.WriteLine("Example 3: Delete with transaction");
            Example3_DeleteAllRecordsWithTransaction();
            Console.WriteLine();

            // Example 7: Using helper methods
            Console.WriteLine("Example 7: Using helper methods");
            Example7_UsingHelperMethods();
            Console.WriteLine();

            Console.WriteLine("Examples completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
