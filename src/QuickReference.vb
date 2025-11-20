Imports System.Data.SqlClient
Imports System.Configuration

''' <summary>
''' Quick reference snippets for clearing LUFSAnalysisQueue table
''' Copy and paste these into your VB.NET code
''' </summary>
Module QuickReference

    ' ===========================================
    ' METHOD 1: SIMPLEST - One line delete
    ' ===========================================
    Sub Method1_SimplestDelete()
        Dim connStr As String = "Server=localhost;Database=YourDB;Integrated Security=true;"

        Using conn As New SqlConnection(connStr)
            conn.Open()
            Using cmd As New SqlCommand("DELETE FROM LUFSAnalysisQueue", conn)
                Dim count As Integer = cmd.ExecuteNonQuery()
                Console.WriteLine($"Deleted {count} records")
            End Using
        End Using
    End Sub

    ' ===========================================
    ' METHOD 2: With Error Handling
    ' ===========================================
    Sub Method2_WithErrorHandling()
        Dim connStr As String = "Server=localhost;Database=YourDB;Integrated Security=true;"

        Try
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand("DELETE FROM LUFSAnalysisQueue", conn)
                    Dim count As Integer = cmd.ExecuteNonQuery()
                    Console.WriteLine($"Successfully deleted {count} records")
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
        End Try
    End Sub

    ' ===========================================
    ' METHOD 3: TRUNCATE (fastest for large tables)
    ' ===========================================
    Sub Method3_TruncateTable()
        Dim connStr As String = "Server=localhost;Database=YourDB;Integrated Security=true;"

        Try
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand("TRUNCATE TABLE LUFSAnalysisQueue", conn)
                    cmd.ExecuteNonQuery()
                    Console.WriteLine("Table truncated successfully")
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
        End Try
    End Sub

    ' ===========================================
    ' METHOD 4: With Transaction (rollback on error)
    ' ===========================================
    Sub Method4_WithTransaction()
        Dim connStr As String = "Server=localhost;Database=YourDB;Integrated Security=true;"

        Using conn As New SqlConnection(connStr)
            conn.Open()
            Dim transaction As SqlTransaction = conn.BeginTransaction()

            Try
                Using cmd As New SqlCommand("DELETE FROM LUFSAnalysisQueue", conn, transaction)
                    Dim count As Integer = cmd.ExecuteNonQuery()
                    transaction.Commit()
                    Console.WriteLine($"Successfully deleted {count} records (transaction committed)")
                End Using
            Catch ex As Exception
                transaction.Rollback()
                Console.WriteLine($"Error - transaction rolled back: {ex.Message}")
            End Try
        End Using
    End Sub

    ' ===========================================
    ' METHOD 5: Async Version
    ' ===========================================
    Async Function Method5_AsyncDelete() As Task(Of Integer)
        Dim connStr As String = "Server=localhost;Database=YourDB;Integrated Security=true;"

        Using conn As New SqlConnection(connStr)
            Await conn.OpenAsync()
            Using cmd As New SqlCommand("DELETE FROM LUFSAnalysisQueue", conn)
                Dim count As Integer = Await cmd.ExecuteNonQueryAsync()
                Console.WriteLine($"Deleted {count} records")
                Return count
            End Using
        End Using
    End Function

    ' ===========================================
    ' METHOD 6: Using Connection String from Config
    ' ===========================================
    Sub Method6_FromConfig()
        ' Add to app.config:
        ' <connectionStrings>
        '   <add name="DefaultConnection" connectionString="Server=...;Database=...;" />
        ' </connectionStrings>

        Dim connStr As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString

        Try
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand("DELETE FROM LUFSAnalysisQueue", conn)
                    Dim count As Integer = cmd.ExecuteNonQuery()
                    Console.WriteLine($"Deleted {count} records")
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
        End Try
    End Sub

    ' ===========================================
    ' METHOD 7: For Windows Service with EventLog
    ' ===========================================
    Sub Method7_WindowsService(eventLog As System.Diagnostics.EventLog)
        Dim connStr As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString

        Try
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand("DELETE FROM LUFSAnalysisQueue", conn)
                    Dim count As Integer = cmd.ExecuteNonQuery()

                    ' Log to Windows Event Viewer
                    If eventLog IsNot Nothing Then
                        eventLog.WriteEntry($"Cleared {count} records from LUFSAnalysisQueue",
                                          System.Diagnostics.EventLogEntryType.Information)
                    End If

                    Console.WriteLine($"Deleted {count} records and logged to Event Viewer")
                End Using
            End Using
        Catch ex As Exception
            If eventLog IsNot Nothing Then
                eventLog.WriteEntry($"Error clearing LUFSAnalysisQueue: {ex.Message}",
                                  System.Diagnostics.EventLogEntryType.Error)
            End If
            Throw
        End Try
    End Sub

    ' ===========================================
    ' METHOD 8: Button Click Handler (Windows Forms)
    ' ===========================================
    Sub Method8_ButtonClick(sender As Object, e As EventArgs)
        Dim connStr As String = "Server=localhost;Database=YourDB;Integrated Security=true;"

        Dim result As DialogResult = MessageBox.Show(
            "Delete all records from LUFSAnalysisQueue?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning)

        If result = DialogResult.Yes Then
            Try
                Using conn As New SqlConnection(connStr)
                    conn.Open()
                    Using cmd As New SqlCommand("DELETE FROM LUFSAnalysisQueue", conn)
                        Dim count As Integer = cmd.ExecuteNonQuery()
                        MessageBox.Show($"Successfully deleted {count} records",
                                      "Success",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information)
                    End Using
                End Using
            Catch ex As Exception
                MessageBox.Show($"Error: {ex.Message}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    ' ===========================================
    ' METHOD 9: Return Count Only
    ' ===========================================
    Function Method9_GetDeleteCount(connectionString As String) As Integer
        Using conn As New SqlConnection(connectionString)
            conn.Open()
            Using cmd As New SqlCommand("DELETE FROM LUFSAnalysisQueue", conn)
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

    ' ===========================================
    ' METHOD 10: Stored Procedure Approach
    ' ===========================================
    Sub Method10_StoredProcedure()
        ' First create this stored procedure in SQL Server:
        ' CREATE PROCEDURE sp_ClearLUFSAnalysisQueue
        ' AS
        ' BEGIN
        '     DELETE FROM LUFSAnalysisQueue
        '     SELECT @@ROWCOUNT AS DeletedCount
        ' END

        Dim connStr As String = "Server=localhost;Database=YourDB;Integrated Security=true;"

        Try
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand("sp_ClearLUFSAnalysisQueue", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    Dim count As Integer = CInt(cmd.ExecuteScalar())
                    Console.WriteLine($"Deleted {count} records via stored procedure")
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
        End Try
    End Sub

End Module

' ===========================================
' USAGE NOTES
' ===========================================
'
' 1. Replace connection string with your actual database connection
' 2. Add reference to System.Data.SqlClient
' 3. For app.config, add reference to System.Configuration
' 4. For Windows Forms, add reference to System.Windows.Forms
'
' WHEN TO USE EACH METHOD:
' - Method 1-2: Simple console apps or services
' - Method 3: Large tables, need speed, no foreign keys
' - Method 4: Critical operations requiring atomicity
' - Method 5: Web apps, async I/O operations
' - Method 6: Production apps with configuration files
' - Method 7: Windows Services with event logging
' - Method 8: Desktop applications with UI
' - Method 9: When you need the count for logic
' - Method 10: When using stored procedures
'
