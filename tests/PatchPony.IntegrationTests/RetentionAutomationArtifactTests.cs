namespace PatchPony.IntegrationTests;
public sealed class RetentionAutomationArtifactTests
{
 [Fact] public void RetentionJob_IsDryRunFirstBoundedAndExcludesUnsafeData()
 { var root=FindRepositoryRoot(); var script=File.ReadAllText(Path.Combine(root,"deploy","host","retention.sh")); var service=File.ReadAllText(Path.Combine(root,"deploy","systemd","patchpony-retention.service")); var timer=File.ReadAllText(Path.Combine(root,"deploy","systemd","patchpony-retention.timer")); var compose=File.ReadAllText(Path.Combine(root,"docker-compose.yml"));
 Assert.Contains("MODE=\"${1:-dry-run}\"",script); Assert.Contains("DELETE_ONLY_APPROVED_PATCHPONY_RETENTION",script); Assert.Contains("PATCHPONY_RETENTION_BATCH_SIZE",script); Assert.Contains("LIMIT ${BATCH_SIZE}",script); Assert.Contains("status = 'Closed'",script); Assert.Contains("interval '180 days'",script); Assert.DoesNotContain("postgres-data",script); Assert.DoesNotContain("docker volume",script); Assert.Contains("RandomizedDelaySec",timer); Assert.Contains("PATCHPONY_RETENTION_CONFIRM",service); Assert.Contains("max-size: \"10m\"",compose); }
 static string FindRepositoryRoot(){for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent)if(File.Exists(Path.Combine(d.FullName,"PLAN.md")))return d.FullName;throw new DirectoryNotFoundException();}
}