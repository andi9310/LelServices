using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LelCommon
{
    public class LelContext : DbContext
    {
        public LelContext(DbContextOptions options)
            : base(options)
        { }

        public DbSet<Aggregation> Aggregations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=mssql;Database=lel_db;User Id=sa;Password=Elo_benc1!; ");
        }
    }

    public class Aggregation
    {
        [Key]
        public string Command { get; set; }
        public int Pass { get; set; }
        public int Fail { get; set; }
        public int Error { get; set; }

    }
}