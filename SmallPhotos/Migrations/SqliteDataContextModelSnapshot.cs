﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmallPhotos.Data;

#nullable disable

namespace SmallPhotos.Migrations
{
    [DbContext(typeof(SqliteDataContext))]
    partial class SqliteDataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.0");

            modelBuilder.Entity("SmallPhotos.Model.AlbumSource", b =>
                {
                    b.Property<long>("AlbumSourceId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DeletedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Folder")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastUpdateDateTime")
                        .HasColumnType("TEXT");

                    b.Property<long>("UserAccountId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AlbumSourceId");

                    b.HasIndex("UserAccountId");

                    b.ToTable("AlbumSources");
                });

            modelBuilder.Entity("SmallPhotos.Model.Photo", b =>
                {
                    b.Property<long>("PhotoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("AlbumSourceId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DeletedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("FileCreationDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("FileModificationDateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Filename")
                        .HasColumnType("TEXT");

                    b.Property<int>("Height")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LastUpdateDateTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("Width")
                        .HasColumnType("INTEGER");

                    b.HasKey("PhotoId");

                    b.HasIndex("AlbumSourceId");

                    b.ToTable("Photos");
                });

            modelBuilder.Entity("SmallPhotos.Model.Thumbnail", b =>
                {
                    b.Property<long>("ThumbnailId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastUpdateDateTime")
                        .HasColumnType("TEXT");

                    b.Property<long>("PhotoId")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("ThumbnailImage")
                        .HasColumnType("BLOB");

                    b.Property<int>("ThumbnailSize")
                        .HasColumnType("INTEGER");

                    b.HasKey("ThumbnailId");

                    b.HasIndex("PhotoId");

                    b.ToTable("Thumbnails");
                });

            modelBuilder.Entity("SmallPhotos.Model.UserAccount", b =>
                {
                    b.Property<long>("UserAccountId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AuthenticationUri")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DeletedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastUpdateDateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("UserAccountId");

                    b.ToTable("UserAccounts");
                });

            modelBuilder.Entity("SmallPhotos.Model.AlbumSource", b =>
                {
                    b.HasOne("SmallPhotos.Model.UserAccount", "UserAccount")
                        .WithMany()
                        .HasForeignKey("UserAccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserAccount");
                });

            modelBuilder.Entity("SmallPhotos.Model.Photo", b =>
                {
                    b.HasOne("SmallPhotos.Model.AlbumSource", "AlbumSource")
                        .WithMany()
                        .HasForeignKey("AlbumSourceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AlbumSource");
                });

            modelBuilder.Entity("SmallPhotos.Model.Thumbnail", b =>
                {
                    b.HasOne("SmallPhotos.Model.Photo", "Photo")
                        .WithMany()
                        .HasForeignKey("PhotoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Photo");
                });
#pragma warning restore 612, 618
        }
    }
}
