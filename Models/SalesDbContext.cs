using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SalesAPI.Models;

public partial class SalesDbContext : DbContext
{
    public SalesDbContext()
    {
    }

    public SalesDbContext(DbContextOptions<SalesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<SalesOrder> SalesOrders { get; set; }

    public virtual DbSet<SalesRep> SalesReps { get; set; }

    public virtual DbSet<VwSalesFact> VwSalesFacts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B87FC35748");

            entity.HasIndex(e => e.Country, "IX_Customers_Country");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.AnnualRevenueUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("AnnualRevenueUSD");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerType).HasMaxLength(50);
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED06A1259B166D");

            entity.HasIndex(e => e.ProductId, "IX_OrderItems_ProductID");

            entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");
            entity.Property(e => e.LineTotalUsd)
                .HasComputedColumnSql("([Quantity]*[UnitPriceUSD])", true)
                .HasColumnType("decimal(23, 2)")
                .HasColumnName("LineTotalUSD");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UnitPriceUsd)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("UnitPriceUSD");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED33F0D22A");

            entity.HasIndex(e => e.ProductCategory, "IX_Products_Category");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.LifecycleStatus).HasMaxLength(50);
            entity.Property(e => e.PackageType).HasMaxLength(50);
            entity.Property(e => e.ProcessNodeNm).HasColumnName("ProcessNodeNM");
            entity.Property(e => e.ProductCategory).HasMaxLength(100);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.UnitPriceUsd)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("UnitPriceUSD");
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__SalesOrd__C3905BAF9EAE831F");

            entity.HasIndex(e => e.OrderDate, "IX_SalesOrders_OrderDate");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("USD")
                .IsFixedLength();
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderStatus).HasMaxLength(50);
            entity.Property(e => e.SalesRepId).HasColumnName("SalesRepID");
            entity.Property(e => e.TotalAmountUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TotalAmountUSD");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrders_Customers");

            entity.HasOne(d => d.SalesRep).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.SalesRepId)
                .HasConstraintName("FK_SalesOrders_SalesReps");
        });

        modelBuilder.Entity<SalesRep>(entity =>
        {
            entity.HasKey(e => e.SalesRepId).HasName("PK__SalesRep__736D06D1F4DE1728");

            entity.Property(e => e.SalesRepId).HasColumnName("SalesRepID");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.RepName).HasMaxLength(150);
        });

        modelBuilder.Entity<VwSalesFact>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SalesFact");

            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerType).HasMaxLength(50);
            entity.Property(e => e.LineTotalUsd)
                .HasColumnType("decimal(23, 2)")
                .HasColumnName("LineTotalUSD");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProcessNodeNm).HasColumnName("ProcessNodeNM");
            entity.Property(e => e.ProductCategory).HasMaxLength(100);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.RepName).HasMaxLength(150);
            entity.Property(e => e.UnitPriceUsd)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("UnitPriceUSD");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
