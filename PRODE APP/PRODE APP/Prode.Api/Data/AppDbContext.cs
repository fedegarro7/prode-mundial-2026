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

    public DbSet<GoldenGoalPick> GoldenGoalPicks => Set<GoldenGoalPick>();

    public DbSet<BombMatch> BombMatches => Set<BombMatch>();

    public DbSet<CaptainPick> CaptainPicks => Set<CaptainPick>();

    public DbSet<SharpShooterPrediction> SharpShooterPredictions => Set<SharpShooterPrediction>();

    public DbSet<OraclePrediction> OraclePredictions => Set<OraclePrediction>();

    public DbSet<RoundAward> RoundAwards => Set<RoundAward>();

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

        modelBuilder.Entity<GoldenGoalPick>()
            .HasOne(p => p.User)
            .WithMany(u => u.GoldenGoalPicks)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoldenGoalPick>()
            .HasOne(p => p.Match)
            .WithMany(m => m.GoldenGoalPicks)
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoldenGoalPick>()
            .HasIndex(p => new { p.UserId, p.RoundKey })
            .IsUnique();

        modelBuilder.Entity<BombMatch>()
            .HasOne(b => b.Match)
            .WithOne(m => m.BombMatch)
            .HasForeignKey<BombMatch>(b => b.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BombMatch>()
            .HasIndex(b => b.RoundKey)
            .IsUnique();

        modelBuilder.Entity<CaptainPick>()
            .HasOne(p => p.User)
            .WithOne(u => u.CaptainPick)
            .HasForeignKey<CaptainPick>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CaptainPick>()
            .HasOne(p => p.Team)
            .WithMany()
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SharpShooterPrediction>()
            .HasOne(p => p.User)
            .WithMany(u => u.SharpShooterPredictions)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SharpShooterPrediction>()
            .HasOne(p => p.Match)
            .WithMany(m => m.SharpShooterPredictions)
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SharpShooterPrediction>()
            .HasIndex(p => new { p.UserId, p.RoundKey })
            .IsUnique();

        modelBuilder.Entity<OraclePrediction>()
            .HasOne(p => p.User)
            .WithMany(u => u.OraclePredictions)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OraclePrediction>()
            .HasIndex(p => new { p.UserId, p.RoundKey })
            .IsUnique();

        modelBuilder.Entity<RoundAward>()
            .HasOne(a => a.User)
            .WithMany(u => u.RoundAwards)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
