# LUFSAnalysisQueue Delete Functions (VB.NET)

This document describes the VB.NET functions available for clearing all records from the LUFSAnalysisQueue table.

## Files

- **LUFSAnalysisQueueRepository.vb** - Main repository class with delete functions
- **Usage_Examples.vb** - Examples showing how to use the functions in VB.NET

## Available Methods

### Repository Class Methods

The `LUFSAnalysisQueueRepository` class provides the following methods:

#### 1. DeleteAllRecords()
```vb.net
Public Function DeleteAllRecords() As Integer
```
- **Description**: Deletes all records from the LUFSAnalysisQueue table synchronously
- **Returns**: Number of records deleted (Integer)
- **Use when**: You need synchronous execution and want to know how many records were deleted

#### 2. DeleteAllRecordsAsync()
```vb.net
Public Async Function DeleteAllRecordsAsync() As Task(Of Integer)
```
- **Description**: Deletes all records from the LUFSAnalysisQueue table asynchronously
- **Returns**: Task(Of Integer) - Number of records deleted
- **Use when**: You want non-blocking execution (recommended for UI applications or web services)

#### 3. DeleteAllRecordsWithTransaction()
```vb.net
Public Function DeleteAllRecordsWithTransaction() As Integer
```
- **Description**: Deletes all records with transaction support (synchronous)
- **Returns**: Number of records deleted (Integer)
- **Use when**: You need atomicity - the operation will rollback if any error occurs

#### 4. DeleteAllRecordsWithTransactionAsync()
```vb.net
Public Async Function DeleteAllRecordsWithTransactionAsync() As Task(Of Integer)
```
- **Description**: Deletes all records with transaction support (asynchronous)
- **Returns**: Task(Of Integer) - Number of records deleted
- **Use when**: You need atomicity and non-blocking execution

#### 5. TruncateTable()
```vb.net
Public Sub TruncateTable()
```
- **Description**: Truncates the LUFSAnalysisQueue table (synchronous)
- **Returns**: Nothing
- **Use when**: You have a large table and need maximum performance
- **Note**: Cannot be used if table has foreign key constraints

#### 6. TruncateTableAsync()
```vb.net
Public Async Function TruncateTableAsync() As Task
```
- **Description**: Truncates the LUFSAnalysisQueue table (asynchronous)
- **Returns**: Task
- **Use when**: You need maximum performance with non-blocking execution
- **Note**: Cannot be used if table has foreign key constraints

### Static Helper Methods

The `LUFSAnalysisQueueHelper` class provides quick shared methods:

```vb.net
' Synchronous delete
Dim count As Integer = LUFSAnalysisQueueHelper.DeleteAllRecords(connectionString)

' Asynchronous delete
Dim count As Integer = Await LUFSAnalysisQueueHelper.DeleteAllRecordsAsync(connectionString)

' Synchronous truncate
LUFSAnalysisQueueHelper.TruncateTable(connectionString)

' Asynchronous truncate
Await LUFSAnalysisQueueHelper.TruncateTableAsync(connectionString)
```

## Quick Start Examples

### Option 1: Using Repository Class

```vb.net
Imports RDPGuard.Data

' Create repository instance
Dim repository As New LUFSAnalysisQueueRepository(connectionString)

' Delete all records
Dim deletedCount As Integer = repository.DeleteAllRecords()
Console.WriteLine($"Deleted {deletedCount} records")
```

### Option 2: Using Shared Helper

```vb.net
Imports RDPGuard.Data

' Quick delete
Dim deletedCount As Integer = LUFSAnalysisQueueHelper.DeleteAllRecords(connectionString)
Console.WriteLine($"Deleted {deletedCount} records")
```

### Option 3: Async Pattern (Recommended)

```vb.net
Imports RDPGuard.Data

Dim repository As New LUFSAnalysisQueueRepository(connectionString)
Dim deletedCount As Integer = Await repository.DeleteAllRecordsAsync()
Console.WriteLine($"Deleted {deletedCount} records")
```

### Option 4: One-Liner Approaches

