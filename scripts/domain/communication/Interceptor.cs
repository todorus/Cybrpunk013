using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.communication;

public sealed class Interceptor
{
    public string Id { get; }
    public InterceptorAttachmentLevel AttachmentLevel { get; }
    public Character? AttachedCharacter { get; init; }
    public Site? AttachedSite { get; init; }
    public string? AttachedBlockId { get; init; }

    public HashSet<CommunicationType> SupportedTypes { get; } = new();
    public int Strength { get; set; } = 1;

    public Interceptor(string id, InterceptorAttachmentLevel attachmentLevel)
    {
        Id = id;
        AttachmentLevel = attachmentLevel;
    }

    public bool Supports(CommunicationType type) => SupportedTypes.Contains(type);
}
