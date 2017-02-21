using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LelCommon
{
    public class LelContext : DbContext
    {
        private readonly string _connectionString;

        public LelContext(DbContextOptions options, string connectionString)
            : base(options)
        {
            _connectionString = connectionString;
        }

        public DbSet<Aggregation> Aggregations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
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