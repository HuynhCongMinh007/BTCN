using System.ComponentModel.DataAnnotations;

namespace Data.Models;

public class Profile
{
    [Key]
    public int Id { get; set; }
    
    [Required]
 [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    // Theme: Light, Dark, System
    public string Theme { get; set; } = "System";
    
    // Default canvas size
    public double DefaultCanvasWidth { get; set; } = 800;
  
 public double DefaultCanvasHeight { get; set; } = 600;
    
 // Default colors
    public string DefaultStrokeColor { get; set; } = "#000000";
    
    public string DefaultFillColor { get; set; } = "#FFFFFF";
 
    public string DefaultBackgroundColor { get; set; } = "#FFFFFF";
  
    // Default stroke thickness
    public double DefaultStrokeThickness { get; set; } = 2.0;
    
  // Structure preferences (JSON format for custom settings)
  public string? CustomSettings { get; set; }
    
 public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? ModifiedAt { get; set; }
    
    public bool IsActive { get; set; } = false;
    
    // Navigation property to drawings created with this profile
    public virtual ICollection<DrawingTemplate> DrawingTemplates { get; set; } = new List<DrawingTemplate>();
}
