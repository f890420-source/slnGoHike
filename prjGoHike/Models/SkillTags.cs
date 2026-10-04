using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class SkillTags
{
    public long TagId { get; set; }

    public string Category { get; set; } = null!;

    public string TagName { get; set; } = null!;

    public long? ParentTagId { get; set; }

    public string UnlockCondition { get; set; } = null!;

    public virtual ICollection<SkillTags> InverseParentTag { get; set; } = new List<SkillTags>();

    public virtual SkillTags? ParentTag { get; set; }

    public virtual ICollection<UserSkillTags> UserSkillTags { get; set; } = new List<UserSkillTags>();
}
