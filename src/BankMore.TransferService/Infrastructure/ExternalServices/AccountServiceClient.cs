using BankMore.TransferService.Application.DTOs;
using BankMore.TransferService.Application.Interfaces;
using BankMore.TransferService.Domain.Exceptions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BankMore.TransferService.Infrastructure.ExternalServices;

public class AccountServiceClient : IAccountServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AccountServiceClient> _logger;

    public AccountServiceClient(HttpClient httpClient, ILogger<AccountServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task CreateMovementAsync(CreateMovementRequest request, string authorizationToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authorizationToken}");

        _logger.LogInformation("Chamando Account Service - POST /movements. RequestId: {RequestId}, Type: {Type}",
            request.RequestId, request.Type);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/movements", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Movimento criado com sucesso. RequestId: {RequestId}", request.RequestId);
                return;
            }

            // Tratar erros
            var errorContent = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Token inválido ou expirado. RequestId: {RequestId}", request.RequestId);
                throw new TransferException("Token inválido ou expirado", "UNAUTHORIZED");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (errorResponse != null)
                    {
                        _logger.LogWarning("Erro de validação do Account Service. RequestId: {RequestId}, FailureType: {FailureType}",
                            request.RequestId, errorResponse.FailureType);
                        throw new TransferException(errorResponse.Message, errorResponse.FailureType);
                    }
                }
                catch (JsonException)
                {
                    // Se não conseguir deserializar, lançar erro genérico
                }
            }

            _logger.LogError("Erro ao chamar Account Service. StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);
            throw new TransferException($"Erro ao comunicar com Account Service: {response.StatusCode}", "ACCOUNT_SERVICE_ERROR");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha na comunicação com Account Service. RequestId: {RequestId}", request.RequestId);
            throw new TransferException("Falha na comunicação com Account Service", "ACCOUNT_SERVICE_UNAVAILABLE", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ao chamar Account Service. RequestId: {RequestId}", request.RequestId);
            throw new TransferException("Timeout ao comunicar com Account Service", "ACCOUNT_SERVICE_TIMEOUT", ex);
        }
    }
}
