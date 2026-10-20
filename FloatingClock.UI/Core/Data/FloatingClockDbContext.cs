using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using FloatingClock.UI.Core.Domain;

using Microsoft.EntityFrameworkCore;

namespace FloatingClock.UI.Core.Data;

public class FloatingClockDbContext : DbContext
{
    public FloatingClockDbContext(DbContextOptions<FloatingClockDbContext> options)
        : base(options)
    {
    }

    public DbSet<ToDo> ToDos { get; set; }

}
