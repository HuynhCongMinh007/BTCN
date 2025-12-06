using Data;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppPaint.Services;

public interface ITemplateService
{
    Task<List<DrawingTemplate>> GetAllTemplatesAsync();
    Task<DrawingTemplate?> GetTemplateByIdAsync(int id);
    Task<DrawingTemplate> CreateTemplateAsync(DrawingTemplate template);
    Task<DrawingTemplate> UpdateTemplateAsync(DrawingTemplate template);
    Task<bool> DeleteTemplateAsync(int id);
    Task<List<Shape>> GetTemplateShapesAsync(int templateId);
}

public class TemplateService : ITemplateService
{
    private readonly AppPaintDbContext _context;

    public TemplateService(AppPaintDbContext context)
    {
        _context = context;
    }

    public async Task<List<DrawingTemplate>> GetAllTemplatesAsync()
    {
        return await _context.DrawingTemplates
     .Include(t => t.Shapes)
          .OrderByDescending(t => t.CreatedAt)
          .ToListAsync();
    }

    public async Task<DrawingTemplate?> GetTemplateByIdAsync(int id)
    {
        return await _context.DrawingTemplates
    .Include(t => t.Shapes)
   .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<DrawingTemplate> CreateTemplateAsync(DrawingTemplate template)
    {
        template.CreatedAt = DateTime.Now;
        _context.DrawingTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<DrawingTemplate> UpdateTemplateAsync(DrawingTemplate template)
    {
        template.ModifiedAt = DateTime.Now;
  _context.DrawingTemplates.Update(template);
    await _context.SaveChangesAsync();
   return template;
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        var template = await _context.DrawingTemplates.FindAsync(id);
   if (template == null) return false;

        _context.DrawingTemplates.Remove(template);
  await _context.SaveChangesAsync();
    return true;
    }

    public async Task<List<Shape>> GetTemplateShapesAsync(int templateId)
    {
     return await _context.Shapes
 .Where(s => s.TemplateId == templateId)
        .OrderBy(s => s.CreatedAt)
     .ToListAsync();
    }
}
