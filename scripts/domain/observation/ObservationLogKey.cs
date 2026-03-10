namespace SurveillanceStategodot.scripts.domain.observation;

public readonly record struct ObservationLogKey(
    string? SiteId,
    string? CharacterId,
    string? ActivityId,
    ObservationType ObservationType
);
