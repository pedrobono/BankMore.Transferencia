using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BankMore.TransferService.Infrastructure.HealthChecks;

public class AccountServiceHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AccountServiceHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountServiceUrl = _configuration["AccountService:BaseUrl"];
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync($"{accountServiceUrl}/health", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Account Service is reachable");
            }

            return HealthCheckResult.Degraded($"Account Service returned {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            // Account Service não está disponível, mas isso não deve impedir o Transfer Service de funcionar
            return HealthCheckResult.Degraded("Account Service is unreachable (will retry on transfer requests)");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded("Account Service timeout (will retry on transfer requests)");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Account Service check failed", ex);
        }
    }
}
