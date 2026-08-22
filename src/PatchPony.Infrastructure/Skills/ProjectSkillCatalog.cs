using System.Text;
using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Infrastructure.Skills;

public sealed record ProjectSkill(string Id);

public sealed record ProjectSkillContent(string Id, string Content);

public sealed class ProjectSkillCatalog
{
    private const string SkillDirectory = ".patchpony/skills";
    private const string SkillFileName = "SKILL.md";
    private const int MaxSkillCount = 100;
    private const int MaxSkillBytes = 64 * 1024;
    private static readonly Regex SkillIdPattern = new("^[a-z][a-z0-9-]{0,62}$", RegexOptions.CultureInvariant);
    private static readonly UTF8Encoding Utf8WithoutReplacement = new(false, true);

    private readonly ProjectPathResolver resolver;
    private readonly ProjectPathAccessService access;

    public ProjectSkillCatalog(ProjectPathResolver resolver, ProjectPathAccessService access)
    {
        this.resolver = resolver;
        this.access = access;
    }

    public Result<IReadOnlyList<ProjectSkill>> List()
    {
        var directory = resolver.Resolve(SkillDirectory);
        if (!directory.IsSuccess)
        {
            return Result<IReadOnlyList<ProjectSkill>>.Failure(directory.Error);
        }

        if (!Directory.Exists(directory.Value!.FullPath))
        {
            return Result<IReadOnlyList<ProjectSkill>>.Success([]);
        }

        try
        {
            var skills = new List<ProjectSkill>();
            foreach (var path in Directory.EnumerateDirectories(directory.Value.FullPath))
            {
                if (skills.Count == MaxSkillCount)
                {
                    return Result<IReadOnlyList<ProjectSkill>>.Failure(new DomainError("skills.limit_exceeded", "The project skill catalog exceeds its configured limit."));
                }

                var skillId = Path.GetFileName(path);
                if (!SkillIdPattern.IsMatch(skillId))
                {
                    continue;
                }

                var skillFile = access.ResolveAndAuthorize(GetSkillFilePath(skillId), ProjectPathAccess.Read);
                if (skillFile.IsSuccess && File.Exists(skillFile.Value!.FullPath))
                {
                    skills.Add(new ProjectSkill(skillId));
                }
            }

            return Result<IReadOnlyList<ProjectSkill>>.Success(skills.OrderBy(skill => skill.Id, StringComparer.Ordinal).ToArray());
        }
        catch (IOException)
        {
            return Result<IReadOnlyList<ProjectSkill>>.Failure(new DomainError("skills.unavailable", "The project skill catalog is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<IReadOnlyList<ProjectSkill>>.Failure(new DomainError("skills.unavailable", "The project skill catalog is unavailable."));
        }
    }

    public Result<ProjectSkillContent> Read(string skillId)
    {
        if (!SkillIdPattern.IsMatch(skillId))
        {
            return Result<ProjectSkillContent>.Failure(new DomainError("skills.invalid_id", "The requested skill identifier is invalid."));
        }

        var skillFile = access.ResolveAndAuthorize(GetSkillFilePath(skillId), ProjectPathAccess.Read);
        if (!skillFile.IsSuccess)
        {
            return Result<ProjectSkillContent>.Failure(skillFile.Error);
        }

        if (!File.Exists(skillFile.Value!.FullPath))
        {
            return Result<ProjectSkillContent>.Failure(new DomainError("skills.not_found", "The requested project skill was not found."));
        }

        try
        {
            var info = new FileInfo(skillFile.Value.FullPath);
            if (info.Length > MaxSkillBytes)
            {
                return Result<ProjectSkillContent>.Failure(new DomainError("skills.too_large", "The requested project skill exceeds the configured size limit."));
            }

            var bytes = File.ReadAllBytes(skillFile.Value.FullPath);
            if (bytes.Length > MaxSkillBytes)
            {
                return Result<ProjectSkillContent>.Failure(new DomainError("skills.too_large", "The requested project skill exceeds the configured size limit."));
            }

            return Result<ProjectSkillContent>.Success(new ProjectSkillContent(skillId, Utf8WithoutReplacement.GetString(bytes)));
        }
        catch (DecoderFallbackException)
        {
            return Result<ProjectSkillContent>.Failure(new DomainError("skills.invalid_encoding", "The requested project skill is not valid UTF-8 text."));
        }
        catch (IOException)
        {
            return Result<ProjectSkillContent>.Failure(new DomainError("skills.unavailable", "The requested project skill is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<ProjectSkillContent>.Failure(new DomainError("skills.unavailable", "The requested project skill is unavailable."));
        }
    }

    private static string GetSkillFilePath(string skillId) => $"{SkillDirectory}/{skillId}/{SkillFileName}";
}
