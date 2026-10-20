using System;
using System.Collections.Generic;
using System.Text;

namespace FloatingClock.UI.Core.Domain;

public record ToDo(Guid Id, string Title, string Description, DateTime DueDate, bool IsComplte);
