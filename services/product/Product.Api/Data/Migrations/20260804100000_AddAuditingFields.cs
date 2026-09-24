using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Product.Api.Data.Migrations;

/// <inheritdoc />
public partial class AddAuditingFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "UpdatedAtUtc",
            table: "products",
            newName: "LastModifiedAtUtc");

        migrationBuilder.AddColumn<string>(
            name: "CreatedByUserId",
            table: "products",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LastModifiedByUserId",
            table: "products",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreatedByUserId",
            table: "products");

        migrationBuilder.DropColumn(
            name: "LastModifiedByUserId",
            table: "products");

        migrationBuilder.RenameColumn(
            name: "LastModifiedAtUtc",
            table: "products",
            newName: "UpdatedAtUtc");
    }
}
