using Microsoft.EntityFrameworkCore;
using Prode.Api.Entities;

namespace Prode.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Stadium> Stadiums => Set<Stadium>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<Prediction> Predictions => Set<Prediction>();

    public DbSet<PrivateGroup> PrivateGroups => Set<PrivateGroup>();

    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Team>()
            .HasIndex(t => t.Code)
            .IsUnique();

        modelBuilder.Entity<Team>()
            .HasIndex(t => t.FifaId)
            .IsUnique();

        modelBuilder.Entity<Stadium>()
            .HasIndex(s => s.FifaId)
            .IsUnique();

        modelBuilder.Entity<Match>()
            .HasIndex(m => m.FifaId)
            .IsUnique();

        modelBuilder.Entity<Match>()
            .HasIndex(m => m.MatchNumber)
            .IsUnique();

        modelBuilder.Entity<Match>()
            .HasOne(m => m.HomeTeam)
            .WithMany(t => t.HomeMatches)
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.AwayTeam)
            .WithMany(t => t.AwayMatches)
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Stadium)
            .WithMany(s => s.Matches)
            .HasForeignKey(m => m.StadiumId);

        modelBuilder.Entity<Prediction>()
            .HasOne(p => p.User)
            .WithMany(u => u.Predictions)
            .HasForeignKey(p => p.UserId);

        modelBuilder.Entity<Prediction>()
            .HasOne(p => p.Match)
            .WithMany(m => m.Predictions)
            .HasForeignKey(p => p.MatchId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<PrivateGroup>()
            .HasIndex(g => g.InviteCode)
            .IsUnique();

        modelBuilder.Entity<PrivateGroup>()
            .HasOne(g => g.Owner)
            .WithMany(u => u.OwnedGroups)
            .HasForeignKey(g => g.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GroupMembership>()
            .HasOne(m => m.Group)
            .WithMany(g => g.Memberships)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupMembership>()
            .HasOne(m => m.User)
            .WithMany(u => u.GroupMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupMembership>()
            .HasIndex(m => new { m.GroupId, m.UserId })
            .IsUnique();
    }
}
