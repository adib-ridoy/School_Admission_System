using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public sealed class SchoolAdmissionDbContext(DbContextOptions<SchoolAdmissionDbContext> options)
    : DbContext(options)
{
    public DbSet<AdmissionClass> Classes => Set<AdmissionClass>();

    public DbSet<SeatHold> Holds => Set<SeatHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdmissionClass>(entity =>
        {
            entity.ToTable("classes", tableBuilder =>
                tableBuilder.HasCheckConstraint("ck_classes_total_seats_positive", "\"total_seats\" > 0"));

            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(32);
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.TotalSeats).HasColumnName("total_seats");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<SeatHold>(entity =>
        {
            entity.ToTable("holds");

            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(32);
            entity.Property(item => item.ClassId).HasColumnName("class_id").HasMaxLength(32);
            entity.Property(item => item.StudentId).HasColumnName("student_id").HasMaxLength(100).IsRequired();
            entity.Property(item => item.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.ExpiresAt).HasColumnName("expires_at");
            entity.Property(item => item.ConfirmedAt).HasColumnName("confirmed_at");
            entity.Property(item => item.CancelledAt).HasColumnName("cancelled_at");

            entity.HasIndex(item => new { item.ClassId, item.Status });
            entity.HasIndex(item => new { item.StudentId, item.ClassId })
                .HasFilter("\"status\" IN ('Held', 'Confirmed')")
                .IsUnique();

            entity.HasOne(item => item.Class)
                .WithMany(item => item.Holds)
                .HasForeignKey(item => item.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
