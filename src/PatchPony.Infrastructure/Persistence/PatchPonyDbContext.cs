using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Sessions;

namespace PatchPony.Infrastructure.Persistence;

public sealed class PatchPonyDbContext(DbContextOptions<PatchPonyDbContext> options) : DbContext(options)
{
    public DbSet<ProjectRow> Projects => Set<ProjectRow>();
    public DbSet<RepositoryRow> Repositories => Set<RepositoryRow>();
    public DbSet<KnowledgeSourceRow> KnowledgeSources => Set<KnowledgeSourceRow>();
    public DbSet<JobRow> Jobs => Set<JobRow>();
    public DbSet<JobStatusChangeRow> JobStatusChanges => Set<JobStatusChangeRow>();
    public DbSet<JobQueueItemRow> JobQueue => Set<JobQueueItemRow>();
    public DbSet<SessionRow> Sessions => Set<SessionRow>();
    public DbSet<ApprovalRow> Approvals => Set<ApprovalRow>();
    public DbSet<ArtifactReferenceRow> ArtifactReferences => Set<ArtifactReferenceRow>();
    public DbSet<ToolInvocationRow> ToolInvocations => Set<ToolInvocationRow>();
    public DbSet<IdempotencyRecordRow> IdempotencyRecords => Set<IdempotencyRecordRow>();
    public DbSet<AuditEventRow> AuditEvents => Set<AuditEventRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("patchpony");

