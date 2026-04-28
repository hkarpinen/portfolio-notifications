using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class PersistedNotificationConfiguration : IEntityTypeConfiguration<PersistedNotification>
{
    public void Configure(EntityTypeBuilder<PersistedNotification> builder)
    {
        builder.ToTable("notifications", "notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Read).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.UserId, x.Read });
    }
}

internal sealed class ThreadAuthorProjectionConfiguration : IEntityTypeConfiguration<ThreadAuthorProjection>
{
    public void Configure(EntityTypeBuilder<ThreadAuthorProjection> builder)
    {
        builder.ToTable("thread_author_projections", "notifications");
        builder.HasKey(x => x.ThreadId);
        builder.Property(x => x.ThreadId).ValueGeneratedNever();
        builder.Property(x => x.AuthorId).IsRequired();
        builder.Property(x => x.CommunitySlug).IsRequired().HasMaxLength(200);
    }
}

internal sealed class CommentAuthorProjectionConfiguration : IEntityTypeConfiguration<CommentAuthorProjection>
{
    public void Configure(EntityTypeBuilder<CommentAuthorProjection> builder)
    {
        builder.ToTable("comment_author_projections", "notifications");
        builder.HasKey(x => x.CommentId);
        builder.Property(x => x.CommentId).ValueGeneratedNever();
        builder.Property(x => x.AuthorId).IsRequired();
    }
}

internal sealed class HouseholdMemberProjectionConfiguration : IEntityTypeConfiguration<HouseholdMemberProjection>
{
    public void Configure(EntityTypeBuilder<HouseholdMemberProjection> builder)
    {
        builder.ToTable("household_member_projections", "notifications");
        builder.HasKey(x => new { x.HouseholdId, x.UserId });
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
    }
}

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events", "notifications");
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.EventId).ValueGeneratedNever();
        builder.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProcessedAt).HasColumnType("timestamptz").IsRequired();
        builder.HasIndex(x => x.ProcessedAt);
    }
}
