using System;
using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.operation;
using SurveillanceStategodot.scripts.domain.plot;
using SurveillanceStategodot.scripts.domain.vision;

namespace SurveillanceStategodot.scripts.domain.system;

public sealed class WorldState
{
    public double Time { get; private set; }

    public List<Site> Sites { get; } = new();
    public List<Character> Characters { get; } = new();
    public List<Assignment> Assignments { get; } = new();
    public List<Plot> Plots { get; } = new();

    // Vision sources are authoritative runtime state owned by WorldState.
    private readonly Dictionary<string, VisionSource> _visionSourcesById = new();
    public IReadOnlyDictionary<string, VisionSource> VisionSources => _visionSourcesById;

    public event Action<VisionSource>? VisionSourceAdded;
    public event Action<VisionSource>? VisionSourceRemoved;

    private readonly Dictionary<string, Site> _sitesById = new();
    private readonly Dictionary<string, Character> _charactersById = new();
    
    private readonly Dictionary<string, Assignment> _assignmentsById = new();
    private readonly Dictionary<string, Assignment> _assignmentsByOperationId = new();

    public void AdvanceTime(double delta)
    {
        Time += delta;
    }

    public void RegisterSite(Site site)
    {
        if (_sitesById.ContainsKey(site.Id))
            return;

        _sitesById.Add(site.Id, site);
        Sites.Add(site);
    }

    public Site GetSite(string id)
    {
        return _sitesById[id];
    }
    
    public Character GetCharacter(string id)
    {
        return _charactersById[id];
    }

    public bool TryGetSite(string id, out Site? site)
    {
        return _sitesById.TryGetValue(id, out site);
    }

    public void RegisterCharacter(Character character)
    {
        if (_charactersById.ContainsKey(character.Id))
            return;

        _charactersById.Add(character.Id, character);
        Characters.Add(character);
    }

    public void RegisterAssignment(Assignment assignment)
    {
        if (!Assignments.Contains(assignment))
        {
            Assignments.Add(assignment);
        }

        _assignmentsById[assignment.Id] = assignment;
        _assignmentsByOperationId[assignment.Operation.Id] = assignment;
    }
    
    public void RegisterPlot(Plot plot)
    {
        if (!Plots.Contains(plot))
        {
            Plots.Add(plot);
        }
        
        foreach (var character in plot.Characters)
        {
            RegisterCharacter(character);
        }
    }

    public bool TryGetAssignmentByOperationId(string operationId, out Assignment assignment)
    {
        return _assignmentsByOperationId.TryGetValue(operationId, out assignment!);
    }

    public bool TryGetAssignmentByMovementId(string movementId, out Assignment? assignment)
    {
        foreach (var candidate in Assignments)
        {
            if (candidate.CurrentMovement?.Id == movementId)
            {
                assignment = candidate;
                return true;
            }
        }

        assignment = null;
        return false;
    }

    /// <summary>
    /// Returns true when a character has an assignment that is not yet completed or cancelled.
    /// </summary>
    public bool HasActiveAssignmentForCharacter(string characterId)
    {
        return GetActiveAssignmentForCharacter(characterId) != null;
    }

    /// <summary>
    /// Returns the first non-completed, non-cancelled assignment for the given character, or null.
    /// </summary>
    public Assignment? GetActiveAssignmentForCharacter(string characterId)
    {
        foreach (var assignment in Assignments)
        {
            if (assignment.Character?.Id != characterId)
                continue;

            if (assignment.Phase is AssignmentPhase.Completed or AssignmentPhase.Cancelled)
                continue;

            return assignment;
        }

        return null;
    }

    public void RegisterVisionSource(VisionSource source)
    {
        _visionSourcesById[source.Id] = source;
        VisionSourceAdded?.Invoke(source);
    }

    public void RemoveVisionSource(string id)
    {
        if (_visionSourcesById.TryGetValue(id, out var source))
        {
            _visionSourcesById.Remove(id);
            source.Deactivate();
            VisionSourceRemoved?.Invoke(source);
        }
    }

    public bool TryGetVisionSource(string id, out VisionSource? source)
    {
        return _visionSourcesById.TryGetValue(id, out source);
    }
}