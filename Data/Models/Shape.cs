using System.ComponentModel.DataAnnotations;

namespace Data.Models;

public class Shape
{
    [Key]
    public int Id { get; set; }

    public ShapeType ShapeType { get; set; }
    
    // Serialized points data (JSON format)
    public string PointsData { get; set; } = string.Empty;
    
    // Color in hex format (e.g., "#FF0000")
    public string Color { get; set; } = "#000000";
    
    // Stroke thickness
    public double StrokeThickness { get; set; } = 2.0;
    
    // For filled shapes
    public bool IsFilled { get; set; } = false;
    
    public string? FillColor { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Foreign key to template (nullable - shapes can exist without template)
    public int? TemplateId { get; set; }
    
    public virtual DrawingTemplate? Template { get; set; }
}
