namespace PatchPony.IntegrationTests;
public sealed class LoadTestArtifactTests
{
 [Fact] public void K6Tests_AreLocalOnlyAndCoverParallelAndRateLimitScenarios()
 { var root=FindRepositoryRoot(); var main=File.ReadAllText(Path.Combine(root,"deploy","load","k6-gateway.js")); var rate=File.ReadAllText(Path.Combine(root,"deploy","load","k6-rate-limit.js")); var evidence=File.ReadAllText(Path.Combine(root,"evaluations","i14-load-and-concurrency.json"));
 Assert.Contains("ACKNOWLEDGE_NON_PRODUCTION",main,StringComparison.Ordinal); Assert.Contains("localhost|127",main,StringComparison.Ordinal); Assert.Contains("rest_parallel",main,StringComparison.Ordinal); Assert.Contains("mcp_parallel",main,StringComparison.Ordinal); Assert.Contains("constant-arrival-rate",main,StringComparison.Ordinal); Assert.Contains("p(95)<500",main,StringComparison.Ordinal); Assert.Contains("status===429",rate,StringComparison.Ordinal); Assert.Contains("pending-isolated-pilot-execution",evidence,StringComparison.Ordinal); Assert.DoesNotContain(".env",main,StringComparison.Ordinal); }
 static string FindRepositoryRoot(){for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent)if(File.Exists(Path.Combine(d.FullName,"PLAN.md")))return d.FullName;throw new DirectoryNotFoundException();}
}