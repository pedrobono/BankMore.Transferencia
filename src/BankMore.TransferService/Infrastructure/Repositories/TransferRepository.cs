using BankMore.TransferService.Application.Interfaces;
using BankMore.TransferService.Domain.Entities;
using BankMore.TransferService.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BankMore.TransferService.Infrastructure.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly string _connectionString;

    public TransferRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Transfer?> GetByOriginAndRequestIdAsync(Guid originAccountId, string requestId)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        var sql = @"
            SELECT id, request_id, origin_account_id, destination_account_id, 
                   value, status, error_message, error_type, created_at, updated_at
            FROM transfers
            WHERE origin_account_id = @OriginAccountId AND request_id = @RequestId
            LIMIT 1";

        var row = await connection.QueryFirstOrDefaultAsync<TransferDto>(sql, new
        {
            OriginAccountId = originAccountId.ToString(),
            RequestId = requestId
        });

        return row?.ToEntity();
    }

    public async Task<Guid> CreateAsync(Transfer transfer)
    {
        using var connection = new SqliteConnection(_connectionString);

        var sql = @"
            INSERT INTO transfers (id, request_id, origin_account_id, destination_account_id, 
                                   value, status, error_message, error_type, created_at, updated_at)
            VALUES (@Id, @RequestId, @OriginAccountId, @DestinationAccountId, 
                    @Value, @Status, @ErrorMessage, @ErrorType, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(sql, new
        {
            Id = transfer.Id.ToString(),
            RequestId = transfer.RequestId,
            OriginAccountId = transfer.OriginAccountId.ToString(),
            DestinationAccountId = transfer.DestinationAccountId.ToString(),
            Value = transfer.Value,
            Status = transfer.Status.ToString(),
            ErrorMessage = transfer.ErrorMessage,
            ErrorType = transfer.ErrorType,
            CreatedAt = transfer.CreatedAt.ToString("O"),
            UpdatedAt = transfer.UpdatedAt.ToString("O")
        });

        return transfer.Id;
    }

    private class TransferDto
    {
        public string Id { get; set; } = string.Empty;
        public string Request_Id { get; set; } = string.Empty;
        public string Origin_Account_Id { get; set; } = string.Empty;
        public string Destination_Account_Id { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Error_Message { get; set; }
        public string? Error_Type { get; set; }
        public string Created_At { get; set; } = string.Empty;
        public string Updated_At { get; set; } = string.Empty;

        public Transfer ToEntity()
        {
            var transfer = (Transfer)Activator.CreateInstance(typeof(Transfer), true)!;
            
            typeof(Transfer).GetProperty(nameof(Transfer.Id))!.SetValue(transfer, Guid.Parse(Id));
            typeof(Transfer).GetProperty(nameof(Transfer.RequestId))!.SetValue(transfer, Request_Id);
            typeof(Transfer).GetProperty(nameof(Transfer.OriginAccountId))!.SetValue(transfer, Guid.Parse(Origin_Account_Id));
            typeof(Transfer).GetProperty(nameof(Transfer.DestinationAccountId))!.SetValue(transfer, Guid.Parse(Destination_Account_Id));
            typeof(Transfer).GetProperty(nameof(Transfer.Value))!.SetValue(transfer, Value);
            typeof(Transfer).GetProperty(nameof(Transfer.Status))!.SetValue(transfer, Enum.Parse<TransferStatus>(Status));
            typeof(Transfer).GetProperty(nameof(Transfer.ErrorMessage))!.SetValue(transfer, Error_Message);
            typeof(Transfer).GetProperty(nameof(Transfer.ErrorType))!.SetValue(transfer, Error_Type);
            typeof(Transfer).GetProperty(nameof(Transfer.CreatedAt))!.SetValue(transfer, DateTime.Parse(Created_At));
            typeof(Transfer).GetProperty(nameof(Transfer.UpdatedAt))!.SetValue(transfer, DateTime.Parse(Updated_At));

            return transfer;
        }
    }
}