```vb.net
' One-line delete with new instance
Console.WriteLine($"Deleted {New LUFSAnalysisQueueRepository(connectionString).DeleteAllRecords()} records")

' One-line truncate
Call New LUFSAnalysisQueueRepository(connectionString).TruncateTable()

' One-line using helper
Console.WriteLine($"Deleted {LUFSAnalysisQueueHelper.DeleteAllRecords(connectionString)} records")
```

## Connection String Configuration

### app.config
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <connectionStrings>
    <add name="DefaultConnection"
         connectionString="Server=localhost;Database=YourDatabase;Integrated Security=true;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

### Getting Connection String in VB.NET
```vb.net
Imports System.Configuration

Dim connectionString As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString
```

### With Null Checking
```vb.net
Dim connectionString As String = _
    If(ConfigurationManager.ConnectionStrings("DefaultConnection")?.ConnectionString,
       "Server=localhost;Database=YourDatabase;Integrated Security=true;")
```

## Complete Examples

### Example 1: Basic Delete
```vb.net
Imports RDPGuard.Data

Public Sub ClearQueue()
    Try
        Dim repository As New LUFSAnalysisQueueRepository(connectionString)
        Dim count As Integer = repository.DeleteAllRecords()
        Console.WriteLine($"Successfully deleted {count} records")
    Catch ex As Exception
        Console.WriteLine($"Error: {ex.Message}")
    End Try
End Sub
```

### Example 2: Async Delete
```vb.net
Imports RDPGuard.Data

Public Async Function ClearQueueAsync() As Task
    Try
        Dim repository As New LUFSAnalysisQueueRepository(connectionString)
        Dim count As Integer = Await repository.DeleteAllRecordsAsync()
        Console.WriteLine($"Successfully deleted {count} records")
    Catch ex As Exception
        Console.WriteLine($"Error: {ex.Message}")
    End Try
End Function
```

### Example 3: With Transaction
```vb.net
Imports RDPGuard.Data

Public Sub ClearQueueSafely()
    Try
        Dim repository As New LUFSAnalysisQueueRepository(connectionString)
        Dim count As Integer = repository.DeleteAllRecordsWithTransaction()
        Console.WriteLine($"Successfully deleted {count} records (with transaction)")
    Catch ex As Exception
        Console.WriteLine($"Error - transaction rolled back: {ex.Message}")
    End Try
End Sub
```

### Example 4: Truncate for Performance
```vb.net
Imports RDPGuard.Data

Public Sub ClearQueueFast()
    Try
        Dim repository As New LUFSAnalysisQueueRepository(connectionString)
        repository.TruncateTable()
        Console.WriteLine("Successfully truncated table")
    Catch ex As Exception
        Console.WriteLine($"Error: {ex.Message}")
    End Try
End Sub
```

### Example 5: Windows Service Integration
```vb.net
Imports System.ServiceProcess
Imports System.Diagnostics
Imports RDPGuard.Data

Public Class GuardService
    Inherits ServiceBase

    Private ReadOnly _connectionString As String

    Public Sub New()
        _connectionString = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString
    End Sub

    Public Sub ClearAnalysisQueue()
        Try
            Dim repository As New LUFSAnalysisQueueRepository(_connectionString)
            Dim deletedCount As Integer = repository.DeleteAllRecords()

            EventLog.WriteEntry($"Cleared {deletedCount} records from LUFSAnalysisQueue",
                              EventLogEntryType.Information)
        Catch ex As Exception
            EventLog.WriteEntry($"Error clearing queue: {ex.Message}",
                              EventLogEntryType.Error)
        End Try
    End Sub

End Class
```

### Example 6: Using in Form/Button Click
```vb.net
Imports RDPGuard.Data

Public Class MainForm

    Private Sub btnClearQueue_Click(sender As Object, e As EventArgs) Handles btnClearQueue.Click
        Try
            Dim connectionString As String = txtConnectionString.Text
            Dim repository As New LUFSAnalysisQueueRepository(connectionString)

            Dim result As DialogResult = MessageBox.Show(
                "Are you sure you want to delete all records?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning)

            If result = DialogResult.Yes Then
                Dim count As Integer = repository.DeleteAllRecords()
                MessageBox.Show($"Successfully deleted {count} records",
                              "Success",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}",
                          "Error",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

End Class
```

