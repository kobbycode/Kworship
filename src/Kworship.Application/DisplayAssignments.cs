namespace Kworship.Application;

public sealed record DisplayInfo(string Id,string Name,bool IsPrimary,int Left,int Top,int Width,int Height,bool StableIdentity=true)
{
    public string Label=>$"{Name} · {Width}×{Height}{(IsPrimary?" · operator/primary":"")}";
}
public sealed record DisplayAssignments(string? CongregationId=null,string? StageId=null);
public interface IDisplayCatalog:IDisposable
{
    IReadOnlyList<DisplayInfo> GetDisplays();
    event EventHandler? Changed;
}
public static class OutputAssignmentRules
{
    public static void Validate(DisplayAssignments assignments,IReadOnlyList<DisplayInfo> displays)
    {
        if(assignments.CongregationId is { } a && a==assignments.StageId)
            throw new ArgumentException("Assign congregation and stage to different displays, or use a windowed output.");
        foreach(var id in new[]{assignments.CongregationId,assignments.StageId}.Where(id=>id is not null))
            if(displays.Count(display=>display.Id==id)!=1)throw new ArgumentException("An assigned display is disconnected or has an ambiguous identity. Refresh the display list.");
    }
    public static bool MustSuspend(DisplayInfo? active,IReadOnlyList<DisplayInfo> displays)
    {
        // Windowed outputs also stop on topology changes; Windows may otherwise relocate them.
        if(active is null)return true;
        return displays.Count(display=>display.Id==active.Id)!=1 || displays.Single(display=>display.Id==active.Id)!=active;
    }
}
