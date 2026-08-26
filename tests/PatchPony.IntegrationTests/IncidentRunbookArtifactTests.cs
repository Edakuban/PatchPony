namespace PatchPony.IntegrationTests;
public sealed class IncidentRunbookArtifactTests
{
 [Fact] public void KillSwitch_IsReversibleAndDoesNotDeleteOperationalData()
 { var root=FindRepositoryRoot(); var script=File.ReadAllText(Path.Combine(root,"deploy","host","kill-switch.sh")); var doc=File.ReadAllText(Path.Combine(root,"docs","incident-recovery-runbooks.md"));
 Assert.Contains("DISABLE_PATCHPONY_EXECUTION",script,StringComparison.Ordinal); Assert.Contains("ENABLE_PATCHPONY_AFTER_INCIDENT_REVIEW",script,StringComparison.Ordinal); Assert.Contains("compose stop gateway",script,StringComparison.Ordinal); Assert.Contains("systemctl --user stop patchpony-sandbox-worker.service",script,StringComparison.Ordinal); Assert.Contains("compose up -d gateway",script,StringComparison.Ordinal); Assert.DoesNotContain("docker volume",script,StringComparison.Ordinal); Assert.DoesNotContain("rm -",script,StringComparison.Ordinal); Assert.Contains("Host compromise",doc,StringComparison.Ordinal); Assert.Contains("Two people",doc,StringComparison.Ordinal); }
 static string FindRepositoryRoot(){for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent)if(File.Exists(Path.Combine(d.FullName,"PLAN.md")))return d.FullName;throw new DirectoryNotFoundException();}
}