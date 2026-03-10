using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed record CharacterEnteredSiteEvent(
    Character Character,
    Site Site,
    Operation? CurrentOperation,
    double Time) : IDomainEvent;

public sealed record CharacterExitedSiteEvent(
    Character Character,
    Site Site,
    Operation? CurrentOperation,
    double Time) : IDomainEvent;