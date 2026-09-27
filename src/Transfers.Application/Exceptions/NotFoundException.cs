namespace Transfers.Application.Exceptions;

public sealed class NotFoundException(string resource, object id)
    : Exception($"{resource} '{id}' não encontrada.")
{
    public string Resource { get; } = resource;

    public object ResourceId { get; } = id;
}
