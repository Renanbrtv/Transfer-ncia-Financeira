namespace Transfers.Domain.Transfers;

/// <summary>
/// Transições permitidas:
/// <code>
/// Scheduled  -> Processing -> Completed | Failed
/// Scheduled  -> Cancelled
/// (imediata)    Processing -> Completed | Failed
/// </code>
/// </summary>
public enum TransferStatus
{
    Scheduled = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
