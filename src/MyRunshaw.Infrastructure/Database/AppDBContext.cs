using Microsoft.EntityFrameworkCore;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<FriendRequest> FriendRequests { get; set; }
    public DbSet<BlockedUser> BlockedUsers { get; set; }
    public DbSet<Bus> Buses { get; set; }
    public DbSet<BusStop> BusStops { get; set; }
    public DbSet<BusSubscription> BusSubscriptions { get; set; }
    public DbSet<TimetableCache> Timetables { get; set; }
    public DbSet<InAppNotice> InAppNotices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One student can have many bus subscriptions, and one bus can have many students subscribed.
        modelBuilder.Entity<BusSubscription>()
            .HasKey(bs => new { bs.StudentId, bs.BusId });

        // One user can block many users, and one user can be blocked by many users.
        modelBuilder.Entity<BlockedUser>()
            .HasKey(bu => new { bu.BlockerId, bu.BlockedId });

        // If a user is deleted, their sent and received friend requests should also be deleted.
        modelBuilder.Entity<FriendRequest>()
            .HasOne(f => f.Sender)
            .WithMany(u => u.SentFriendRequests)
            .HasForeignKey(f => f.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FriendRequest>()
            .HasOne(f => f.Receiver)
            .WithMany(u => u.ReceivedFriendRequests)
            .HasForeignKey(f => f.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map the Enum to a string in the DB so it's readable (e.g., "Accepted" instead of 1)
        modelBuilder.Entity<FriendRequest>()
            .Property(f => f.Status)
            .HasConversion<string>();
    }
}