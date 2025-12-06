namespace AppPaint.Models;

/// <summary>
/// Parameters for navigating to DrawingCanvasPage
/// </summary>
public class DrawingNavigationParameters
{
    /// <summary>
    /// Profile ID to apply settings from
    /// </summary>
    public int ProfileId { get; set; }
    
    /// <summary>
    /// Drawing/Template ID to load (null = new blank drawing)
    /// </summary>
    public int? DrawingId { get; set; }
    
    public DrawingNavigationParameters(int profileId, int? drawingId = null)
    {
        ProfileId = profileId;
  DrawingId = drawingId;
    }
}
