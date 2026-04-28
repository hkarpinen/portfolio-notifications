using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class NotificationsDbContext : DbContext
{
    public DbSet<PersistedNotification> Notifications => Set<PersistedNotification>();
    public DbSet<ThreadAuthorProjection> ThreadAuthors => Set<ThreadAuthorProjection>();
    public DbSet<CommentAuthorProjection> CommentAuthors => Set<CommentAuthorProjection>();
    public DbSet<HouseholdMemberProjection> HouseholdMembers => Set<HouseholdMemberProjection>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
