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

            // Group & message
            modelBuilder.Entity<AppUser>()
                .HasMany(e => e.Group)
                .WithMany(e => e.User)
                .UsingEntity<GroupUsers>(
                    l => l.HasOne<Group>(gu => gu.Group).WithMany(g => g.GroupUsers).HasForeignKey(e => e.GroupId),
                    r => r.HasOne<AppUser>(gu => gu.User).WithMany(au => au.GroupUsers).HasForeignKey(e => e.UserId)
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

            // Blog relationships
            modelBuilder.Entity<Blog>()
                .HasMany(b => b.Comments)
                .WithOne(c => c.Blog)
                .HasForeignKey(c => c.BlogId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Blog>()
                .HasMany(b => b.Ratings)
                .WithOne(r => r.Blog)
                .HasForeignKey(r => r.BlogId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BlogComment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.NoAction);

            // Document relationships
            modelBuilder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User Profile relationships
            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Documents)
                .WithOne(d => d.User)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        // Group & message
        public DbSet<GroupUsers> GroupUsers { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<AttachFiles> AttachFiles { get; set; }

        // Blog related
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<BlogRating> BlogRatings { get; set; }
        public DbSet<BlogComment> BlogComments { get; set; }

        // Document management
        public DbSet<Document> Documents { get; set; }
    }
}
