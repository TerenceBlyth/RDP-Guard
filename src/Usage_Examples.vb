Imports System
Imports System.Configuration
Imports System.Threading.Tasks
Imports RDPGuard.Data

Namespace RDPGuard.Examples

    ''' <summary>
    ''' Examples of how to use the LUFSAnalysisQueue delete functions
    ''' </summary>
    Public Class LUFSAnalysisQueueUsageExamples

        ' Connection string - typically stored in app.config or appsettings.json
        Private Shared ReadOnly ConnectionString As String = _
            If(ConfigurationManager.ConnectionStrings("DefaultConnection")?.ConnectionString,
               "Server=localhost;Database=YourDatabase;Integrated Security=true;")

        ''' <summary>
        ''' Example 1: Delete all records using the repository class (synchronous)
        ''' </summary>
        Public Shared Sub Example1_DeleteAllRecordsSync()
            Try
                Dim repository As New LUFSAnalysisQueueRepository(ConnectionString)
                Dim deletedCount As Integer = repository.DeleteAllRecords()
                Console.WriteLine($"Successfully deleted {deletedCount} records from LUFSAnalysisQueue table.")
            Catch ex As Exception
                Console.WriteLine($"Error deleting records: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Example 2: Delete all records using the repository class (asynchronous)
        ''' </summary>
        Public Shared Async Function Example2_DeleteAllRecordsAsync() As Task
            Try
                Dim repository As New LUFSAnalysisQueueRepository(ConnectionString)
                Dim deletedCount As Integer = Await repository.DeleteAllRecordsAsync()
                Console.WriteLine($"Successfully deleted {deletedCount} records from LUFSAnalysisQueue table.")
            Catch ex As Exception
                Console.WriteLine($"Error deleting records: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Example 3: Delete all records with transaction support
        ''' </summary>
        Public Shared Sub Example3_DeleteAllRecordsWithTransaction()
            Try
                Dim repository As New LUFSAnalysisQueueRepository(ConnectionString)
                Dim deletedCount As Integer = repository.DeleteAllRecordsWithTransaction()
                Console.WriteLine($"Successfully deleted {deletedCount} records from LUFSAnalysisQueue table (with transaction).")
            Catch ex As Exception
                Console.WriteLine($"Error deleting records (transaction rolled back): {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Example 4: Delete all records asynchronously with transaction support
        ''' </summary>
        Public Shared Async Function Example4_DeleteAllRecordsWithTransactionAsync() As Task
            Try
                Dim repository As New LUFSAnalysisQueueRepository(ConnectionString)
                Dim deletedCount As Integer = Await repository.DeleteAllRecordsWithTransactionAsync()
                Console.WriteLine($"Successfully deleted {deletedCount} records from LUFSAnalysisQueue table (with transaction).")
            Catch ex As Exception
                Console.WriteLine($"Error deleting records (transaction rolled back): {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Example 5: Truncate table (fastest for large tables)
        ''' </summary>
        Public Shared Sub Example5_TruncateTable()
            Try
                Dim repository As New LUFSAnalysisQueueRepository(ConnectionString)
                repository.TruncateTable()
                Console.WriteLine("Successfully truncated LUFSAnalysisQueue table.")
            Catch ex As Exception
                Console.WriteLine($"Error truncating table: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Example 6: Truncate table asynchronously
        ''' </summary>
        Public Shared Async Function Example6_TruncateTableAsync() As Task
            Try
                Dim repository As New LUFSAnalysisQueueRepository(ConnectionString)
                Await repository.TruncateTableAsync()
                Console.WriteLine("Successfully truncated LUFSAnalysisQueue table.")
            Catch ex As Exception
                Console.WriteLine($"Error truncating table: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Example 7: Using static helper methods
        ''' </summary>
        Public Shared Sub Example7_UsingHelperMethods()
            Try
                ' Quick synchronous delete
                Dim deletedCount As Integer = LUFSAnalysisQueueHelper.DeleteAllRecords(ConnectionString)
                Console.WriteLine($"Deleted {deletedCount} records using helper method.")

                ' Quick truncate
                LUFSAnalysisQueueHelper.TruncateTable(ConnectionString)
                Console.WriteLine("Truncated table using helper method.")
            Catch ex As Exception
                Console.WriteLine($"Error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Example 8: Using static helper methods asynchronously
        ''' </summary>
        Public Shared Async Function Example8_UsingHelperMethodsAsync() As Task
            Try
                ' Quick asynchronous delete
                Dim deletedCount As Integer = Await LUFSAnalysisQueueHelper.DeleteAllRecordsAsync(ConnectionString)
                Console.WriteLine($"Deleted {deletedCount} records using helper method.")

                ' Quick asynchronous truncate
                Await LUFSAnalysisQueueHelper.TruncateTableAsync(ConnectionString)
                Console.WriteLine("Truncated table using helper method.")
            Catch ex As Exception
                Console.WriteLine($"Error: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Example 9: Simple one-liner approaches
        ''' </summary>
        Public Shared Sub Example9_OneLinerApproaches()
            Try
                ' One-line delete
                Console.WriteLine($"Deleted {New LUFSAnalysisQueueRepository(ConnectionString).DeleteAllRecords()} records")

                ' One-line truncate
                Call New LUFSAnalysisQueueRepository(ConnectionString).TruncateTable()
                Console.WriteLine("Table truncated")

                ' One-line using helper
                Console.WriteLine($"Deleted {LUFSAnalysisQueueHelper.DeleteAllRecords(ConnectionString)} records")

            Catch ex As Exception
                Console.WriteLine($"Error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Example 10: Integration with Windows Service EventLog
        ''' </summary>
        Public Shared Sub Example10_WithEventLog(eventLog As System.Diagnostics.EventLog)
            Try
                Dim repository As New LUFSAnalysisQueueRepository(ConnectionString)
                Dim deletedCount As Integer = repository.DeleteAllRecords()

                If eventLog IsNot Nothing Then
                    eventLog.WriteEntry($"Successfully cleared {deletedCount} records from LUFSAnalysisQueue table.",
                                      System.Diagnostics.EventLogEntryType.Information)
                End If

                Console.WriteLine($"Deleted {deletedCount} records and logged to Event Viewer")
            Catch ex As Exception
                If eventLog IsNot Nothing Then
                    eventLog.WriteEntry($"Error clearing LUFSAnalysisQueue: {ex.Message}",
                                      System.Diagnostics.EventLogEntryType.Error)
                End If
                Console.WriteLine($"Error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Main entry point to run examples
        ''' </summary>
        Public Shared Async Function Main(args As String()) As Task
            Console.WriteLine("=== LUFSAnalysisQueue Delete Examples (VB.NET) ===" & vbCrLf)

            ' Example 1: Synchronous delete
            Console.WriteLine("Example 1: Synchronous delete")
            Example1_DeleteAllRecordsSync()
            Console.WriteLine()

            ' Example 2: Asynchronous delete
            Console.WriteLine("Example 2: Asynchronous delete")
            Await Example2_DeleteAllRecordsAsync()
            Console.WriteLine()

            ' Example 3: Delete with transaction
            Console.WriteLine("Example 3: Delete with transaction")
            Example3_DeleteAllRecordsWithTransaction()
            Console.WriteLine()

            ' Example 5: Truncate table
            Console.WriteLine("Example 5: Truncate table")
            Example5_TruncateTable()
            Console.WriteLine()

            ' Example 7: Using helper methods
            Console.WriteLine("Example 7: Using helper methods")
            Example7_UsingHelperMethods()
            Console.WriteLine()

            ' Example 9: One-liner approaches
            Console.WriteLine("Example 9: One-liner approaches")
            Example9_OneLinerApproaches()
            Console.WriteLine()

            Console.WriteLine("Examples completed. Press any key to exit.")
            Console.ReadKey()
        End Function

    End Class

End Namespace
