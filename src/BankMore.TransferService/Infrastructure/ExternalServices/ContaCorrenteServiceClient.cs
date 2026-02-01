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

        var endpoint = $"{_httpClient.BaseAddress}api/Movimento";
        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
        
        _logger.LogInformation(
            "\n========== CHAMADA HTTP ==========\n" +
            "Método: POST\n" +
            "Endpoint: {Endpoint}\n" +
            "Headers:\n" +
            "  - Authorization: Bearer {Token}\n" +
            "  - Content-Type: application/json\n" +
            "Body (JSON):\n{Json}\n" +
            "==================================",
            endpoint, 
            authorizationToken.Substring(0, Math.Min(20, authorizationToken.Length)) + "...",
            requestJson);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Movimento", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "\n========== RESPOSTA HTTP ==========\n" +
                    "Status: {StatusCode} {ReasonPhrase}\n" +
                    "Movimento criado com sucesso\n" +
                    "===================================",
                    (int)response.StatusCode, response.ReasonPhrase);
                return;
            }

            // Tratar erros
            var errorContent = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning(
                    "\n========== RESPOSTA HTTP (ERRO) ==========\n" +
                    "Status: 403 Forbidden\n" +
                    "Erro: Token inválido ou expirado\n" +
                    "=========================================");
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
                        _logger.LogWarning(
                            "\n========== RESPOSTA HTTP (ERRO) ==========\n" +
                            "Status: 400 Bad Request\n" +
                            "Mensagem: {Message}\n" +
                            "Tipo: {FailureType}\n" +
                            "Body: {ErrorContent}\n" +
                            "=========================================",
                            errorResponse.Message, errorResponse.FailureType, errorContent);
                        throw new TransferenciaException(errorResponse.Message, errorResponse.FailureType);
                    }
                }
                catch (JsonException)
                {
                    // Se não conseguir deserializar, lançar erro genérico
                }
            }

            _logger.LogError(
                "\n========== RESPOSTA HTTP (ERRO) ==========\n" +
                "Status: {StatusCode}\n" +
                "Body: {ErrorContent}\n" +
                "=========================================",
                (int)response.StatusCode, errorContent);
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

    public async Task<Guid> ResolveAccountIdAsync(string numeroConta, string authorizationToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authorizationToken}");

        var endpoint = $"{_httpClient.BaseAddress}api/Conta/resolve";
        var requestBody = new { numeroConta };
        var requestJson = JsonSerializer.Serialize(requestBody);
        
        _logger.LogInformation(
            "\n========== CHAMADA HTTP (RESOLVE) ==========\n" +
            "Método: POST\n" +
            "Endpoint: {Endpoint}\n" +
            "Body: {Json}\n" +
            "===========================================",
            endpoint, requestJson);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Conta/resolve", requestBody);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                var contaId = accountData.GetProperty("contaId").GetGuid();
                
                _logger.LogInformation(
                    "\n========== RESPOSTA HTTP (RESOLVE) ==========\n" +
                    "Status: 200 OK\n" +
                    "ContaId: {ContaId}\n" +
                    "NumeroConta: {NumeroConta}\n" +
                    "============================================",
                    contaId, numeroConta);
                
                return contaId;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "\n========== RESPOSTA HTTP (ERRO) ==========\n" +
                "Status: {StatusCode}\n" +
                "Body: {Body}\n" +
                "=========================================",
                (int)response.StatusCode, errorContent);
            throw new TransferenciaException($"Conta {numeroConta} não encontrada", "INVALID_ACCOUNT");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ Falha na comunicação com Account Service");
            throw new TransferenciaException("Falha na comunicação com Account Service", "ACCOUNT_SERVICE_UNAVAILABLE", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "⏱️ Timeout ao chamar Account Service");
            throw new TransferenciaException("Timeout ao comunicar com Account Service", "ACCOUNT_SERVICE_TIMEOUT", ex);
        }
    }
}
