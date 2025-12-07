using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class AppPaintDbContext : DbContext
{
    public DbSet<Shape> Shapes { get; set; }
    public DbSet<DrawingTemplate> DrawingTemplates { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    public AppPaintDbContext(DbContextOptions<AppPaintDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=designTime.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Shape configuration
        modelBuilder.Entity<Shape>(entity =>
      {
          entity.HasKey(e => e.Id);
          entity.Property(e => e.ShapeType).IsRequired();
          entity.Property(e => e.PointsData).IsRequired();
          entity.Property(e => e.Color).IsRequired().HasMaxLength(20);
          entity.Property(e => e.StrokeThickness).IsRequired();

          // Relationship with Template
          entity.HasOne(e => e.Template)
      .WithMany(t => t.Shapes)
.HasForeignKey(e => e.TemplateId)
    .OnDelete(DeleteBehavior.Cascade);
      });

        // DrawingTemplate configuration
        modelBuilder.Entity<DrawingTemplate>(entity =>
      {
          entity.HasKey(e => e.Id);
          entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
          entity.Property(e => e.BackgroundColor).IsRequired().HasMaxLength(20);

          // Relationship with Profile
          entity.HasOne(e => e.Profile)
   .WithMany(p => p.DrawingTemplates)
    .HasForeignKey(e => e.ProfileId)
   .OnDelete(DeleteBehavior.SetNull); // When profile deleted, set ProfileId to null
        });

        // Profile configuration
        modelBuilder.Entity<Profile>(entity =>
     {
         entity.HasKey(e => e.Id);
         entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
         entity.Property(e => e.Theme).IsRequired().HasMaxLength(50);
     });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default profile
        modelBuilder.Entity<Profile>().HasData(
 new Profile
 {
     Id = 1,
     Name = "Default Profile",
     Theme = "System",
     DefaultCanvasWidth = 800,
     DefaultCanvasHeight = 600,
     DefaultStrokeColor = "#000000",
     DefaultFillColor = "#FFFFFF",
     DefaultBackgroundColor = "#FFFFFF",
     DefaultStrokeThickness = 2.0,
     IsActive = true,
     CreatedAt = DateTime.Now
 }
     );

        // Seed sample templates
        modelBuilder.Entity<DrawingTemplate>().HasData(
         new DrawingTemplate
         {
             Id = 1,
             Name = "Sample Template 1",
             Width = 800,
             Height = 600,
             BackgroundColor = "#F0F0F0",
             CreatedAt = DateTime.Now
         },
         new DrawingTemplate
         {
             Id = 2,
             Name = "Sample Template 2",
             Width = 1024,
             Height = 768,
             BackgroundColor = "#E8F4F8",
             CreatedAt = DateTime.Now
         }
        );

        // Seed sample shapes
        modelBuilder.Entity<Shape>().HasData(
            new Shape
            {
                Id = 1,
                ShapeType = ShapeType.Rectangle,
                PointsData = "[{\"X\":100,\"Y\":100},{\"X\":300,\"Y\":250}]",
                Color = "#FF0000",
                StrokeThickness = 3,
                IsFilled = true,
                FillColor = "#FFCCCC",
                TemplateId = 1,
                CreatedAt = DateTime.Now
            },
            new Shape
            {
                Id = 2,
                ShapeType = ShapeType.Circle,
                PointsData = "[{\"X\":500,\"Y\":300},{\"X\":600,\"Y\":300}]",
                Color = "#0000FF",
                StrokeThickness = 2,
                IsFilled = false,
                TemplateId = 1,
                CreatedAt = DateTime.Now
            },
            new Shape
            {
                Id = 3,
                ShapeType = ShapeType.Line,
                PointsData = "[{\"X\":50,\"Y\":50},{\"X\":400,\"Y\":400}]",
                Color = "#00FF00",
                StrokeThickness = 5,
                IsFilled = false,
                TemplateId = 2,
                CreatedAt = DateTime.Now
            }
        );
    }
}
