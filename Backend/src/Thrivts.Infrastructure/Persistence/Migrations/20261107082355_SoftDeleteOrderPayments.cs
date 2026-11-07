using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thrivts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteOrderPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "client_order_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "client_order_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "client_order_payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "client_order_payments");

            migrationBuilder.DropColumn(
                name: "deleted_by_id",
                table: "client_order_payments");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "client_order_payments");
        }
    }
}
