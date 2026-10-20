using System;
using System.Collections.Generic;
using System.Text;

namespace FloatingClock.UI.Core.Data;

public interface IRepository<T>
{
    Task<IEnumerable<T>> GetAllAsync(Func<T, bool> predicate);
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}
