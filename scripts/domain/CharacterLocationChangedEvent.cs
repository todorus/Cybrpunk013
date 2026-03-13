using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain;

public sealed record CharacterLocationChangedEvent(
    Character Character,
    CharacterLocationType PreviousLocation,
    CharacterLocationType NewLocation,
    double Time) : IDomainEvent;

