using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Orquestra.Application.Errors;

namespace Orquestra.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflito"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError("Erro não tratado do tipo {ExceptionType}. TraceId: {TraceId}",
                exception.GetType().Name, context.TraceIdentifier);
        }

        var detail = status == StatusCodes.Status500InternalServerError
            ? "Não foi possível concluir a operação."
            : exception.Message;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        }, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
