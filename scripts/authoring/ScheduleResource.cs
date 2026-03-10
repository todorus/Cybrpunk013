using System.Linq;
using Godot;
using SurveillanceStategodot.scripts.domain.schedule;

namespace SurveillanceStategodot.scripts.authoring;

[GlobalClass]
public partial class ScheduleResource : Resource
{
    [Export] public ScheduleEntryResource[] Entries { get; set; } = [];

    public Schedule ToSchedule()
    {
        var entries = Entries.Select(e => e.ToScheduleEntry()).ToArray();
        return new Schedule(entries);
    }
}

