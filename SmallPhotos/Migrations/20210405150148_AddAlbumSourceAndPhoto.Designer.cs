﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmallPhotos.Data;

namespace SmallPhotos.Migrations
{
    [DbContext(typeof(SqliteDataContext))]
    [Migration("20210405150148_AddAlbumSourceAndPhoto")]
    partial class AddAlbumSourceAndPhoto
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.4");

            modelBuilder.Entity("SmallPhotos.Model.AlbumSource", b =>
                {
                    b.Property<int>("AlbumSourceId")
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

                    b.Property<string>("UserAccount")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UserAccountId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AlbumSourceId");

                    b.ToTable("AlbumSources");
                });

            modelBuilder.Entity("SmallPhotos.Model.Photo", b =>
                {
                    b.Property<int>("PhotoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AlbumSource")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("AlbumSourceId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DeletedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("FileCreationDateTime")
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

                    b.ToTable("Photos");
                });

            modelBuilder.Entity("SmallPhotos.Model.UserAccount", b =>
                {
                    b.Property<int>("UserAccountId")
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
#pragma warning restore 612, 618
        }
    }
}
