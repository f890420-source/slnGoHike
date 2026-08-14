using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace prjGoHike.Models;

public partial class GoHikeData40Context : DbContext
{
    public GoHikeData40Context()
    {
    }

    public GoHikeData40Context(DbContextOptions<GoHikeData40Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Announcement> Announcements { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=GoHikeData40;Integrated Security=True;Encrypt=False", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PK__Announce__853AB7CFB653CA76");

            entity.ToTable("Announcement");

            entity.Property(e => e.AnnouncementId).HasColumnName("Announcement_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
