using BankMore.TransferService.Application.DTOs;
using BankMore.TransferService.Application.Interfaces;
using BankMore.TransferService.Domain.Exceptions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BankMore.TransferService.Infrastructure.ExternalServices;

public class ContaCorrenteServiceClient : IContaCorrenteServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContaCorrenteServiceClient> _logger;

    public ContaCorrenteServiceClient(HttpClient httpClient, ILogger<ContaCorrenteServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task CreateMovementAsync(CriarMovimentoRequest request, string authorizationToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authorizationToken}");

        _logger.LogInformation("📡 Chamando Account Service - POST /movements (Tipo: {Type}, Valor: R$ {Value:N2})",
            request.Tipo, request.Valor);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/movements", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Movimento criado com sucesso no Account Service");
                return;
            }

            // Tratar erros
            var errorContent = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("🚫 Token inválido ou expirado");
                throw new TransferenciaException("Token inválido ou expirado", "UNAUTHORIZED");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErroResponse>(errorContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (errorResponse != null)
                    {
                        _logger.LogWarning("⚠️ Erro do Account Service: {Message} (Tipo: {FailureType})",
                            errorResponse.Message, errorResponse.FailureType);
                        throw new TransferenciaException(errorResponse.Message, errorResponse.FailureType);
                    }
                }
                catch (JsonException)
                {
                    // Se não conseguir deserializar, lançar erro genérico
                }
            }

            _logger.LogError("❌ Erro ao chamar Account Service. Status: {StatusCode}",
                response.StatusCode);
            throw new TransferenciaException($"Erro ao comunicar com Account Service: {response.StatusCode}", "ACCOUNT_SERVICE_ERROR");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ Falha na comunicação com Account Service");
            throw new TransferenciaException("Falha na comunicação com Account Service", "ACCOUNT_SERVICE_UNAVAILABLE", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "⏱️ Timeout ao chamar Account Service (>{Timeout}s)", 30);
            throw new TransferenciaException("Timeout ao comunicar com Account Service", "ACCOUNT_SERVICE_TIMEOUT", ex);
        }
    }
}
