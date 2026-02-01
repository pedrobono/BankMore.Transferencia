using BankMore.TransferService.Application.Commands;
using BankMore.TransferService.Controllers.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankMore.TransferService.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TransfersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransfersController> _logger;

    public TransfersController(IMediator mediator, ILogger<TransfersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Efetua transferência entre contas
    /// </summary>
    /// <param name="request">Dados da transferência</param>
    /// <returns>204 No Content em caso de sucesso</returns>
    /// <response code="204">Transferência realizada com sucesso</response>
    /// <response code="400">Dados inválidos ou erro na transferência</response>
    /// <response code="403">Token inválido ou expirado</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferRequest request)
    {
        var originAccountId = GetAccountIdFromToken();
        var authToken = GetAuthorizationToken();

        _logger.LogInformation("Recebida requisição de transferência. RequestId: {RequestId}, Origin: {Origin}",
            request.RequestId, originAccountId);

        var command = new CreateTransferCommand(
            request.RequestId,
            request.DestinationAccountNumber,
            request.Value,
            originAccountId,
            authToken
        );

        await _mediator.Send(command);

        return NoContent();
    }

    private Guid GetAccountIdFromToken()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new UnauthorizedAccessException("Token inválido: accountId não encontrado");
        }

        return accountId;
    }

    private string GetAuthorizationToken()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new UnauthorizedAccessException("Token de autorização não encontrado");
        }

        return authHeader.Replace("Bearer ", "").Trim();
    }
}

public record ErrorResponse(string Message, string FailureType);
