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
    public List<Character> Operators { get; } = new();
    public List<Assignment> Assignments { get; } = new();
    public List<Plot> Plots { get; } = new();

    // Vision sources are authoritative runtime state owned by WorldState.
    private readonly Dictionary<string, VisionSource> _visionSourcesById = new();
    public IReadOnlyDictionary<string, VisionSource> VisionSources => _visionSourcesById;

    public event Action<VisionSource>? VisionSourceAdded;
    public event Action<VisionSource>? VisionSourceRemoved;

    private readonly Dictionary<string, Site> _sitesById = new();
    private readonly Dictionary<string, Character> _charactersById = new();
    public IReadOnlyDictionary<string, Site> SitesById => _sitesById;

    private readonly Dictionary<string, Assignment> _assignmentsById = new();
    // NOTE: operation id index is kept updated via UpdateOperationIndex / RemoveOperationIndex.
    private readonly Dictionary<string, Assignment> _assignmentsByOperationId = new();
    // Index: target character id -> tail assignment (one tail per character at a time).
    private readonly Dictionary<string, Assignment> _assignmentsByTargetCharacterId = new();

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

    public void RegisterOperator(Character character)
    {
        character.IsOperator = true;

        if (_charactersById.ContainsKey(character.Id))
            return;

        _charactersById.Add(character.Id, character);
        Characters.Add(character);
        Operators.Add(character);
    }

    public void RegisterAssignment(Assignment assignment)
    {
        if (!Assignments.Contains(assignment))
        {
            Assignments.Add(assignment);
        }

        _assignmentsById[assignment.Id] = assignment;

        if (assignment.CurrentOperation != null)
            _assignmentsByOperationId[assignment.CurrentOperation.Id] = assignment;

        if (assignment.TargetCharacter != null)
            _assignmentsByTargetCharacterId[assignment.TargetCharacter.Id] = assignment;
    }

    /// <summary>
    /// Call whenever assignment.CurrentOperation changes so the lookup stays consistent.
    /// </summary>
    public void UpdateAssignmentOperationIndex(Assignment assignment, string? oldOperationId)
    {
        if (oldOperationId != null)
            _assignmentsByOperationId.Remove(oldOperationId);

        if (assignment.CurrentOperation != null)
            _assignmentsByOperationId[assignment.CurrentOperation.Id] = assignment;
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
    /// Returns the active TailCharacter assignment targeting the given character, if any.
    /// </summary>
    public bool TryGetTailAssignmentForTarget(string targetCharacterId, out Assignment? assignment)
    {
        if (_assignmentsByTargetCharacterId.TryGetValue(targetCharacterId, out var found) &&
            found.Phase is not (AssignmentPhase.Completed or AssignmentPhase.Cancelled or AssignmentPhase.Failed))
        {
            assignment = found;
            return true;
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

            if (assignment.Phase is AssignmentPhase.Completed or AssignmentPhase.Cancelled or AssignmentPhase.Failed)
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

