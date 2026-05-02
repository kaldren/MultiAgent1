using Microsoft.EntityFrameworkCore;

namespace MultiAgent1.VisionAgent.Data;

public class VisionAgentDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentOutput>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.OwnsOne(x => x.Result, result =>
            {
                result.ToJson();

                result.OwnsMany(x => x.Defects);
            });
        });
    }

    public DbSet<AgentOutput> AgentOutput => Set<AgentOutput>();
}