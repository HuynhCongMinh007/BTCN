using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using AppPaint.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppPaint.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
         using var scope = App.Services.CreateScope();
       var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
   var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();
       var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

            // Load basic stats
  var drawings = await templateService.GetAllTemplatesAsync();
      var allShapes = await shapeService.GetAllShapesAsync();
        var profiles = await profileService.GetAllProfilesAsync();

      // Update stat cards
 TotalDrawingsText.Text = drawings.Count.ToString();
     TotalShapesText.Text = allShapes.Count.ToString();
            TotalProfilesText.Text = profiles.Count.ToString();

        var activeProfile = profiles.FirstOrDefault(p => p.IsActive);
            ActiveProfileText.Text = activeProfile?.Name ?? "None";

 // Calculate shape types distribution
    var shapeTypeStats = CalculateShapeTypeStats(allShapes);
   if (shapeTypeStats.Any())
   {
           ShapeTypesChart.ItemsSource = shapeTypeStats;
                NoShapesMessage.Visibility = Visibility.Collapsed;
 }
     else
     {
        NoShapesMessage.Visibility = Visibility.Visible;
            }

  // Calculate top drawings by shape count
var topDrawings = CalculateTopDrawings(drawings);
          if (topDrawings.Any())
            {
   TopDrawingsChart.ItemsSource = topDrawings;
          NoDrawingsMessage.Visibility = Visibility.Collapsed;
            }
       else
            {
       NoDrawingsMessage.Visibility = Visibility.Visible;
        }

            System.Diagnostics.Debug.WriteLine($"📊 Dashboard loaded: {drawings.Count} drawings, {allShapes.Count} shapes");
        }
        catch (Exception ex)
        {
    System.Diagnostics.Debug.WriteLine($"❌ Error loading dashboard: {ex.Message}");
        }
    }

    private List<ShapeTypeStat> CalculateShapeTypeStats(List<Data.Models.Shape> shapes)
    {
        if (!shapes.Any()) return new List<ShapeTypeStat>();

     var totalCount = shapes.Count;
 
        var stats = shapes
        .GroupBy(s => s.ShapeType)
      .Select(g => new ShapeTypeStat
            {
            ShapeType = g.Key.ToString(),
  Count = g.Count(),
    Percentage = Math.Round((double)g.Count() / totalCount * 100, 1)
      })
            .OrderByDescending(s => s.Count)
            .ToList();

        return stats;
    }

    private List<TopDrawingStat> CalculateTopDrawings(List<Data.Models.DrawingTemplate> drawings)
    {
    if (!drawings.Any()) return new List<TopDrawingStat>();

  var topDrawings = drawings
            .OrderByDescending(d => d.Shapes.Count)
        .Take(5)
            .Select((d, index) => new TopDrawingStat
          {
  Rank = index + 1,
                Name = d.Name,
    ShapeCount = d.Shapes.Count
            })
            .ToList();

        return topDrawings;
    }
}

// Data models for charts
public class ShapeTypeStat
{
    public string ShapeType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class TopDrawingStat
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ShapeCount { get; set; }
}
