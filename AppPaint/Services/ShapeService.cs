using Data;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppPaint.Services;

public interface IShapeService
{
Task<List<Shape>> GetAllShapesAsync();
    Task<Shape?> GetShapeByIdAsync(int id);
    Task<Shape> CreateShapeAsync(Shape shape);
 Task<Shape> UpdateShapeAsync(Shape shape);
    Task<bool> DeleteShapeAsync(int id);
    Task<List<Shape>> GetShapesByTemplateIdAsync(int? templateId);
}

public class ShapeService : IShapeService
{
    private readonly AppPaintDbContext _context;

    public ShapeService(AppPaintDbContext context)
    {
 _context = context;
    }

    public async Task<List<Shape>> GetAllShapesAsync()
    {
   return await _context.Shapes
  .Include(s => s.Template)
    .OrderByDescending(s => s.CreatedAt)
   .ToListAsync();
    }

    public async Task<Shape?> GetShapeByIdAsync(int id)
    {
        return await _context.Shapes
    .Include(s => s.Template)
    .FirstOrDefaultAsync(s => s.Id == id);
    }

 public async Task<Shape> CreateShapeAsync(Shape shape)
  {
        shape.CreatedAt = DateTime.Now;
  _context.Shapes.Add(shape);
    await _context.SaveChangesAsync();
        return shape;
    }

    public async Task<Shape> UpdateShapeAsync(Shape shape)
    {
  _context.Shapes.Update(shape);
   await _context.SaveChangesAsync();
   return shape;
    }

  public async Task<bool> DeleteShapeAsync(int id)
    {
   var shape = await _context.Shapes.FindAsync(id);
        if (shape == null) return false;

  _context.Shapes.Remove(shape);
    await _context.SaveChangesAsync();
        return true;
    }

 public async Task<List<Shape>> GetShapesByTemplateIdAsync(int? templateId)
  {
   return await _context.Shapes
  .Where(s => s.TemplateId == templateId)
   .OrderBy(s => s.CreatedAt)
   .ToListAsync();
    }
}
