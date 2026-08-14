using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class HikeRecord
{
    public long RecordId { get; set; }

    public long UserId { get; set; }

    public long MountainId { get; set; }

    public DateOnly HikeDate { get; set; }

    public int CompanionCount { get; set; }

    public string Note { get; set; } = null!;

    public bool Verified { get; set; }

    public virtual ICollection<HikeRecordDetail> HikeRecordDetails { get; set; } = new List<HikeRecordDetail>();

    public virtual Mountain Mountain { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
