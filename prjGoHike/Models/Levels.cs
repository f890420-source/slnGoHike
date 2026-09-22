using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class Levels
{
    public long LevelId { get; set; }

    public string LevelName { get; set; } = null!;

    public int MinXp { get; set; }

    public int MaxXp { get; set; }

    public virtual ICollection<Users> Users { get; set; } = new List<Users>();
}
