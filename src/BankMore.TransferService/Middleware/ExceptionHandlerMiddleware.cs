using BankMore.TransferService.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace BankMore.TransferService.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            ValidationException validationEx => new
            {
                statusCode = (int)HttpStatusCode.BadRequest,
                message = string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage)),
                failureType = "VALIDATION_ERROR"
            },
            TransferenciaException transferEx when transferEx is CompensacaoFalhaException => new
            {
                statusCode = (int)HttpStatusCode.InternalServerError,
                message = transferEx.Message,
                failureType = transferEx.FailureType
            },
            TransferenciaException transferEx => new
            {
                statusCode = (int)HttpStatusCode.BadRequest,
                message = transferEx.Message,
                failureType = transferEx.FailureType
            },
            UnauthorizedAccessException => new
            {
                statusCode = (int)HttpStatusCode.Forbidden,
                message = exception.Message,
                failureType = "UNAUTHORIZED"
            },
            _ => new
            {
                statusCode = (int)HttpStatusCode.InternalServerError,
                message = "Erro interno do servidor",
                failureType = "INTERNAL_ERROR"
            }
        };

        response.StatusCode = errorResponse.statusCode;

        _logger.LogError(exception, "Erro capturado pelo middleware. StatusCode: {StatusCode}, FailureType: {FailureType}",
            errorResponse.statusCode, errorResponse.failureType);

        var jsonResponse = JsonSerializer.Serialize(new
        {
            message = errorResponse.message,
            failureType = errorResponse.failureType
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await response.WriteAsync(jsonResponse);
    }
}
