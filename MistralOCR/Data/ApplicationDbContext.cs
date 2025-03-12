using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MistralOCR.Models;

namespace MistralOCR.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DocumentRecord> Documents { get; set; } = null!;
        public DbSet<DocumentQuestionLog> QuestionLogs { get; set; } = null!;
        public DbSet<DocumentOcrResult> OcrResults { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add indexes for Documents
            modelBuilder.Entity<DocumentRecord>()
                .HasIndex(d => d.Url);

            modelBuilder.Entity<DocumentRecord>()
                .HasIndex(d => d.CreatedAt);
                
            // Add indexes for QuestionLogs
            modelBuilder.Entity<DocumentQuestionLog>()
                .HasIndex(q => q.DocumentId);
                
            modelBuilder.Entity<DocumentQuestionLog>()
                .HasIndex(q => q.CreatedAt);
                
            // Add indexes for OcrResults
            modelBuilder.Entity<DocumentOcrResult>()
                .HasIndex(o => o.DocumentId);
                
            modelBuilder.Entity<DocumentOcrResult>()
                .HasIndex(o => o.CreatedAt);
        }
    }
} 