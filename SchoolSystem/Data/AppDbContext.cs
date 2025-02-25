using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Models;

namespace SchoolSystem.Data
{
	public class AppDbContext : IdentityDbContext<AppUser>
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Group>()
				.HasIndex(e => new {e.StudentId, e.TutorId})
				.IsUnique(true);

			modelBuilder.Entity<Message>()
				.Property(e => e.FileCount)
				.HasDefaultValue(0);
		}

		public DbSet<Group> Groups { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<AttachFiles> AttachFiles { get; set; }
	}
}
