﻿// <auto-generated />
using System;
using Anidow.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Anidow.Migrations
{
    [DbContext(typeof(TrackContext))]
    [Migration("20200823101941_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7");

            modelBuilder.Entity("Anidow.Database.Models.Anime", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Cover")
                        .HasColumnType("TEXT");

                    b.Property<string>("Folder")
                        .HasColumnType("TEXT");

                    b.Property<string>("AnimeBytesScrapeAnime")
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

                    b.ToTable("Anime");
                });

            modelBuilder.Entity("Anidow.Database.Models.Episode", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AnimeId")
                        .HasColumnType("TEXT");

                    b.Property<string>("DownloadLink")
                        .HasColumnType("TEXT");

                    b.Property<string>("File")
                        .HasColumnType("TEXT");

                    b.Property<string>("Folder")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Hide")
                        .HasColumnType("INTEGER");

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

                    b.ToTable("Episodes");
                });
#pragma warning restore 612, 618
        }
    }
}
