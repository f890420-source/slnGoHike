using System;
using System.Collections.Generic;

namespace prjGoHike.Models;

public partial class EquipmentCategory
{
    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}
