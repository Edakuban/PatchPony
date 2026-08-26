namespace PatchPony.IntegrationTests;
public sealed class ThreatModelReviewArtifactTests
{
 [Fact] public void ThreatModelReview_MapsCoreThreatsToControlsAndKeepsPilotBlockersVisible()
 { var root=FindRepositoryRoot(); var model=File.ReadAllText(Path.Combine(root,"docs","threat-model.md")); var review=File.ReadAllText(Path.Combine(root,"docs","security-review-i14.md"));
 Assert.Contains("Prompt injection",model,StringComparison.Ordinal); Assert.Contains("Sandbox escape",model,StringComparison.Ordinal); Assert.Contains("Supply-chain",model,StringComparison.Ordinal); Assert.Contains("Data disclosure to model provider",model,StringComparison.Ordinal); Assert.Contains("not a production-launch approval",review,StringComparison.Ordinal); Assert.Contains("I14.9",review,StringComparison.Ordinal); Assert.Contains("AppArmor/seccomp",review,StringComparison.Ordinal); Assert.Contains("Do not enable",review,StringComparison.Ordinal); }
 static string FindRepositoryRoot(){for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent)if(File.Exists(Path.Combine(d.FullName,"PLAN.md")))return d.FullName;throw new DirectoryNotFoundException();}
}