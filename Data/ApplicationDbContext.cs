using Microsoft.EntityFrameworkCore;
using Tender_Tool_Logs_Lambda.Models.User;

namespace Tender_Tool_Logs_Lambda.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<TenderUser> Users { get; set; }

        //User Child Entities
        public DbSet<SuperUser> SuperUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //user specific
            modelBuilder.Entity<TenderUser>(entity => { entity.ToTable("TenderUser"); });
            modelBuilder.Entity<SuperUser>(entity => { entity.ToTable("SuperUser"); });
        }
    }
}
