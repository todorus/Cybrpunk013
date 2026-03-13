using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.observation;

public sealed class AggregatedObservationLogEntry
{
    public ObservationLogKey Key { get; }
    public ObservationType ObservationType => Key.ObservationType;
    public string SiteLabel { get; }
    public string CharacterLabel { get; }
    public string OperationLabel { get; }

    /// <summary>
    /// The highest (worst) compliance level observed across all occurrences.
    /// NonCompliant > Suspicious > Compliant.
    /// </summary>
    public ComplianceType ComplianceType { get; private set; }

    public int Count { get; private set; }
    public double FirstSeenTime { get; private set; }
    public double LastSeenTime { get; private set; }

    public AggregatedObservationLogEntry(
        ObservationLogKey key,
        string siteLabel,
        string characterLabel,
        string operationLabel,
        double firstSeenTime,
        ComplianceType complianceType = ComplianceType.Compliant)
    {
        Key = key;
        SiteLabel = siteLabel;
        CharacterLabel = characterLabel;
        OperationLabel = operationLabel;
        Count = 1;
        FirstSeenTime = firstSeenTime;
        LastSeenTime = firstSeenTime;
        ComplianceType = complianceType;
    }

    public void AddOccurrence(double time, ComplianceType complianceType)
    {
        Count++;
        if (time < FirstSeenTime) FirstSeenTime = time;
        if (time > LastSeenTime)  LastSeenTime  = time;
        // Escalate compliance level — never downgrade.
        if (complianceType > ComplianceType) ComplianceType = complianceType;
    }
}