using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.communication;

public sealed class Communication
{
    public string Id { get; }
    public CommunicationType Type { get; }
    public Character Sender { get; }
    public List<Character> Recipients { get; } = new();
    public Operation SourceOperation { get; }
    public Site? SourceSite { get; }
    public double Time { get; }
    public int EncryptionLevel { get; set; }
    public List<string> PayloadTags { get; } = new();

    public Communication(
        string id,
        CommunicationType type,
        Character sender,
        Operation sourceOperation,
        Site? sourceSite,
        double time)
    {
        Id = id;
        Type = type;
        Sender = sender;
        SourceOperation = sourceOperation;
        SourceSite = sourceSite;
        Time = time;
    }
}