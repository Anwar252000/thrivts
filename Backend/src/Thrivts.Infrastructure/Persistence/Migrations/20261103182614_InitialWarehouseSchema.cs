using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thrivts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialWarehouseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    value_text = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approvals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blockers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    department = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    responsible = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    needs_owner = table.Column<bool>(type: "boolean", nullable: false),
                    escalation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blockers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    client_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    login_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    destination_country = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    order_received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    dispatch_date = table.Column<DateOnly>(type: "date", nullable: true),
                    boxes = table.Column<int>(type: "integer", nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
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
                    table.PrimaryKey("pk_client_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "daily_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_no_progress = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_submissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "day_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    rating = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    on_track = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    focus_tomorrow = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    owner_actions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    team_report = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_day_summaries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dispatches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    dispatch_date = table.Column<DateOnly>(type: "date", nullable: false),
                    order_received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    client_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    destination_country = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    container_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    total_boxes = table.Column<int>(type: "integer", nullable: true),
                    total_weight_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    total_pieces = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_dispatches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "escalations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    department = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reply = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    replied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replied_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reply_read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escalations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    has_receipt = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expenses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    progress = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goals", x => x.id);
                    table.CheckConstraint("ck_goals_progress", "progress BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    response_body = table.Column<byte[]>(type: "bytea", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_records", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "item_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    decided_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approved_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    sku_category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    grade_note = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    reject_rule = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criteria = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "labourers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_labourers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rejection_returns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slip_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sacks = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    total_pieces = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rejection_returns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prefix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    sku_category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    size = table.Column<int>(type: "integer", nullable: false),
                    is_custom = table.Column<bool>(type: "boolean", nullable: false),
                    qty_a = table.Column<int>(type: "integer", nullable: false),
                    qty_c = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    destination = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    client_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    packed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criteria_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    criteria_note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    shipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sacks", x => x.id);
                    table.CheckConstraint("ck_sacks_quantities", "qty_a >= 0 AND qty_c >= 0");
                });

            migrationBuilder.CreateTable(
                name: "serial_counters",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serial_counters", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "sku_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sack_size = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_categories", x => x.id);
                    table.CheckConstraint("ck_sku_categories_size", "sack_size BETWEEN 1 AND 1000");
                });

            migrationBuilder.CreateTable(
                name: "sku_dispatches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    client_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    sack_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_dispatches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sku_prefixes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    prefix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_prefixes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stored_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    content_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    size_bytes = table.Column<int>(type: "integer", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stored_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    city = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    since = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "team_checks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    person_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    role_label = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    attended = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    punctuality = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entry_submitted = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    has_issues = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_checks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    app = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    color = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    pin_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    failed_pin_attempts = table.Column<int>(type: "integer", nullable: false),
                    lockout_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "zone_checks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    zone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zone_checks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_order_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_order_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_order_payments_client_orders_client_order_id",
                        column: x => x.client_order_id,
                        principalTable: "client_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    normalized_item_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    required = table.Column<int>(type: "integer", nullable: false),
                    ready = table.Column<int>(type: "integer", nullable: false),
                    client_ready = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    seq = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_lines", x => x.id);
                    table.CheckConstraint("ck_order_lines_quantities", "required >= 0 AND ready >= 0 AND client_ready >= 0");
                    table.ForeignKey(
                        name: "fk_order_lines_client_orders_client_order_id",
                        column: x => x.client_order_id,
                        principalTable: "client_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buy_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sell_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sell_a = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sell_c = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_prices_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    labourer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    labourer_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    overtime_hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_sunday = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_records_labourers_labourer_id",
                        column: x => x.labourer_id,
                        principalTable: "labourers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sku_dispatch_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_dispatch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_dispatch_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_sku_dispatch_components_sku_dispatches_sku_dispatch_id",
                        column: x => x.sku_dispatch_id,
                        principalTable: "sku_dispatches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gate_exits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sacks = table.Column<int>(type: "integer", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gate_exits", x => x.id);
                    table.ForeignKey(
                        name: "fk_gate_exits_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sacks = table.Column<int>(type: "integer", nullable: false),
                    arrival_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gated_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    count_finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    grade_draft_saved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    supervisor_signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    supervisor_signed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supervisor_signature_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cosigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cosigned_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cosign_signature_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_back_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_back_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_back_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_lots", x => x.id);
                    table.CheckConstraint("ck_lots_sacks", "sacks > 0");
                    table.ForeignKey(
                        name: "fk_lots_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_invoices_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    vendor_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_payments_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lot_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    received = table.Column<int>(type: "integer", nullable: false),
                    qty_a = table.Column<int>(type: "integer", nullable: false),
                    qty_b = table.Column<int>(type: "integer", nullable: false),
                    qty_c = table.Column<int>(type: "integer", nullable: false),
                    rejected = table.Column<int>(type: "integer", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    returned_rejected = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lot_items", x => x.id);
                    table.CheckConstraint("ck_lot_items_non_negative", "received >= 0 AND qty_a >= 0 AND qty_b >= 0 AND qty_c >= 0 AND rejected >= 0");
                    table.CheckConstraint("ck_lot_items_returned", "returned_rejected >= 0 AND returned_rejected <= rejected");
                    table.ForeignKey(
                        name: "fk_lot_items_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lot_items_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_rejections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    rejected = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    accepted = table.Column<int>(type: "integer", nullable: false),
                    accept_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_rejections", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_rejections_vendor_invoices_vendor_invoice_id",
                        column: x => x.vendor_invoice_id,
                        principalTable: "vendor_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_invoice_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    pieces = table.Column<int>(type: "integer", nullable: false),
                    buy_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_accepted_rejection = table.Column<bool>(type: "boolean", nullable: false),
                    merged_from = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_invoice_lines_vendor_invoices_vendor_invoice_id",
                        column: x => x.vendor_invoice_id,
                        principalTable: "vendor_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rejection_return_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rejection_return_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rejection_return_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_rejection_return_lines_lot_items_lot_item_id",
                        column: x => x.lot_item_id,
                        principalTable: "lot_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rejection_return_lines_rejection_returns_rejection_return_id",
                        column: x => x.rejection_return_id,
                        principalTable: "rejection_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    arrival_date = table.Column<DateOnly>(type: "date", nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantity_in = table.Column<int>(type: "integer", nullable: false),
                    on_hand = table.Column<int>(type: "integer", nullable: false),
                    in_sacks = table.Column<int>(type: "integer", nullable: false),
                    dispatched_container = table.Column<int>(type: "integer", nullable: false),
                    dispatched_small = table.Column<int>(type: "integer", nullable: false),
                    dispatched_sku = table.Column<int>(type: "integer", nullable: false),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_balances", x => x.id);
                    table.CheckConstraint("ck_stock_balances_in_sacks", "in_sacks >= 0 AND in_sacks <= on_hand");
                    table.CheckConstraint("ck_stock_balances_on_hand", "on_hand >= 0 AND on_hand <= quantity_in");
                    table.ForeignKey(
                        name: "fk_stock_balances_lot_items_lot_item_id",
                        column: x => x.lot_item_id,
                        principalTable: "lot_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_balances_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dispatch_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispatch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispatch_lines", x => x.id);
                    table.CheckConstraint("ck_dispatch_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_dispatch_lines_dispatches_dispatch_id",
                        column: x => x.dispatch_id,
                        principalTable: "dispatches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dispatch_lines_stock_balances_stock_balance_id",
                        column: x => x.stock_balance_id,
                        principalTable: "stock_balances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sack_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_serial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    item_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sack_components", x => x.id);
                    table.CheckConstraint("ck_sack_components_quantity", "quantity >= 0");
                    table.ForeignKey(
                        name: "fk_sack_components_sacks_sack_id",
                        column: x => x.sack_id,
                        principalTable: "sacks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sack_components_stock_balances_stock_balance_id",
                        column: x => x.stock_balance_id,
                        principalTable: "stock_balances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activity_events_area_occurred_at_id",
                table: "activity_events",
                columns: new[] { "area", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_events_occurred_at_id",
                table: "activity_events",
                columns: new[] { "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_approvals_status_created_at",
                table: "approvals",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_business_date_labourer_id",
                table: "attendance_records",
                columns: new[] { "business_date", "labourer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_labourer_id",
                table: "attendance_records",
                column: "labourer_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_occurred_at_id",
                table: "audit_logs",
                columns: new[] { "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_blockers_business_date",
                table: "blockers",
                column: "business_date");

            migrationBuilder.CreateIndex(
                name: "ix_client_order_payments_client_order_id",
                table: "client_order_payments",
                column: "client_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_orders_is_completed_deadline",
                table: "client_orders",
                columns: new[] { "is_completed", "deadline" });

            migrationBuilder.CreateIndex(
                name: "ix_client_orders_login_code",
                table: "client_orders",
                column: "login_code",
                unique: true,
                filter: "login_code IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_client_orders_normalized_name",
                table: "client_orders",
                column: "normalized_name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_daily_submissions_user_id_business_date",
                table: "daily_submissions",
                columns: new[] { "user_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_daily_submissions_user_id_section_business_date",
                table: "daily_submissions",
                columns: new[] { "user_id", "section", "business_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_day_summaries_business_date",
                table: "day_summaries",
                column: "business_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_lines_dispatch_id",
                table: "dispatch_lines",
                column: "dispatch_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_lines_stock_balance_id",
                table: "dispatch_lines",
                column: "stock_balance_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispatches_number",
                table: "dispatches",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dispatches_type_dispatch_date",
                table: "dispatches",
                columns: new[] { "type", "dispatch_date" });

            migrationBuilder.CreateIndex(
                name: "ix_escalations_from_user_id",
                table: "escalations",
                column: "from_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_escalations_is_resolved_created_at",
                table: "escalations",
                columns: new[] { "is_resolved", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_business_date",
                table: "expenses",
                column: "business_date");

            migrationBuilder.CreateIndex(
                name: "ix_gate_exits_created_at",
                table: "gate_exits",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_gate_exits_serial",
                table: "gate_exits",
                column: "serial",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gate_exits_supplier_id",
                table: "gate_exits",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_expires_at",
                table: "idempotency_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_rejections_vendor_invoice_id",
                table: "invoice_rejections",
                column: "vendor_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_prices_item_id",
                table: "item_prices",
                column: "item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_requests_normalized_name",
                table: "item_requests",
                column: "normalized_name",
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_item_requests_status_created_at",
                table: "item_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_items_category",
                table: "items",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_items_normalized_name",
                table: "items",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_labourers_normalized_name",
                table: "labourers",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lot_items_item_id",
                table: "lot_items",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_lot_items_lot_id_normalized_item_name",
                table: "lot_items",
                columns: new[] { "lot_id", "normalized_item_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lots_arrival_date",
                table: "lots",
                column: "arrival_date");

            migrationBuilder.CreateIndex(
                name: "ix_lots_serial",
                table: "lots",
                column: "serial");

            migrationBuilder.CreateIndex(
                name: "ix_lots_stage_gated_at",
                table: "lots",
                columns: new[] { "stage", "gated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_lots_supplier_id",
                table: "lots",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_client_order_id_normalized_item_name",
                table: "order_lines",
                columns: new[] { "client_order_id", "normalized_item_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rejection_return_lines_lot_item_id",
                table: "rejection_return_lines",
                column: "lot_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_rejection_return_lines_rejection_return_id",
                table: "rejection_return_lines",
                column: "rejection_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_rejection_returns_business_date",
                table: "rejection_returns",
                column: "business_date");

            migrationBuilder.CreateIndex(
                name: "ix_rejection_returns_supplier_id",
                table: "rejection_returns",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_sack_components_sack_id",
                table: "sack_components",
                column: "sack_id");

            migrationBuilder.CreateIndex(
                name: "ix_sack_components_stock_balance_id",
                table: "sack_components",
                column: "stock_balance_id");

            migrationBuilder.CreateIndex(
                name: "ix_sacks_code",
                table: "sacks",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sacks_order_line_id",
                table: "sacks",
                column: "order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_sacks_status_destination",
                table: "sacks",
                columns: new[] { "status", "destination" });

            migrationBuilder.CreateIndex(
                name: "ix_sacks_status_normalized_item_name",
                table: "sacks",
                columns: new[] { "status", "normalized_item_name" });

            migrationBuilder.CreateIndex(
                name: "ix_sku_categories_normalized_name",
                table: "sku_categories",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sku_dispatch_components_sku_dispatch_id",
                table: "sku_dispatch_components",
                column: "sku_dispatch_id");

            migrationBuilder.CreateIndex(
                name: "ix_sku_dispatch_components_stock_balance_id",
                table: "sku_dispatch_components",
                column: "stock_balance_id");

            migrationBuilder.CreateIndex(
                name: "ix_sku_dispatches_occurred_at_id",
                table: "sku_dispatches",
                columns: new[] { "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_sku_prefixes_normalized_item_name",
                table: "sku_prefixes",
                column: "normalized_item_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sku_prefixes_prefix",
                table: "sku_prefixes",
                column: "prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_lot_id",
                table: "stock_balances",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_lot_item_id_grade",
                table: "stock_balances",
                columns: new[] { "lot_item_id", "grade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_normalized_item_name_grade_arrival_date",
                table: "stock_balances",
                columns: new[] { "normalized_item_name", "grade", "arrival_date" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_supplier_id",
                table: "stock_balances",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_lot_id",
                table: "stock_movements",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_occurred_at_id",
                table: "stock_movements",
                columns: new[] { "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_reference_id",
                table: "stock_movements",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_stock_balance_id",
                table: "stock_movements",
                column: "stock_balance_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_normalized_name",
                table: "suppliers",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_team_checks_business_date_person_name",
                table: "team_checks",
                columns: new[] { "business_date", "person_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_app_is_active_sort_order",
                table: "users",
                columns: new[] { "app", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_invoice_lines_vendor_invoice_id",
                table: "vendor_invoice_lines",
                column: "vendor_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_invoices_lot_id",
                table: "vendor_invoices",
                column: "lot_id",
                unique: true,
                filter: "lot_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_invoices_number",
                table: "vendor_invoices",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_invoices_supplier_id",
                table: "vendor_invoices",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payments_paid_on",
                table: "vendor_payments",
                column: "paid_on");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payments_supplier_id",
                table: "vendor_payments",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_zone_checks_business_date_zone",
                table: "zone_checks",
                columns: new[] { "business_date", "zone" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_events");

            migrationBuilder.DropTable(
                name: "approvals");

            migrationBuilder.DropTable(
                name: "attendance_records");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "blockers");

            migrationBuilder.DropTable(
                name: "client_order_payments");

            migrationBuilder.DropTable(
                name: "daily_submissions");

            migrationBuilder.DropTable(
                name: "day_summaries");

            migrationBuilder.DropTable(
                name: "dispatch_lines");

            migrationBuilder.DropTable(
                name: "escalations");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "gate_exits");

            migrationBuilder.DropTable(
                name: "goals");

            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "invoice_rejections");

            migrationBuilder.DropTable(
                name: "item_prices");

            migrationBuilder.DropTable(
                name: "item_requests");

            migrationBuilder.DropTable(
                name: "order_lines");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "rejection_return_lines");

            migrationBuilder.DropTable(
                name: "sack_components");

            migrationBuilder.DropTable(
                name: "serial_counters");

            migrationBuilder.DropTable(
                name: "sku_categories");

            migrationBuilder.DropTable(
                name: "sku_dispatch_components");

            migrationBuilder.DropTable(
                name: "sku_prefixes");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "stored_files");

            migrationBuilder.DropTable(
                name: "team_checks");

            migrationBuilder.DropTable(
                name: "vendor_invoice_lines");

            migrationBuilder.DropTable(
                name: "vendor_payments");

            migrationBuilder.DropTable(
                name: "zone_checks");

            migrationBuilder.DropTable(
                name: "labourers");

            migrationBuilder.DropTable(
                name: "dispatches");

            migrationBuilder.DropTable(
                name: "client_orders");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "rejection_returns");

            migrationBuilder.DropTable(
                name: "sacks");

            migrationBuilder.DropTable(
                name: "stock_balances");

            migrationBuilder.DropTable(
                name: "sku_dispatches");

            migrationBuilder.DropTable(
                name: "vendor_invoices");

            migrationBuilder.DropTable(
                name: "lot_items");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "lots");

            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
