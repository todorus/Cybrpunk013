using System.Collections.Generic;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.operation;

namespace SurveillanceStategodot.scripts.domain.system;

public sealed class WorldState
{
    public double Time { get; private set; }

    public List<Site> Sites { get; } = new();
    public List<Character> Characters { get; } = new();
    public List<Assignment> Assignments { get; } = new();

    private readonly Dictionary<string, Site> _sitesById = new();
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

    public void RegisterCharacter(Character character)
    {
        if (!Characters.Contains(character))
        {
            Characters.Add(character);
        }
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
}