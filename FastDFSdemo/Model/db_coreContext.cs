using System;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FastDFSdemo.Model
{
    public partial class db_coreContext : DbContext
    {
        public db_coreContext()
        {
        }

        public db_coreContext(DbContextOptions<db_coreContext> options)
            : base(options)
        {
        }

        public virtual DbSet<TCommonFile> TCommonFile { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                String mysql_server = ConfigurationManager.AppSettings["mysql_server"];
                String mysql_database = ConfigurationManager.AppSettings["mysql_database"];
                String mysql_user = ConfigurationManager.AppSettings["mysql_user"];
                String mysql_pwd = ConfigurationManager.AppSettings["mysql_pwd"];
                string connectionString = "server="+mysql_server+";"+ "database="+mysql_database+";"+ "user="+mysql_user + ";" + "password=" + mysql_pwd;
                Console.WriteLine(connectionString);
                DbContextOptionsBuilder dbContextOptionsBuilder = optionsBuilder.UseMySql(connectionString);

            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TCommonFile>(entity =>
            {
                entity.ToTable("t_common_file");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasColumnName("file_name")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.FilePath)
                    .IsRequired()
                    .HasColumnName("file_path")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.FileSize)
                    .HasColumnName("file_size")
                    .HasColumnType("double(16,4)");

                entity.Property(e => e.FileType)
                    .IsRequired()
                    .HasColumnName("file_type")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.Ip)
                    .IsRequired()
                    .HasColumnName("ip")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.IsDelete).HasColumnName("is_delete");

                entity.Property(e => e.UploadTime)
                    .HasColumnName("upload_time")
                    .HasColumnType("datetime");

                entity.Property(e => e.Uploader)
                    .IsRequired()
                    .HasColumnName("uploader")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
