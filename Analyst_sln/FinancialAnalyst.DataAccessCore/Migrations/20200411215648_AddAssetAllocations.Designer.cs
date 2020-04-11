﻿// <auto-generated />
using System;
using FinancialAnalyst.DataAccess.Portfolios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinancialAnalyst.DataAccess.Migrations
{
    [DbContext(typeof(DummyPortfoliosContext))]
    [Migration("20200411215648_AddAssetAllocations")]
    partial class AddAssetAllocations
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Portfolios.AssetAllocation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("Exchange")
                        .HasColumnType("int");

                    b.Property<decimal?>("Percentage")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("PortfolioId")
                        .HasColumnType("int");

                    b.Property<string>("Ticker")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("PortfolioId");

                    b.ToTable("AssetAllocations");
                });

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Portfolios.Portfolio", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("InitialBalance")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("TotalCash")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Portfolios");
                });

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Portfolios.PortfolioBalance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("NetCashBalance")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("PortfolioId")
                        .HasColumnType("int");

                    b.Property<string>("TransactionCode")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("PortfolioId");

                    b.ToTable("PortfolioBalances");
                });

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Portfolios.Transaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Commission")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("NetCashBalance")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int?>("PortfolioId")
                        .HasColumnType("int");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<decimal>("RegFee")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Symbol")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TransactionCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("PortfolioId");

                    b.HasIndex("UserId");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Users.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Portfolios.AssetAllocation", b =>
                {
                    b.HasOne("FinancialAnalyst.Common.Entities.Portfolios.Portfolio", null)
                        .WithMany("AssetAllocations")
                        .HasForeignKey("PortfolioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Portfolios.Portfolio", b =>
                {
                    b.HasOne("FinancialAnalyst.Common.Entities.Users.User", null)
                        .WithMany("Portfolios")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Portfolios.PortfolioBalance", b =>
                {
                    b.HasOne("FinancialAnalyst.Common.Entities.Portfolios.Portfolio", null)
                        .WithMany("Balances")
                        .HasForeignKey("PortfolioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FinancialAnalyst.Common.Entities.Portfolios.Transaction", b =>
                {
                    b.HasOne("FinancialAnalyst.Common.Entities.Portfolios.Portfolio", null)
                        .WithMany("Transactions")
                        .HasForeignKey("PortfolioId");

                    b.HasOne("FinancialAnalyst.Common.Entities.Users.User", null)
                        .WithMany("Transactions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
