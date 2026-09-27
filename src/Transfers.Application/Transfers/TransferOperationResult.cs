namespace Transfers.Application.Transfers;

/// <param name="Transfer">Transferência resultante.</param>
/// <param name="IsReplay">True quando a requisição repetiu uma chave de idempotência já processada.</param>
public sealed record TransferOperationResult(TransferResponse Transfer, bool IsReplay)
{
    public static TransferOperationResult Created(TransferResponse transfer) => new(transfer, IsReplay: false);

    public static TransferOperationResult Replayed(TransferResponse transfer) => new(transfer, IsReplay: true);
}
