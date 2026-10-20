using Kworship.Application;
using Kworship.Infrastructure.Sqlite;
using Xunit;
namespace Kworship.Tests;
public sealed class DisplayAssignmentTests
{
    private static DisplayInfo A=>new("stable-a","Projector",false,1920,0,1920,1080);
    private static DisplayInfo B=>new("stable-b","Stage",false,-1280,0,1280,720);
    [Fact]public void AssignmentsUseIdentityRatherThanEnumerationOrder()
    {OutputAssignmentRules.Validate(new(A.Id,B.Id),[B,A]);Assert.False(OutputAssignmentRules.MustSuspend(A,[B,A]));}
    [Fact]public void DisconnectAndResolutionOrPositionChangesRequireSuspension()
    {Assert.True(OutputAssignmentRules.MustSuspend(A,[B]));Assert.True(OutputAssignmentRules.MustSuspend(A,[A with{Width=3840},B]));Assert.True(OutputAssignmentRules.MustSuspend(A,[A with{Left=0},B]));}
    [Fact]public void DuplicateIdentityAndSharedOutputAssignmentsAreRejected()
    {Assert.Throws<ArgumentException>(()=>OutputAssignmentRules.Validate(new(A.Id,A.Id),[A,B]));Assert.Throws<ArgumentException>(()=>OutputAssignmentRules.Validate(new(A.Id),[A,A]));Assert.Throws<ArgumentException>(()=>OutputAssignmentRules.Validate(new("missing"),[A,B]));}
    [Fact]public void WindowedOutputsAreAllowedButSuspendOnTopologyEvents()
    {OutputAssignmentRules.Validate(new(),[A]);Assert.True(OutputAssignmentRules.MustSuspend(null,[A]));}
    [Fact]public void PreferencesRoundTripWithoutAddingLiveState()
    {
        var directory=Path.Combine(Path.GetTempPath(),"kworship-display-"+Guid.NewGuid());
        try{var file=new WorkspaceStateFile(Path.Combine(directory,"workspace.json"));file.Save(new(Displays:new(A.Id,B.Id)));Assert.Equal(new DisplayAssignments(A.Id,B.Id),file.Load()!.Displays);}
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
}
