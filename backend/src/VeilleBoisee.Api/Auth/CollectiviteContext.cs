namespace VeilleBoisee.Api.Auth;

/// <summary>
/// Contexte de collectivité — bouchon en attendant l'authentification réelle.
/// À remplacer par une extraction depuis le JWT lors de la phase d'auth.
/// </summary>
public sealed class CollectiviteContext
{
    public string CollectiviteId { get; } = "DEV_COLLECTIVITE";

    // Liste vide = tous les signalements (mode démo — sera remplacé par les claims JWT)
    public IReadOnlyList<string> InseeCodes { get; } = Array.Empty<string>();
}
