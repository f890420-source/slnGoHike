using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class HikeRecords
{
    public long RecordId { get; set; }

    public long UserId { get; set; }

    public long MountainId { get; set; }

    public DateOnly HikeDate { get; set; }

    public int CompanionCount { get; set; }

    public string Note { get; set; } = null!;

    public bool Verified { get; set; }

    public virtual ICollection<HikeRecordDetails> HikeRecordDetails { get; set; } = new List<HikeRecordDetails>();

    public virtual Mountains Mountain { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
