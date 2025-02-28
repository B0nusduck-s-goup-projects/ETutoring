using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Abstractions;
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

			modelBuilder.Entity<AppUser>()
				.HasMany(e => e.Group)
				.WithMany(e => e.User)
				.UsingEntity<GroupUsers>(
					l => l.HasOne<Group>().WithMany().HasForeignKey(e => e.GroupId),
					r => r.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId)
				);

            modelBuilder.Entity<Group>()
				.Property(e => e.IsValid)
				.HasDefaultValue(true);
			modelBuilder.Entity<Group>()
				.HasMany(e => e.Messages)
				.WithOne(e => e.Group)
				.HasForeignKey(e => e.GroupId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Message>()
				.Property(e => e.FileCount)
				.HasDefaultValue(0);
            modelBuilder.Entity<Message>()
                .HasMany(e => e.AttachFiles)
                .WithOne(e => e.Message)
                .HasForeignKey(e => e.MessageId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

        }

		public DbSet<GroupUsers> GroupUsers { get; set; }
		public DbSet<Group> Groups { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<AttachFiles> AttachFiles { get; set; }
	}
}
