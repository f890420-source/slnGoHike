using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class SkillTag
{
    public long TagId { get; set; }

    public string Category { get; set; } = null!;

    public string TagName { get; set; } = null!;

    public long? ParentTagId { get; set; }

    public string UnlockCondition { get; set; } = null!;

    public virtual ICollection<SkillTag> InverseParentTag { get; set; } = new List<SkillTag>();

    public virtual SkillTag? ParentTag { get; set; }

    public virtual ICollection<UserSkillTag> UserSkillTags { get; set; } = new List<UserSkillTag>();
}
