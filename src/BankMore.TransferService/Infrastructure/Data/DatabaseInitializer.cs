using DbUp;
using System.Reflection;

namespace BankMore.TransferService.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Initialize()
    {
        EnsureDatabaseExists();

        var upgrader = DeployChanges.To
            .SqliteDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), 
                script => script.Contains(".Migrations."))
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new Exception("Falha ao executar migrations do banco de dados", result.Error);
        }
    }

    private void EnsureDatabaseExists()
    {
        // SQLite cria o arquivo automaticamente na primeira conexão
        // Mas vamos garantir que o diretório existe
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(_connectionString);
        var dbPath = builder.DataSource;
        
        if (!string.IsNullOrEmpty(dbPath) && dbPath != ":memory:")
        {
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