        modelBuilder.Entity<ProjectRow>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.ManifestId).HasMaxLength(63).IsRequired();
            entity.Property(project => project.Name).HasMaxLength(120).IsRequired();
            entity.Property(project => project.CreatedAt).IsRequired();
            entity.HasIndex(project => project.ManifestId).IsUnique();
        });

        modelBuilder.Entity<RepositoryRow>(entity =>
        {
            entity.ToTable("repositories");
            entity.HasKey(repository => repository.ProjectId);
            entity.Property(repository => repository.RemoteUri).HasMaxLength(2_000).IsRequired();
            entity.Property(repository => repository.DefaultBranch).HasMaxLength(255).IsRequired();
            entity.HasOne<ProjectRow>().WithOne().HasForeignKey<RepositoryRow>(repository => repository.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSourceRow>(entity =>
        {
            entity.ToTable("knowledge_sources");
            entity.HasKey(source => source.Id);
            entity.Property(source => source.Name).HasMaxLength(63).IsRequired();
            entity.Property(source => source.RemoteUri).HasMaxLength(2_000).IsRequired();
            entity.Property(source => source.DefaultBranch).HasMaxLength(255).IsRequired();
            entity.Property(source => source.RegisteredAt).IsRequired();
            entity.HasIndex(source => new { source.ProjectId, source.Name }).IsUnique();
            entity.HasOne<ProjectRow>().WithMany().HasForeignKey(source => source.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<JobRow>(entity =>
        {
            entity.ToTable("jobs");
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Kind).HasMaxLength(100).IsRequired();
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(job => job.CreatedAt).IsRequired();
            entity.HasIndex(job => new { job.ProjectId, job.CreatedAt });
            entity.HasIndex(job => new { job.Status, job.CreatedAt });
            entity.HasOne<ProjectRow>().WithMany().HasForeignKey(job => job.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobStatusChangeRow>(entity =>
        {
            entity.ToTable("job_status_changes");
            entity.HasKey(change => change.Id);
            entity.Property(change => change.From).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(change => change.To).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(change => new { change.JobId, change.OccurredAt });
            entity.HasOne<JobRow>().WithMany().HasForeignKey(change => change.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionRow>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(session => session.Id);
            entity.HasIndex(session => new { session.JobId, session.ExpiresAt });
            entity.HasOne<ProjectRow>().WithMany().HasForeignKey(session => session.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JobRow>().WithMany().HasForeignKey(session => session.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalRow>(entity =>
        {
            entity.ToTable("approvals");
            entity.HasKey(approval => approval.Id);
            entity.Property(approval => approval.Decision).HasMaxLength(16).IsRequired();
            entity.Property(approval => approval.RequestedBy).HasMaxLength(255).IsRequired();
            entity.HasIndex(approval => new { approval.JobId, approval.RequestedAt });
            entity.HasOne<JobRow>().WithMany().HasForeignKey(approval => approval.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ArtifactReferenceRow>(entity =>
        {
            entity.ToTable("artifact_references");
            entity.HasKey(artifact => artifact.Id);
            entity.Property(artifact => artifact.Kind).HasMaxLength(100).IsRequired();
            entity.Property(artifact => artifact.Location).HasMaxLength(2_000).IsRequired();
            entity.Property(artifact => artifact.ContentHash).HasMaxLength(255).IsRequired();
            entity.HasIndex(artifact => new { artifact.JobId, artifact.CreatedAt });
            entity.HasOne<JobRow>().WithMany().HasForeignKey(artifact => artifact.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ToolInvocationRow>(entity =>
        {
            entity.ToTable("tool_invocations");
            entity.HasKey(invocation => invocation.Id);
            entity.Property(invocation => invocation.ToolName).HasMaxLength(255).IsRequired();
            entity.HasIndex(invocation => new { invocation.JobId, invocation.StartedAt });
            entity.HasOne<JobRow>().WithMany().HasForeignKey(invocation => invocation.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdempotencyRecordRow>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(record => record.Key);
            entity.Property(record => record.Key).HasMaxLength(255);
            entity.HasIndex(record => record.ExpiresAt);
            entity.HasOne<ProjectRow>().WithMany().HasForeignKey(record => record.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JobRow>().WithMany().HasForeignKey(record => record.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobQueueItemRow>(entity =>
        {
            entity.ToTable("job_queue");
            entity.HasKey(item => item.JobId);
            entity.HasIndex(item => new { item.AvailableAt, item.EnqueuedAt });
            entity.Property(item => item.ClaimedBy).HasMaxLength(255);
            entity.HasIndex(item => new { item.AvailableAt, item.ClaimExpiresAt, item.EnqueuedAt });
            entity.HasOne<JobRow>().WithOne().HasForeignKey<JobQueueItemRow>(item => item.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditEventRow>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(auditEvent => auditEvent.Id);
            entity.Property(auditEvent => auditEvent.EventType).HasMaxLength(255).IsRequired();
            entity.Property(auditEvent => auditEvent.CorrelationId).HasMaxLength(255).IsRequired();
            entity.HasIndex(auditEvent => new { auditEvent.ProjectId, auditEvent.OccurredAt });
            entity.HasIndex(auditEvent => new { auditEvent.JobId, auditEvent.OccurredAt });
            entity.HasOne<ProjectRow>().WithMany().HasForeignKey(auditEvent => auditEvent.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JobRow>().WithMany().HasForeignKey(auditEvent => auditEvent.JobId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class ProjectRow { public Guid Id { get; init; } public string ManifestId { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public DateTimeOffset CreatedAt { get; init; } }
public sealed class RepositoryRow { public Guid ProjectId { get; init; } public string RemoteUri { get; init; } = string.Empty; public string DefaultBranch { get; init; } = string.Empty; }
public sealed class KnowledgeSourceRow { public Guid Id { get; init; } public Guid ProjectId { get; init; } public string Name { get; init; } = string.Empty; public string RemoteUri { get; init; } = string.Empty; public string DefaultBranch { get; init; } = string.Empty; public DateTimeOffset RegisteredAt { get; init; } }
public sealed class JobRow { public Guid Id { get; init; } public Guid ProjectId { get; init; } public string Kind { get; init; } = string.Empty; public JobStatus Status { get; init; } public DateTimeOffset CreatedAt { get; init; } }
public sealed class JobStatusChangeRow { public Guid Id { get; init; } public Guid JobId { get; init; } public JobStatus From { get; init; } public JobStatus To { get; init; } public DateTimeOffset OccurredAt { get; init; } }
public sealed class JobQueueItemRow { public Guid JobId { get; init; } public DateTimeOffset EnqueuedAt { get; init; } public DateTimeOffset AvailableAt { get; init; } public Guid? ClaimId { get; set; } public string? ClaimedBy { get; set; } public DateTimeOffset? ClaimExpiresAt { get; set; } }
public sealed class SessionRow { public Guid Id { get; init; } public Guid ProjectId { get; init; } public Guid JobId { get; init; } public DateTimeOffset CreatedAt { get; init; } public DateTimeOffset ExpiresAt { get; init; } public SessionStatus Status { get; init; } public DateTimeOffset StatusChangedAt { get; init; } public string? FailureCode { get; init; } }
public sealed class ApprovalRow { public Guid Id { get; init; } public Guid JobId { get; init; } public string Decision { get; init; } = string.Empty; public string RequestedBy { get; init; } = string.Empty; public DateTimeOffset RequestedAt { get; init; } }
public sealed class ArtifactReferenceRow { public Guid Id { get; init; } public Guid JobId { get; init; } public string Kind { get; init; } = string.Empty; public string Location { get; init; } = string.Empty; public string ContentHash { get; init; } = string.Empty; public DateTimeOffset CreatedAt { get; init; } }
public sealed class ToolInvocationRow { public Guid Id { get; init; } public Guid JobId { get; init; } public string ToolName { get; init; } = string.Empty; public string RedactedParametersJson { get; init; } = "{}"; public DateTimeOffset StartedAt { get; init; } public DateTimeOffset? CompletedAt { get; init; } }
public sealed class IdempotencyRecordRow { public string Key { get; init; } = string.Empty; public Guid ProjectId { get; init; } public Guid JobId { get; init; } public DateTimeOffset CreatedAt { get; init; } public DateTimeOffset ExpiresAt { get; init; } }
public sealed class AuditEventRow { public Guid Id { get; init; } public Guid ProjectId { get; init; } public Guid? JobId { get; init; } public string EventType { get; init; } = string.Empty; public string CorrelationId { get; init; } = string.Empty; public DateTimeOffset OccurredAt { get; init; } public string MetadataJson { get; init; } = "{}"; }
