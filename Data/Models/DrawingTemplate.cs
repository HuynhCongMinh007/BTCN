using System.ComponentModel.DataAnnotations;

namespace Data.Models;

public class DrawingTemplate
{
    [Key]
    public int Id { get; set; }
 
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    // Canvas dimensions
    public double Width { get; set; } = 800;
 
    public double Height { get; set; } = 600;
    
 // Background color in hex format
    public string BackgroundColor { get; set; } = "#FFFFFF";
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? ModifiedAt { get; set; }
    
    // Navigation property
    public virtual ICollection<Shape> Shapes { get; set; } = new List<Shape>();
}
