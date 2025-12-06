using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppPaint.Services;
using Data.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AppPaint.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    [ObservableProperty]
    private int _totalDrawings;

    [ObservableProperty]
    private int _totalShapes;

    [ObservableProperty]
    private int _totalProfiles;

    [ObservableProperty]
    private string _averageShapesPerDrawing = "0";

    [ObservableProperty]
    private ObservableCollection<ShapeTypeStat> _shapeTypeStats = new();

    [ObservableProperty]
    private ObservableCollection<DrawingTemplate> _recentDrawings = new();

    public DashboardViewModel()
    {
        Title = "Dashboard";
    }

    public override async void OnNavigatedTo(object? parameter = null)
    {
        base.OnNavigatedTo(parameter);
        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        try
        {
            IsBusy = true;

            using var scope = App.Services.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
            var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();
            var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

            // Load all data
            var templates = await templateService.GetAllTemplatesAsync();
            var allShapes = await shapeService.GetAllShapesAsync();
            var profiles = await profileService.GetAllProfilesAsync();

            // Calculate statistics
            TotalDrawings = templates.Count;
            TotalShapes = allShapes.Count;
            TotalProfiles = profiles.Count;

            // Average shapes per drawing
            if (TotalDrawings > 0)
            {
                double avg = (double)TotalShapes / TotalDrawings;
                AverageShapesPerDrawing = avg.ToString("F1");
            }
            else
            {
                AverageShapesPerDrawing = "0";
            }

            // Shape type distribution
            CalculateShapeTypeDistribution(allShapes);

            // Recent drawings (top 10)
            RecentDrawings.Clear();
            foreach (var template in templates.OrderByDescending(t => t.CreatedAt).Take(10))
            {
                RecentDrawings.Add(template);
            }

            System.Diagnostics.Debug.WriteLine($"✅ Dashboard loaded: {TotalDrawings} drawings, {TotalShapes} shapes, {TotalProfiles} profiles");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading dashboard: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CalculateShapeTypeDistribution(List<Shape> shapes)
    {
        ShapeTypeStats.Clear();

        if (shapes.Count == 0) return;

        // Group by shape type and count
        var typeGroups = shapes
            .GroupBy(s => s.ShapeType)
            .Select(g => new
            {
                ShapeType = g.Key,
                Count = g.Count(),
                Percentage = (double)g.Count() / shapes.Count * 100
            })
            .OrderByDescending(x => x.Count);

        foreach (var group in typeGroups)
        {
            ShapeTypeStats.Add(new ShapeTypeStat
            {
                ShapeTypeName = GetShapeTypeDisplayName(group.ShapeType),
                Count = group.Count,
                Percentage = (int)Math.Round(group.Percentage)
            });
        }
    }

    private string GetShapeTypeDisplayName(ShapeType shapeType)
    {
        return shapeType switch
        {
            ShapeType.Line => "📏 Line",
            ShapeType.Rectangle => "▭ Rectangle",
            ShapeType.Circle => "⭕ Circle",
            ShapeType.Oval => "⬭ Oval",
            ShapeType.Triangle => "△ Triangle",
            ShapeType.Polygon => "⬡ Polygon",
            _ => shapeType.ToString()
        };
    }
}

/// <summary>
/// Statistics for each shape type
/// </summary>
public class ShapeTypeStat
{
    public string ShapeTypeName { get; set; } = "";
    public int Count { get; set; }
    public int Percentage { get; set; }
}
