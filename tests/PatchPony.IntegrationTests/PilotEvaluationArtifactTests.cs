namespace PatchPony.IntegrationTests;
public sealed class PilotEvaluationArtifactTests
{
 [Fact] public void EvaluationTemplate_IsAggregateOnlyAndPrioritizesSafetyFirst()
 { var root=FindRepositoryRoot(); var doc=File.ReadAllText(Path.Combine(root,"docs","pilot-evaluation-v1-1.md")); var template=File.ReadAllText(Path.Combine(root,"evaluations","i14-pilot-evaluation.json"));
 Assert.Contains("Do not add ticket text",doc,StringComparison.Ordinal); Assert.Contains("P0",doc,StringComparison.Ordinal); Assert.Contains("trust boundary",doc,StringComparison.Ordinal); Assert.Contains("expand one bounded dimension",doc,StringComparison.Ordinal); Assert.Contains("blocked-awaiting-i14-12-aggregate-evidence",template,StringComparison.Ordinal); Assert.Contains("allowedDecisions",template,StringComparison.Ordinal); }
 static string FindRepositoryRoot(){for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent)if(File.Exists(Path.Combine(d.FullName,"PLAN.md")))return d.FullName;throw new DirectoryNotFoundException();}
}