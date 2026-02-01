using BankMore.TransferService.Application.Interfaces;
using BankMore.TransferService.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BankMore.TransferService.Infrastructure.Repositories;

public class TransferenciaRepository : ITransferenciaRepository
{
    private readonly string _connectionString;

    public TransferenciaRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Transferencia?> GetByOriginAndRequestIdAsync(Guid originAccountId, string requestId)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        // Verificar idempotência na tabela separada
        var idempotenciaKey = $"{originAccountId}:{requestId}";
        var sqlIdem = "SELECT resultado FROM idempotencia WHERE chave_idempotencia = @Key";
        
        var resultado = await connection.QueryFirstOrDefaultAsync<string>(sqlIdem, new { Key = idempotenciaKey });
        
        if (resultado != null)
        {
            // Já foi processado, retornar uma transferência fake para indicar idempotência
            var transfer = (Transferencia)Activator.CreateInstance(typeof(Transferencia), true)!;
            return transfer;
        }
        
        return null;
    }

    public async Task<Guid> CreateAsync(Transferencia transfer)
    {
        using var connection = new SqliteConnection(_connectionString);

        var sql = @"
            INSERT INTO transferencia (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, 
                                       datamovimento, valor)
            VALUES (@Id, @OriginAccountId, @DestinationAccountId, @DataMovimento, @Valor)";

        await connection.ExecuteAsync(sql, new
        {
            Id = transfer.IdTransferencia.ToString(),
            OriginAccountId = transfer.IdContaCorrenteOrigem.ToString(),
            DestinationAccountId = transfer.IdContaCorrenteDestino.ToString(),
            DataMovimento = transfer.DataMovimento,
            Valor = transfer.Valor
        });

        return transfer.IdTransferencia;
    }

    public async Task SaveIdempotenciaAsync(Guid originAccountId, string requestId, string requisicao, string resultado)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        var idempotenciaKey = $"{originAccountId}:{requestId}";
        var sql = @"
            INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado)
            VALUES (@Key, @Requisicao, @Resultado)";

        await connection.ExecuteAsync(sql, new
        {
            Key = idempotenciaKey,
            Requisicao = requisicao,
            Resultado = resultado
        });
    }

    private class TransferenciaDto
    {
        public string Idtransferencia { get; set; } = string.Empty;
        public string Idcontacorrente_Origem { get; set; } = string.Empty;
        public string Idcontacorrente_Destino { get; set; } = string.Empty;
        public string Datamovimento { get; set; } = string.Empty;
        public decimal Valor { get; set; }

        public Transferencia ToEntity()
        {
            var transfer = (Transferencia)Activator.CreateInstance(typeof(Transferencia), true)!;
            
            typeof(Transferencia).GetProperty(nameof(Transferencia.IdTransferencia))!.SetValue(transfer, Guid.Parse(Idtransferencia));
            typeof(Transferencia).GetProperty(nameof(Transferencia.IdContaCorrenteOrigem))!.SetValue(transfer, Guid.Parse(Idcontacorrente_Origem));
            typeof(Transferencia).GetProperty(nameof(Transferencia.IdContaCorrenteDestino))!.SetValue(transfer, Guid.Parse(Idcontacorrente_Destino));
            typeof(Transferencia).GetProperty(nameof(Transferencia.DataMovimento))!.SetValue(transfer, Datamovimento);
            typeof(Transferencia).GetProperty(nameof(Transferencia.Valor))!.SetValue(transfer, Valor);

            return transfer;
        }
    }
}
