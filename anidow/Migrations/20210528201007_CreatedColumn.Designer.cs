// <auto-generated />
using System;
using Anidow.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Anidow.Migrations
{
    [DbContext(typeof(TrackContext))]
    [Migration("20210528201007_CreatedColumn")]
    partial class CreatedColumn
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.6");

            modelBuilder.Entity("Anidow.Database.Models.Anime", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Cover")
                        .HasColumnType("TEXT");

                    b.Property<int?>("CoverDataId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<string>("Folder")
                        .HasColumnType("TEXT");

                    b.Property<string>("Group")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("GroupId")
                        .HasColumnType("TEXT");

                    b.Property<string>("GroupUrl")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Released")
                        .HasColumnType("TEXT");

                    b.Property<string>("Resolution")
                        .HasColumnType("TEXT");

                    b.Property<int>("Site")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("CoverDataId");

                    b.ToTable("Anime");
                });

            modelBuilder.Entity("Anidow.Database.Models.Cover", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<string>("File")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Covers");
                });

            modelBuilder.Entity("Anidow.Database.Models.Episode", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AnimeId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Cover")
                        .HasColumnType("TEXT");

                    b.Property<int?>("CoverDataId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<string>("DownloadLink")
                        .HasColumnType("TEXT");

                    b.Property<string>("File")
                        .HasColumnType("TEXT");

                    b.Property<string>("Folder")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Hide")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("HideDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Link")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Released")
                        .HasColumnType("TEXT");

                    b.Property<int>("Site")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TorrentId")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Watched")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("WatchedDate")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CoverDataId");

                    b.ToTable("Episodes");
                });

            modelBuilder.Entity("Anidow.Database.Models.Anime", b =>
                {
                    b.HasOne("Anidow.Database.Models.Cover", "CoverData")
                        .WithMany()
                        .HasForeignKey("CoverDataId");

                    b.Navigation("CoverData");
                });

            modelBuilder.Entity("Anidow.Database.Models.Episode", b =>
                {
                    b.HasOne("Anidow.Database.Models.Cover", "CoverData")
                        .WithMany()
                        .HasForeignKey("CoverDataId");

                    b.Navigation("CoverData");
                });
#pragma warning restore 612, 618
        }
    }
}
