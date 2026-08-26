namespace PatchPony.IntegrationTests;
public sealed class ControlledPilotArtifactTests
{
 [Fact] public void PilotRunbook_IsNarrowGatedAndHasAbortCriteria()
 { var root=FindRepositoryRoot(); var doc=File.ReadAllText(Path.Combine(root,"docs","controlled-pilot-runbook.md")); var evidence=File.ReadAllText(Path.Combine(root,"evaluations","i14-controlled-pilot.json"));
 Assert.Contains("PatchPony",doc,StringComparison.Ordinal); Assert.Contains("VocaVid",doc,StringComparison.Ordinal); Assert.Contains("Entry gates",doc,StringComparison.Ordinal); Assert.Contains("Abort criteria",doc,StringComparison.Ordinal); Assert.Contains("kill switch",doc,StringComparison.Ordinal); Assert.Contains("automatic merge",doc,StringComparison.Ordinal); Assert.Contains("blocked-awaiting-operator-approval-and-external-evidence",evidence,StringComparison.Ordinal); }
 static string FindRepositoryRoot(){for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent)if(File.Exists(Path.Combine(d.FullName,"PLAN.md")))return d.FullName;throw new DirectoryNotFoundException();}
}