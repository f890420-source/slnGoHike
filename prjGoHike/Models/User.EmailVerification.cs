using Microsoft.EntityFrameworkCore;

namespace prjGoHike.Models;

public partial class User
{
    public DateTime? EmailVerifiedAt { get; set; }
}

public partial class GoHikeDataContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(user => user.EmailVerifiedAt)
            .HasColumnName("email_verified_at").HasColumnType("datetime2");
    }
}
