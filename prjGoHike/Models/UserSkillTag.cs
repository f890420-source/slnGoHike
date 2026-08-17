using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class UserSkillTag
{
    public long UserId { get; set; }

    public long TagId { get; set; }

    public string Source { get; set; } = null!;

    public virtual SkillTag SkillTag { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
