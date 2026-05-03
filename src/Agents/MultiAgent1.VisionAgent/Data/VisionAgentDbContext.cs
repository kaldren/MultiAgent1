using Microsoft.EntityFrameworkCore;
using MultiAgent1.VisionAgent.Models;

namespace MultiAgent1.VisionAgent.Data;

public class VisionAgentDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentOutput>(entity =>
        {
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.OwnsOne(x => x.Result, result =>
            {
                result.ToJson();

                result.OwnsMany(x => x.Defects);
            });
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
        });
    }

    public DbSet<AgentOutput> AgentOutput => Set<AgentOutput>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}