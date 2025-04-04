using Microsoft.EntityFrameworkCore;
using Sotoped.Models;

namespace Core.Data
{
    public class SotopedContext : DbContext
    {
        public SotopedContext(DbContextOptions<SotopedContext> options) : base(options)
        {

        }
        public DbSet<Spectator> Spectators { get; set; }
    }

}