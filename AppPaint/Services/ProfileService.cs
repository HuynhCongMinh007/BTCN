using Data;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppPaint.Services;

public interface IProfileService
{
    Task<List<Profile>> GetAllProfilesAsync();
    Task<Profile?> GetProfileByIdAsync(int id);
    Task<Profile?> GetActiveProfileAsync();
    Task<Profile> CreateProfileAsync(Profile profile);
    Task<Profile> UpdateProfileAsync(Profile profile);
    Task<bool> DeleteProfileAsync(int id);
 Task<bool> SetActiveProfileAsync(int id);
}

public class ProfileService : IProfileService
{
    private readonly AppPaintDbContext _context;

    public ProfileService(AppPaintDbContext context)
    {
   _context = context;
    }

    public async Task<List<Profile>> GetAllProfilesAsync()
    {
 return await _context.Profiles
            .OrderByDescending(p => p.IsActive)
   .ThenByDescending(p => p.CreatedAt)
  .ToListAsync();
    }

    public async Task<Profile?> GetProfileByIdAsync(int id)
    {
    return await _context.Profiles.FindAsync(id);
    }

    public async Task<Profile?> GetActiveProfileAsync()
  {
        return await _context.Profiles
   .FirstOrDefaultAsync(p => p.IsActive);
    }

 public async Task<Profile> CreateProfileAsync(Profile profile)
    {
   profile.CreatedAt = DateTime.Now;
        _context.Profiles.Add(profile);
   await _context.SaveChangesAsync();
     return profile;
    }

    public async Task<Profile> UpdateProfileAsync(Profile profile)
    {
   profile.ModifiedAt = DateTime.Now;
  _context.Profiles.Update(profile);
   await _context.SaveChangesAsync();
   return profile;
    }

    public async Task<bool> DeleteProfileAsync(int id)
    {
        var profile = await _context.Profiles.FindAsync(id);
   if (profile == null) return false;

        // Don't allow deleting the active profile
  if (profile.IsActive)
  {
   return false;
        }

 _context.Profiles.Remove(profile);
        await _context.SaveChangesAsync();
   return true;
 }

    public async Task<bool> SetActiveProfileAsync(int id)
    {
  var profile = await _context.Profiles.FindAsync(id);
    if (profile == null) return false;

        // Deactivate all profiles
  var allProfiles = await _context.Profiles.ToListAsync();
   foreach (var p in allProfiles)
     {
      p.IsActive = false;
   }

        // Activate the selected profile
   profile.IsActive = true;
   await _context.SaveChangesAsync();
   return true;
 }
}
