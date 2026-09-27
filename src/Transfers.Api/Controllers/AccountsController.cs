using Microsoft.AspNetCore.Mvc;
using Transfers.Application.Accounts;
using Transfers.Application.Transfers;

namespace Transfers.Api.Controllers;

/// <summary>Consulta de contas.</summary>
[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(AccountService accountService) : ControllerBase
{
    /// <summary>Lista todas as contas.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="200">Contas cadastradas.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await accountService.ListAsync(cancellationToken));

    /// <summary>Consulta uma conta pelo Id.</summary>
    /// <param name="id">Id da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="200">Conta encontrada.</response>
    /// <response code="404">Conta inexistente.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await accountService.GetByIdAsync(id, cancellationToken));

    /// <summary>Lista as transferências enviadas e recebidas pela conta, das mais recentes para as mais antigas.</summary>
    /// <param name="id">Id da conta.</param>
    /// <param name="take">Quantidade máxima de itens (1 a 100).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="200">Transferências da conta.</response>
    /// <response code="404">Conta inexistente.</response>
    [HttpGet("{id:int}/transfers")]
    [ProducesResponseType<IReadOnlyList<TransferResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransferResponse>>> ListTransfers(
        int id,
        CancellationToken cancellationToken,
        [FromQuery] int take = 20) =>
        Ok(await accountService.ListTransfersAsync(id, take, cancellationToken));
}
