namespace SurveillanceStategodot.scripts.domain.observation;

public sealed class Observation
{
    public string Id { get; }
    public string? SiteId { get; }
    public string? CharacterId { get; }
    public string? OperationId { get; }
    public double Time { get; }

    // Optional label snapshots for UI convenience.
    public string? SiteLabelSnapshot { get; }
    public string? CharacterLabelSnapshot { get; }
    public string? OperationLabelSnapshot { get; }

    public Observation(
        string id,
        string? siteId,
        string? characterId,
        string? operationId,
        double time,
        string? siteLabelSnapshot = null,
        string? characterLabelSnapshot = null,
        string? operationLabelSnapshot = null)
    {
        Id = id;
        SiteId = siteId;
        CharacterId = characterId;
        OperationId = operationId;
        Time = time;
        SiteLabelSnapshot = siteLabelSnapshot;
        CharacterLabelSnapshot = characterLabelSnapshot;
        OperationLabelSnapshot = operationLabelSnapshot;
    }
}