### Example 7: Using in Background Worker
```vb.net
Imports System.ComponentModel
Imports RDPGuard.Data

Public Class MainForm

    Private Sub bgWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles bgWorker.DoWork
        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        Dim repository As New LUFSAnalysisQueueRepository(connectionString)

        worker.ReportProgress(0, "Starting delete operation...")
        Dim count As Integer = repository.DeleteAllRecords()
        worker.ReportProgress(100, $"Deleted {count} records")

        e.Result = count
    End Sub

    Private Sub bgWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles bgWorker.ProgressChanged
        lblStatus.Text = e.UserState.ToString()
        progressBar1.Value = e.ProgressPercentage
    End Sub

    Private Sub bgWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles bgWorker.RunWorkerCompleted
        If e.Error IsNot Nothing Then
            MessageBox.Show($"Error: {e.Error.Message}")
        Else
            MessageBox.Show($"Successfully deleted {e.Result} records")
        End If
    End Sub

End Class
```

## Error Handling

### Basic Try-Catch
```vb.net
Try
    Dim repository As New LUFSAnalysisQueueRepository(connectionString)
    Dim count As Integer = repository.DeleteAllRecords()
    Console.WriteLine($"Deleted {count} records")
Catch ex As SqlException
    Console.WriteLine($"Database error: {ex.Message}")
Catch ex As Exception
    Console.WriteLine($"Error: {ex.Message}")
End Try
```

### With Finally Block
```vb.net
Dim count As Integer = 0
Try
    Dim repository As New LUFSAnalysisQueueRepository(connectionString)
    count = repository.DeleteAllRecords()
Catch ex As Exception
    Console.WriteLine($"Error: {ex.Message}")
Finally
    Console.WriteLine($"Operation completed. Records deleted: {count}")
End Try
```

## DELETE vs TRUNCATE

### Use DELETE when:
- You want to know how many records were deleted
- The table has foreign key constraints
- You need transaction support with rollback capability
- You might want to delete specific records (with WHERE clause)

### Use TRUNCATE when:
- You need maximum performance on large tables
- You don't need to know the count of deleted records
- The table has NO foreign key constraints
- You're deleting ALL records (TRUNCATE doesn't support WHERE)

**Performance**: TRUNCATE is significantly faster than DELETE for large tables because it deallocates data pages instead of deleting rows one by one.

## Dependencies

Required References:
- System.Data
- System.Data.SqlClient
- System.Configuration (for ConfigurationManager)

Target Framework:
- .NET Framework 4.6.1 or higher

## Database Support

For databases other than SQL Server:

### MySQL
```vb.net
Imports MySql.Data.MySqlClient
' Replace SqlConnection with MySqlConnection
```

### PostgreSQL
```vb.net
Imports Npgsql
' Replace SqlConnection with NpgsqlConnection
```

### SQLite
```vb.net
Imports System.Data.SQLite
' Replace SqlConnection with SQLiteConnection
```

## VB.NET Specific Tips

1. **Null-Conditional Operator**: VB.NET uses `?.` for null checking
   ```vb.net
   Dim connStr As String = ConfigurationManager.ConnectionStrings("DefaultConnection")?.ConnectionString
   ```

2. **If() Operator**: VB.NET's ternary operator
   ```vb.net
   Dim value As String = If(condition, trueValue, falseValue)
   ```

3. **String Interpolation**: Use `$` prefix
   ```vb.net
   Console.WriteLine($"Deleted {count} records")
   ```

4. **Async/Await**: Similar to C# but use `As Task`
   ```vb.net
   Public Async Function MyMethod() As Task(Of Integer)
       Return Await repository.DeleteAllRecordsAsync()
   End Function
   ```

## License

This code is provided as-is for use with the RDP-Guard project.
