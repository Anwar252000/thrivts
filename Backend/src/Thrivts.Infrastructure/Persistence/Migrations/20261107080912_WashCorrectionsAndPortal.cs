using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thrivts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WashCorrectionsAndPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_balances_on_hand",
                table: "stock_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lot_items_non_negative",
                table: "lot_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lot_items_returned",
                table: "lot_items");

            migrationBuilder.AddColumn<int>(
                name: "at_wash",
                table: "stock_balances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "wash_in",
                table: "stock_balances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "wash_lost",
                table: "stock_balances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "wash_rejected",
                table: "lot_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "client_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    client_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    requested_when = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    is_done = table.Column<bool>(type: "boolean", nullable: false),
                    done_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    done_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_messages_client_orders_client_order_id",
                        column: x => x.client_order_id,
                        principalTable: "client_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wash_item_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_item_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wash_vendors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_vendors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wash_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    sent_date = table.Column<DateOnly>(type: "date", nullable: false),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalized_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_wash_batches_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wash_batches_wash_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "wash_vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wash_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rate_per_piece = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_rates", x => x.id);
                    table.CheckConstraint("ck_wash_rates_rate", "rate_per_piece >= 0");
                    table.ForeignKey(
                        name: "fk_wash_rates_wash_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "wash_vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wash_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wash_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sent = table.Column<int>(type: "integer", nullable: false),
                    price_per_piece = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    back_a = table.Column<int>(type: "integer", nullable: false),
                    back_c = table.Column<int>(type: "integer", nullable: false),
                    rejected = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_lines", x => x.id);
                    table.CheckConstraint("ck_wash_lines_back", "back_a >= 0 AND back_c >= 0 AND rejected >= 0 AND back_a + back_c + rejected <= sent");
                    table.CheckConstraint("ck_wash_lines_price", "price_per_piece >= 0");
                    table.CheckConstraint("ck_wash_lines_sent", "sent > 0");
                    table.ForeignKey(
                        name: "fk_wash_lines_lot_items_lot_item_id",
                        column: x => x.lot_item_id,
                        principalTable: "lot_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wash_lines_stock_balances_stock_balance_id",
                        column: x => x.stock_balance_id,
                        principalTable: "stock_balances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wash_lines_wash_batches_wash_batch_id",
                        column: x => x.wash_batch_id,
                        principalTable: "wash_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_balances_conserved",
                table: "stock_balances",
                sql: "quantity_in + wash_in = on_hand + at_wash + dispatched_container + dispatched_small + dispatched_sku + wash_lost");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_balances_on_hand",
                table: "stock_balances",
                sql: "on_hand >= 0 AND on_hand <= quantity_in + wash_in");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_balances_wash",
                table: "stock_balances",
                sql: "at_wash >= 0 AND wash_in >= 0 AND wash_lost >= 0 AND quantity_in >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lot_items_non_negative",
                table: "lot_items",
                sql: "received >= 0 AND qty_a >= 0 AND qty_b >= 0 AND qty_c >= 0 AND rejected >= 0 AND wash_rejected >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lot_items_returned",
                table: "lot_items",
                sql: "returned_rejected >= 0 AND returned_rejected <= rejected + wash_rejected");

            migrationBuilder.CreateIndex(
                name: "ix_client_messages_client_order_id",
                table: "client_messages",
                column: "client_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_messages_is_done_created_at",
                table: "client_messages",
                columns: new[] { "is_done", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_wash_batches_created_at_id",
                table: "wash_batches",
                columns: new[] { "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_wash_batches_lot_id",
                table: "wash_batches",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_batches_number",
                table: "wash_batches",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wash_batches_status_sent_date",
                table: "wash_batches",
                columns: new[] { "status", "sent_date" });

            migrationBuilder.CreateIndex(
                name: "ix_wash_batches_vendor_id",
                table: "wash_batches",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_item_categories_normalized_item_name",
                table: "wash_item_categories",
                column: "normalized_item_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wash_lines_lot_item_id",
                table: "wash_lines",
                column: "lot_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_lines_stock_balance_id",
                table: "wash_lines",
                column: "stock_balance_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_lines_wash_batch_id",
                table: "wash_lines",
                column: "wash_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_rates_vendor_id_category",
                table: "wash_rates",
                columns: new[] { "vendor_id", "category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wash_vendors_normalized_name",
                table: "wash_vendors",
                column: "normalized_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_messages");

            migrationBuilder.DropTable(
                name: "wash_item_categories");

            migrationBuilder.DropTable(
                name: "wash_lines");

            migrationBuilder.DropTable(
                name: "wash_rates");

            migrationBuilder.DropTable(
                name: "wash_batches");

            migrationBuilder.DropTable(
                name: "wash_vendors");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_balances_conserved",
                table: "stock_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_balances_on_hand",
                table: "stock_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_balances_wash",
                table: "stock_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lot_items_non_negative",
                table: "lot_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_lot_items_returned",
                table: "lot_items");

            migrationBuilder.DropColumn(
                name: "at_wash",
                table: "stock_balances");

            migrationBuilder.DropColumn(
                name: "wash_in",
                table: "stock_balances");

            migrationBuilder.DropColumn(
                name: "wash_lost",
                table: "stock_balances");

            migrationBuilder.DropColumn(
                name: "wash_rejected",
                table: "lot_items");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_balances_on_hand",
                table: "stock_balances",
                sql: "on_hand >= 0 AND on_hand <= quantity_in");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lot_items_non_negative",
                table: "lot_items",
                sql: "received >= 0 AND qty_a >= 0 AND qty_b >= 0 AND qty_c >= 0 AND rejected >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_lot_items_returned",
                table: "lot_items",
                sql: "returned_rejected >= 0 AND returned_rejected <= rejected");
        }
    }
}
