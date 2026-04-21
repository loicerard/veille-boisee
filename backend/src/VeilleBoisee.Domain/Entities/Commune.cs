using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Domain.Entities;

public sealed record Commune(CodeInsee CodeInsee, string Name)
{
    public static Commune Create(CodeInsee codeInsee, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Commune(codeInsee, name);
    }
}
