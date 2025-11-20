Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Threading.Tasks

Namespace RDPGuard.Data

    ''' <summary>
    ''' Repository class for managing LUFSAnalysisQueue table operations
    ''' </summary>
    Public Class LUFSAnalysisQueueRepository

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            If String.IsNullOrEmpty(connectionString) Then
                Throw New ArgumentNullException(NameOf(connectionString))
            End If
            _connectionString = connectionString
        End Sub

        ''' <summary>
        ''' Deletes all records from the LUFSAnalysisQueue table synchronously
        ''' </summary>
        ''' <returns>Number of records deleted</returns>
        Public Function DeleteAllRecords() As Integer
            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand("DELETE FROM LUFSAnalysisQueue", connection)
                    command.CommandType = CommandType.Text
                    Dim rowsAffected As Integer = command.ExecuteNonQuery()
                    Return rowsAffected
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Deletes all records from the LUFSAnalysisQueue table asynchronously
        ''' </summary>
        ''' <returns>Number of records deleted</returns>
        Public Async Function DeleteAllRecordsAsync() As Task(Of Integer)
            Using connection As New SqlConnection(_connectionString)
                Await connection.OpenAsync()

                Using command As New SqlCommand("DELETE FROM LUFSAnalysisQueue", connection)
                    command.CommandType = CommandType.Text
                    Dim rowsAffected As Integer = Await command.ExecuteNonQueryAsync()
                    Return rowsAffected
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Deletes all records from the LUFSAnalysisQueue table with transaction support
        ''' </summary>
        ''' <returns>Number of records deleted</returns>
        Public Function DeleteAllRecordsWithTransaction() As Integer
            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using transaction As SqlTransaction = connection.BeginTransaction()
                    Try
                        Using command As New SqlCommand("DELETE FROM LUFSAnalysisQueue", connection, transaction)
                            command.CommandType = CommandType.Text
                            Dim rowsAffected As Integer = command.ExecuteNonQuery()
                            transaction.Commit()
                            Return rowsAffected
                        End Using
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Deletes all records from the LUFSAnalysisQueue table asynchronously with transaction support
        ''' </summary>
        ''' <returns>Number of records deleted</returns>
        Public Async Function DeleteAllRecordsWithTransactionAsync() As Task(Of Integer)
            Using connection As New SqlConnection(_connectionString)
                Await connection.OpenAsync()

                Using transaction As SqlTransaction = connection.BeginTransaction()
                    Try
                        Using command As New SqlCommand("DELETE FROM LUFSAnalysisQueue", connection, transaction)
                            command.CommandType = CommandType.Text
                            Dim rowsAffected As Integer = Await command.ExecuteNonQueryAsync()
                            transaction.Commit()
                            Return rowsAffected
                        End Using
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Truncates the LUFSAnalysisQueue table (faster than DELETE for large tables)
        ''' Note: TRUNCATE cannot be used with foreign key constraints
        ''' </summary>
        Public Sub TruncateTable()
            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand("TRUNCATE TABLE LUFSAnalysisQueue", connection)
                    command.CommandType = CommandType.Text
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        ''' <summary>
        ''' Truncates the LUFSAnalysisQueue table asynchronously
        ''' </summary>
        Public Async Function TruncateTableAsync() As Task
            Using connection As New SqlConnection(_connectionString)
                Await connection.OpenAsync()

                Using command As New SqlCommand("TRUNCATE TABLE LUFSAnalysisQueue", connection)
                    command.CommandType = CommandType.Text
                    Await command.ExecuteNonQueryAsync()
                End Using
            End Using
        End Function

    End Class

    ''' <summary>
    ''' Helper class with static methods for quick operations
    ''' </summary>
    Public NotInheritable Class LUFSAnalysisQueueHelper

        Private Sub New()
            ' Prevent instantiation
        End Sub

        ''' <summary>
        ''' Deletes all records from the LUFSAnalysisQueue table
        ''' </summary>
        ''' <param name="connectionString">Database connection string</param>
        ''' <returns>Number of records deleted</returns>
        Public Shared Function DeleteAllRecords(connectionString As String) As Integer
            Dim repository As New LUFSAnalysisQueueRepository(connectionString)
            Return repository.DeleteAllRecords()
        End Function

        ''' <summary>
        ''' Deletes all records from the LUFSAnalysisQueue table asynchronously
        ''' </summary>
        ''' <param name="connectionString">Database connection string</param>
        ''' <returns>Number of records deleted</returns>
        Public Shared Async Function DeleteAllRecordsAsync(connectionString As String) As Task(Of Integer)
            Dim repository As New LUFSAnalysisQueueRepository(connectionString)
            Return Await repository.DeleteAllRecordsAsync()
        End Function

        ''' <summary>
        ''' Truncates the LUFSAnalysisQueue table
        ''' </summary>
        ''' <param name="connectionString">Database connection string</param>
        Public Shared Sub TruncateTable(connectionString As String)
            Dim repository As New LUFSAnalysisQueueRepository(connectionString)
            repository.TruncateTable()
        End Sub

        ''' <summary>
        ''' Truncates the LUFSAnalysisQueue table asynchronously
        ''' </summary>
        ''' <param name="connectionString">Database connection string</param>
        Public Shared Async Function TruncateTableAsync(connectionString As String) As Task
            Dim repository As New LUFSAnalysisQueueRepository(connectionString)
            Await repository.TruncateTableAsync()
        End Function

    End Class

End Namespace
