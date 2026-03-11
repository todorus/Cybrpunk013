using System;
using Godot;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.vision;

public sealed class VisionSource
{
    public string Id { get; }
    public Character? Owner { get; }
    public VisionSourceType Type { get; }
    public float Range { get; private set; }
    public Vector3 WorldPosition { get; private set; }
    public Site? SiteContext { get; private set; }
    public bool IsMapVisible { get; private set; }
    public bool IsActive { get; private set; } = true;

    public event Action<VisionSource>? Changed;
    public event Action<VisionSource>? Deactivated;

    public VisionSource(string id, Character? owner, VisionSourceType type, float range, bool isMapVisible = false)
    {
        Id = id;
        Owner = owner;
        Type = type;
        Range = range;
        IsMapVisible = isMapVisible;
    }

    public void SetWorldPosition(Vector3 position)
    {
        WorldPosition = position;
        Changed?.Invoke(this);
    }

    public void SetRange(float range)
    {
        Range = range;
        Changed?.Invoke(this);
    }

    public void SetSiteContext(Site? site)
    {
        SiteContext = site;
        Changed?.Invoke(this);
    }

    public void SetMapVisible(bool visible)
    {
        IsMapVisible = visible;
        Changed?.Invoke(this);
    }

    public void Deactivate()
    {
        IsActive = false;
        Deactivated?.Invoke(this);
    }
}