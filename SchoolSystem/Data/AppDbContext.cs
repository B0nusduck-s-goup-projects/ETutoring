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

			modelBuilder.Entity<Student>()
				.HasOne(e => e.GroupStudent)
				.WithOne(e => e.Student)
				.HasForeignKey<Group>(e => e.StudentId)
				.OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tutor>()
                .HasMany(e => e.GroupTutor)
                .WithOne(e => e.Tutor)
                .HasForeignKey(e => e.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Group>()
				.Property(e => e.IsValid)
				.HasDefaultValue(true);
			modelBuilder.Entity<Group>()
				.HasIndex(e => new {e.StudentId, e.TutorId})
				.IsUnique(true);
			modelBuilder.Entity<Group>()
				.HasMany(e => e.Messages)
				.WithOne(e => e.Group)
				.HasForeignKey(e => e.GroupId)
				.IsRequired(true)
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

		public DbSet<Group> Groups { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<AttachFiles> AttachFiles { get; set; }
	}
}
