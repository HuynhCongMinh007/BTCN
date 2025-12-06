using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Data;

public class AppPaintDbContextFactory : IDesignTimeDbContextFactory<AppPaintDbContext>
{
    public AppPaintDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppPaintDbContext>();

        optionsBuilder.UseSqlite("Data Source=data.db");

        return new AppPaintDbContext(optionsBuilder.Options);
    }
}
