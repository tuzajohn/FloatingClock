using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;

namespace FloatingClock.UI.Core.Data;

public class Repository<T>(IDbContextFactory<FloatingClockDbContext> contextFactory) : IRepository<T> where T : class
{
    public async Task<IEnumerable<T>> GetAllAsync(Func<T, bool> predicate)
    {
        using var db = await contextFactory.CreateDbContextAsync();

        var results = db.Set<T>().Where(predicate).ToList();

        return await Task.FromResult(results);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        using var db = await contextFactory.CreateDbContextAsync();
        return await db.Set<T>().FindAsync(id);
    }

    public async Task AddAsync(T entity)
    {
        using var db = await contextFactory.CreateDbContextAsync();
        await db.Set<T>().AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        using var db = await contextFactory.CreateDbContextAsync();

        db.Set<T>().Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        using var db = await contextFactory.CreateDbContextAsync();
        var entity = await db.Set<T>().FindAsync(id);
        if (entity != null)
        {
            db.Set<T>().Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
