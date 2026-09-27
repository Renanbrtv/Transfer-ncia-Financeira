using Microsoft.AspNetCore.Mvc;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;

namespace Transfers.Api.Controllers;

/// <summary>Transferências imediatas e agendadas.</summary>
[ApiController]
[Route("api/transfers")]
public sealed class TransfersController(TransferService transferService) : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const string IdempotencyReplayedHeader = "Idempotency-Replayed";

    /// <summary>Realiza uma transferência imediata.</summary>
    /// <remarks>
    /// Débito e crédito acontecem na mesma transação. Se alguma regra de negócio impedir a transferência
    /// (saldo, limites, status das contas), ela é registrada como <c>Failed</c> e a resposta é 422 com o
    /// motivo e o <c>transferId</c>.
    ///
    /// Envie o header opcional <c>Idempotency-Key</c> para repetir a requisição com segurança: a mesma chave
    /// devolve a transferência original em vez de criar outra.
    /// </remarks>
    /// <param name="request">Origem, destino e valor.</param>
    /// <param name="idempotencyKey">Chave única da operação (até 100 caracteres), gerada pelo cliente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="201">Transferência concluída.</response>
    /// <response code="200">Repetição de uma requisição já processada (mesma Idempotency-Key).</response>
    /// <response code="400">Dados inválidos: valor menor ou igual a zero, origem igual ao destino etc.</response>
    /// <response code="404">Conta de origem ou destino inexistente.</response>
    /// <response code="409">Idempotency-Key reutilizada com dados diferentes.</response>
    /// <response code="422">Transferência rejeitada por regra de negócio (registrada como Failed).</response>
    [HttpPost]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransferRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await transferService.TransferAsync(request, idempotencyKey, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Agenda uma transferência para uma data/hora futura.</summary>
    /// <remarks>
    /// A transferência nasce com status <c>Scheduled</c>. Saldo, status das contas e limites são verificados
    /// novamente no momento da execução; se alguma regra falhar, ela termina como <c>Failed</c>.
    /// Aceita o header opcional <c>Idempotency-Key</c>.
    /// </remarks>
    /// <param name="request">Origem, destino, valor e data/hora da execução.</param>
    /// <param name="idempotencyKey">Chave única da operação (até 100 caracteres), gerada pelo cliente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="201">Transferência agendada.</response>
    /// <response code="200">Repetição de uma requisição já processada (mesma Idempotency-Key).</response>
    /// <response code="400">Dados inválidos ou data no passado.</response>
    /// <response code="404">Conta de origem ou destino inexistente.</response>
    /// <response code="409">Idempotency-Key reutilizada com dados diferentes.</response>
    [HttpPost("scheduled")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Schedule(
        [FromBody] ScheduleTransferRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await transferService.ScheduleAsync(request, idempotencyKey, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Cancela uma transferência agendada.</summary>
    /// <remarks>Só transferências com status <c>Scheduled</c> podem ser canceladas.</remarks>
    /// <param name="id">Id da transferência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="200">Transferência cancelada.</response>
    /// <response code="404">Transferência inexistente.</response>
    /// <response code="409">A transferência não está mais agendada (Processing, Completed, Failed ou Cancelled).</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransferResponse>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await transferService.CancelAsync(id, cancellationToken));

    /// <summary>Consulta uma transferência pelo Id.</summary>
    /// <param name="id">Id da transferência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="200">Transferência encontrada.</response>
    /// <response code="404">Transferência inexistente.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await transferService.GetByIdAsync(id, cancellationToken));

    // Tradução do resultado da aplicação para HTTP; nenhuma regra de negócio é decidida aqui.
    private IActionResult ToActionResult(TransferOperationResult result)
    {
        var transfer = result.Transfer;

        if (result.IsReplay)
        {
            Response.Headers[IdempotencyReplayedHeader] = "true";
        }

        if (transfer.Status == TransferStatus.Failed)
        {
            return RejectedTransfer(transfer);
        }

        return result.IsReplay
            ? Ok(transfer)
            : CreatedAtAction(nameof(GetById), new { id = transfer.Id }, transfer);
    }

    private ObjectResult RejectedTransfer(TransferResponse transfer)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Transferência rejeitada",
            detail: transfer.FailureMessage);

        problem.Extensions["code"] = transfer.FailureCode;
        problem.Extensions["transferId"] = transfer.Id;
        problem.Extensions["transfer"] = transfer;

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            ContentTypes = { "application/problem+json" }
        };
    }
}
