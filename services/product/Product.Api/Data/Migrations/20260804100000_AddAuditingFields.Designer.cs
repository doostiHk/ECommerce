using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Product.Api.Data;

#nullable disable

namespace Product.Api.Data.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20260804100000_AddAuditingFields")]
partial class AddAuditingFields
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.3")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("Product.Api.Domain.Entities.Product", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<DateTime>("CreatedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("CreatedByUserId")
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)");

                b.Property<string>("Description")
                    .HasMaxLength(2000)
                    .HasColumnType("character varying(2000)");

                b.Property<bool>("IsActive")
                    .HasColumnType("boolean");

                b.Property<DateTime?>("LastModifiedAtUtc")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("LastModifiedByUserId")
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<decimal>("Price")
                    .HasPrecision(18, 2)
                    .HasColumnType("numeric(18,2)");

                b.Property<string>("Sku")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)");

                b.Property<int>("StockQuantity")
                    .HasColumnType("integer");

                b.HasKey("Id");

                b.HasIndex("Sku")
                    .IsUnique();

                b.ToTable("products", (string)null);
            });
#pragma warning restore 612, 618
    }
}
