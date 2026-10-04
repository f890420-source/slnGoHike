using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class UserSkillTags
{
    public long UserId { get; set; }

    public long TagId { get; set; }

    public string Source { get; set; } = null!;

    public bool IsDisplayed { get; set; }

    public virtual SkillTags Tag { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
