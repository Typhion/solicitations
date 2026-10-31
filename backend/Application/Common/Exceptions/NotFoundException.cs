namespace Application.Common.Exceptions;

public sealed class NotFoundException(string name, object key) : Exception($"\"{name}\" ({key}) was not found.")
{
    /// <summary>Entity name, e.g. "Solicitation".</summary>
    public string Name { get; } = name;
    public object Key { get; } = key;
}