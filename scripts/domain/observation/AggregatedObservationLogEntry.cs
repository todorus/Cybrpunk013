namespace SurveillanceStategodot.scripts.domain.observation;

public sealed class AggregatedObservationLogEntry
{
    public ObservationLogKey Key { get; }
    public string SiteLabel { get; }
    public string CharacterLabel { get; }
    public string OperationLabel { get; }

    public int Count { get; private set; }
    public double FirstSeenTime { get; private set; }
    public double LastSeenTime { get; private set; }

    public AggregatedObservationLogEntry(
        ObservationLogKey key,
        string siteLabel,
        string characterLabel,
        string operationLabel,
        double firstSeenTime)
    {
        Key = key;
        SiteLabel = siteLabel;
        CharacterLabel = characterLabel;
        OperationLabel = operationLabel;
        Count = 1;
        FirstSeenTime = firstSeenTime;
        LastSeenTime = firstSeenTime;
    }

    public void AddOccurrence(double time)
    {
        Count++;

        if (time < FirstSeenTime)
            FirstSeenTime = time;

        if (time > LastSeenTime)
            LastSeenTime = time;
    }
}