# LUFSAnalysisQueue Delete Functions

This document describes the functions available for deleting all records from the LUFSAnalysisQueue table.

## Files

- **LUFSAnalysisQueueRepository.cs** - Main repository class with delete functions
- **Usage_Examples.cs** - Examples showing how to use the functions

## Available Methods

### Repository Class Methods

The `LUFSAnalysisQueueRepository` class provides the following methods:

#### 1. DeleteAllRecords()
```csharp
public int DeleteAllRecords()
```
- **Description**: Deletes all records from the LUFSAnalysisQueue table synchronously
- **Returns**: Number of records deleted
- **Use when**: You need synchronous execution and want to know how many records were deleted

#### 2. DeleteAllRecordsAsync()
```csharp
public async Task<int> DeleteAllRecordsAsync()
```
- **Description**: Deletes all records from the LUFSAnalysisQueue table asynchronously
- **Returns**: Number of records deleted
- **Use when**: You want non-blocking execution (recommended for UI applications or web services)

#### 3. DeleteAllRecordsWithTransaction()
```csharp
public int DeleteAllRecordsWithTransaction()
```
- **Description**: Deletes all records with transaction support (synchronous)
- **Returns**: Number of records deleted
- **Use when**: You need atomicity - the operation will rollback if any error occurs

#### 4. DeleteAllRecordsWithTransactionAsync()
```csharp
public async Task<int> DeleteAllRecordsWithTransactionAsync()
```
- **Description**: Deletes all records with transaction support (asynchronous)
- **Returns**: Number of records deleted
- **Use when**: You need atomicity and non-blocking execution

#### 5. TruncateTable()
```csharp
public void TruncateTable()
```
- **Description**: Truncates the LUFSAnalysisQueue table (synchronous)
- **Returns**: Nothing
- **Use when**: You have a large table and need maximum performance
- **Note**: Cannot be used if table has foreign key constraints

#### 6. TruncateTableAsync()
```csharp
public async Task TruncateTableAsync()
```
- **Description**: Truncates the LUFSAnalysisQueue table (asynchronous)
- **Returns**: Nothing
- **Use when**: You need maximum performance with non-blocking execution
- **Note**: Cannot be used if table has foreign key constraints

### Static Helper Methods

The `LUFSAnalysisQueueHelper` class provides quick static methods:

```csharp
// Synchronous delete
int count = LUFSAnalysisQueueHelper.DeleteAllRecords(connectionString);

// Asynchronous delete
int count = await LUFSAnalysisQueueHelper.DeleteAllRecordsAsync(connectionString);

// Synchronous truncate
LUFSAnalysisQueueHelper.TruncateTable(connectionString);

// Asynchronous truncate
await LUFSAnalysisQueueHelper.TruncateTableAsync(connectionString);
```

## Quick Start

### Option 1: Using Repository Class

```csharp
using RDPGuard.Data;

// Create repository instance
var repository = new LUFSAnalysisQueueRepository(connectionString);

// Delete all records
int deletedCount = repository.DeleteAllRecords();
Console.WriteLine($"Deleted {deletedCount} records");
```

### Option 2: Using Static Helper

```csharp
using RDPGuard.Data;

// Quick delete
int deletedCount = LUFSAnalysisQueueHelper.DeleteAllRecords(connectionString);
Console.WriteLine($"Deleted {deletedCount} records");
```

### Option 3: Async Pattern (Recommended)

```csharp
using RDPGuard.Data;

var repository = new LUFSAnalysisQueueRepository(connectionString);
int deletedCount = await repository.DeleteAllRecordsAsync();
Console.WriteLine($"Deleted {deletedCount} records");
```

## Connection String

The connection string should be configured in your app.config or appsettings.json:

### app.config
```xml
<configuration>
  <connectionStrings>
    <add name="DefaultConnection"
         connectionString="Server=localhost;Database=YourDatabase;Integrated Security=true;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

### Getting Connection String in Code
```csharp
string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
```

## DELETE vs TRUNCATE

### Use DELETE when:
- You want to know how many records were deleted
- The table has foreign key constraints
- You need transaction support with rollback capability
- You want to delete specific records (with a WHERE clause)

### Use TRUNCATE when:
- You need maximum performance on large tables
- You don't need to know the count of deleted records
- The table has NO foreign key constraints
- You're deleting ALL records (TRUNCATE doesn't support WHERE)

**Performance**: TRUNCATE is significantly faster than DELETE for large tables because it deallocates data pages instead of deleting rows one by one.

## Error Handling

All methods throw exceptions if something goes wrong. Always use try-catch:

```csharp
try
{
    var repository = new LUFSAnalysisQueueRepository(connectionString);
    int count = repository.DeleteAllRecords();
    Console.WriteLine($"Successfully deleted {count} records");
}
catch (SqlException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Dependencies

These functions require:
- .NET Framework 4.6.1 or higher
- System.Data.SqlClient (for SQL Server)

For other databases (MySQL, PostgreSQL, SQLite), replace `SqlConnection` with the appropriate connection class:
- MySQL: `MySqlConnection` (from MySql.Data)
- PostgreSQL: `NpgsqlConnection` (from Npgsql)
- SQLite: `SQLiteConnection` (from System.Data.SQLite)

## Integration Example

Example of integrating into a Windows Service:

```csharp
public class GuardService : ServiceBase
{
    private readonly string _connectionString;

    public GuardService()
    {
        _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
    }

    public void ClearAnalysisQueue()
    {
        try
        {
            var repository = new LUFSAnalysisQueueRepository(_connectionString);
            int deletedCount = repository.DeleteAllRecords();
            EventLog.WriteEntry($"Cleared {deletedCount} records from LUFSAnalysisQueue", EventLogEntryType.Information);
        }
        catch (Exception ex)
        {
            EventLog.WriteEntry($"Error clearing queue: {ex.Message}", EventLogEntryType.Error);
        }
    }
}
```

## License

This code is provided as-is for use with the RDP-Guard project.
