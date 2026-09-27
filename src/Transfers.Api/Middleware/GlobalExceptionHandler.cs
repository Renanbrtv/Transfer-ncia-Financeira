using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Transfers.Application.Exceptions;
using Transfers.Domain.Exceptions;

namespace Transfers.Api.Middleware;

/// <summary>
/// Converte exceções em respostas ProblemDetails (RFC 7807) com status HTTP coerente.
/// Erros inesperados viram 500 sem expor detalhes internos.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const int SqlUniqueIndexViolation = 2601;
    private const int SqlUniqueConstraintViolation = 2627;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = Map(exception);

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Erro inesperado ao processar {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Requisição {Method} {Path} recusada ({Status}): {Message}",
                httpContext.Request.Method, httpContext.Request.Path, problem.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Map(Exception exception) => exception switch
    {
        DomainValidationException ex => Create(StatusCodes.Status400BadRequest, "Dados inválidos", ex.Message, ex.Code),
        InvalidTransferStateException ex => Create(StatusCodes.Status409Conflict, "Estado inválido para a operação", ex.Message, ex.Code),
        DomainException ex => Create(StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", ex.Message, ex.Code),
        NotFoundException ex => Create(StatusCodes.Status404NotFound, "Recurso não encontrado", ex.Message, "not_found"),
        IdempotencyConflictException ex => Create(StatusCodes.Status409Conflict, "Conflito de idempotência", ex.Message, "idempotency.conflict"),
        DbUpdateConcurrencyException => Create(
            StatusCodes.Status409Conflict,
            "Conflito de concorrência",
            "O registro foi alterado por outra operação. Consulte o estado atual e tente novamente.",
            "concurrency.conflict"),
        DbUpdateException { InnerException: SqlException { Number: SqlUniqueIndexViolation or SqlUniqueConstraintViolation } } => Create(
            StatusCodes.Status409Conflict,
            "Registro duplicado",
            "Já existe uma operação registrada com os mesmos dados únicos (ex.: chave de idempotência).",
            "duplicate"),
        BadHttpRequestException ex => Create(StatusCodes.Status400BadRequest, "Requisição inválida", ex.Message, "bad_request"),
        _ => Create(
            StatusCodes.Status500InternalServerError,
            "Erro interno",
            "Ocorreu um erro inesperado. Tente novamente mais tarde.",
            "internal_error")
    };

    private static ProblemDetails Create(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        return problem;
    }
}
