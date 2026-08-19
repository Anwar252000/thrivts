using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Thrivts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_name = table.Column<string>(type: "text", nullable: false),
                    agency_code = table.Column<string>(type: "text", nullable: false),
                    owner_full_name = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: true),
                    team_size = table.Column<int>(type: "integer", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    total_buyers_referred = table.Column<int>(type: "integer", nullable: false),
                    total_deals_closed = table.Column<int>(type: "integer", nullable: false),
                    total_commission_earned_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    total_commission_paid_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    total_commission_pending_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    contract_signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    contract_document_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    approval_status = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_config",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_role = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blocked_ips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    blocked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    blocked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blocked_ips", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buyers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "text", nullable: false),
                    company_registration = table.Column<string>(type: "text", nullable: true),
                    vat_id = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: true),
                    website = table.Column<string>(type: "text", nullable: true),
                    instagram = table.Column<string>(type: "text", nullable: true),
                    social_media = table.Column<string>(type: "jsonb", nullable: true),
                    estimated_monthly_volume_pcs = table.Column<int>(type: "integer", nullable: true),
                    monthly_volume = table.Column<string>(type: "text", nullable: true),
                    typical_requirement_type = table.Column<string>(type: "text", nullable: true),
                    categories = table.Column<string>(type: "jsonb", nullable: true),
                    categories_of_interest = table.Column<string[]>(type: "text[]", nullable: true),
                    attributed_to_agency = table.Column<Guid>(type: "uuid", nullable: true),
                    attribution_locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    agency_ref = table.Column<string>(type: "text", nullable: true),
                    influencer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    referral_code_used = table.Column<string>(type: "text", nullable: true),
                    referral_linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    first_order_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    first_order_discount_applied = table.Column<bool>(type: "boolean", nullable: false),
                    total_orders = table.Column<int>(type: "integer", nullable: false),
                    total_spend_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    is_premium = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    name_fr = table.Column<string>(type: "text", nullable: true),
                    weight_per_piece_kg = table.Column<decimal>(type: "numeric", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "commissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    gross_spread_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    spread_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    net_settled_spread_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    commission_amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    final_commission_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    release_due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accrued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    released_by = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payout_method = table.Column<string>(type: "text", nullable: true),
                    payout_reference = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deal_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_response_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_response_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_number = table.Column<string>(type: "text", nullable: false),
                    allocated_quantity_pcs = table.Column<int>(type: "integer", nullable: false),
                    price_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    replaced_by_allocation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    backed_out_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    gross_payout_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    fee_per_pc_applied_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    platform_fee_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    total_payout_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    payout_scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payout_paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payout_method = table.Column<string>(type: "text", nullable: true),
                    payout_reference = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deal_allocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_number = table.Column<string>(type: "text", nullable: false),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_response_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    total_quantity_pcs = table.Column<int>(type: "integer", nullable: false),
                    buyer_price_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    avg_seller_price_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    seller_cost_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    spread_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    spread_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    subtotal_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    shipping_cost_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    shipping_quote_currency = table.Column<string>(type: "text", nullable: true),
                    total_invoice_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    total_spread_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    net_settled_spread_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    total_seller_payout_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    total_seller_cost_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    exchange_rate_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    destination_country = table.Column<string>(type: "text", nullable: true),
                    destination_port = table.Column<string>(type: "text", nullable: true),
                    payment_received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payment_method = table.Column<string>(type: "text", nullable: true),
                    payment_reference = table.Column<string>(type: "text", nullable: true),
                    payment_received_by = table.Column<Guid>(type: "uuid", nullable: true),
                    sellers_paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    container_number = table.Column<string>(type: "text", nullable: true),
                    container_size = table.Column<string>(type: "text", nullable: true),
                    shipping_line = table.Column<string>(type: "text", nullable: true),
                    vessel_name = table.Column<string>(type: "text", nullable: true),
                    bill_of_lading = table.Column<string>(type: "text", nullable: true),
                    tracking_number = table.Column<string>(type: "text", nullable: true),
                    tracking_url = table.Column<string>(type: "text", nullable: true),
                    courier = table.Column<string>(type: "text", nullable: true),
                    estimated_dispatch_date = table.Column<DateOnly>(type: "date", nullable: true),
                    estimated_arrival_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expected_delivery_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    has_dispute = table.Column<bool>(type: "boolean", nullable: false),
                    dispute_amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    admin_notes = table.Column<string>(type: "text", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    in_fulfillment_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    commission_release_due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispute_number = table.Column<string>(type: "text", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raised_by = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    requested_resolution = table.Column<string>(type: "text", nullable: true),
                    attachments = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    resolution = table.Column<string>(type: "text", nullable: true),
                    resolution_notes = table.Column<string>(type: "text", nullable: true),
                    refund_amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disputes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    currency = table.Column<string>(type: "text", nullable: false),
                    rate_to_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    usd_to_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    set_by = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "influencer_commissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    influencer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_value_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_influencer_commissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "influencers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    influencer_code = table.Column<string>(type: "text", nullable: false),
                    referral_code = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    instagram_handle = table.Column<string>(type: "text", nullable: true),
                    tiktok_handle = table.Column<string>(type: "text", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    window_months = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_influencers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "message_threads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    seller_response_id = table.Column<Guid>(type: "uuid", nullable: true),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    participant_role = table.Column<string>(type: "text", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: true),
                    is_open = table.Column<bool>(type: "boolean", nullable: false),
                    last_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    unread_for_admin = table.Column<bool>(type: "boolean", nullable: false),
                    unread_for_participant = table.Column<bool>(type: "boolean", nullable: false),
                    unread_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_threads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_type = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    attachments = table.Column<string>(type: "jsonb", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    ref_type = table.Column<string>(type: "text", nullable: true),
                    ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    language = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivery_status = table.Column<string>(type: "text", nullable: true),
                    delivery_error = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audience = table.Column<string>(type: "text", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: true),
                    link_type = table.Column<string>(type: "text", nullable: true),
                    link_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "offer_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    price_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_rounds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ops_chat",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sender = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    ts = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ops_chat", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ops_presence",
                columns: table => new
                {
                    who = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    online = table.Column<bool>(type: "boolean", nullable: false),
                    ts = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ops_presence", x => x.who);
                });

            migrationBuilder.CreateTable(
                name: "ops_tasks",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    w = table.Column<int>(type: "integer", nullable: true),
                    cat = table.Column<string>(type: "text", nullable: true),
                    own = table.Column<string>(type: "text", nullable: true),
                    p = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    due = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    ts = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ops_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "partner_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    instagram = table.Column<string>(type: "text", nullable: true),
                    tiktok = table.Column<string>(type: "text", nullable: true),
                    experience = table.Column<string>(type: "text", nullable: true),
                    platform = table.Column<string>(type: "text", nullable: true),
                    profile_link = table.Column<string>(type: "text", nullable: true),
                    about = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    influencer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partner_applications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_config",
                columns: table => new
                {
                    id = table.Column<bool>(type: "boolean", nullable: false),
                    fee_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    pkr_reference = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_fee_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_fee_overrides", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    total_requirements_posted = table.Column<int>(type: "integer", nullable: false),
                    total_deals_closed = table.Column<int>(type: "integer", nullable: false),
                    total_pcs_moved = table.Column<long>(type: "bigint", nullable: false),
                    total_volume_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    active_buyers = table.Column<int>(type: "integer", nullable: false),
                    active_sellers = table.Column<int>(type: "integer", nullable: false),
                    active_agencies = table.Column<int>(type: "integer", nullable: false),
                    countries_served = table.Column<int>(type: "integer", nullable: false),
                    today_requirements = table.Column<int>(type: "integer", nullable: false),
                    today_matches = table.Column<int>(type: "integer", nullable: false),
                    today_dispatched = table.Column<int>(type: "integer", nullable: false),
                    this_month_deals = table.Column<int>(type: "integer", nullable: false),
                    this_month_volume_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    last_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_stats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    whatsapp = table.Column<string>(type: "text", nullable: true),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    language_pref = table.Column<string>(type: "text", nullable: false),
                    approval_status = table.Column<string>(type: "text", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    signup_ip = table.Column<string>(type: "text", nullable: true),
                    signup_user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "requirement_seller_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_number = table.Column<string>(type: "text", nullable: true),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_name = table.Column<string>(type: "text", nullable: true),
                    quantity_pcs = table.Column<int>(type: "integer", nullable: true),
                    grade = table.Column<string>(type: "text", nullable: true),
                    offer_price_per_pc = table.Column<decimal>(type: "numeric", nullable: true),
                    offer_currency = table.Column<string>(type: "text", nullable: false),
                    offer_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    current_price_per_pc = table.Column<decimal>(type: "numeric", nullable: true),
                    exchange_rate_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    counter_price_per_pc = table.Column<decimal>(type: "numeric", nullable: true),
                    counter_currency = table.Column<string>(type: "text", nullable: true),
                    counter_notes = table.Column<string>(type: "text", nullable: true),
                    admin_notes = table.Column<string>(type: "text", nullable: true),
                    sent_by = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requirement_seller_offers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_number = table.Column<string>(type: "text", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    item_name = table.Column<string>(type: "text", nullable: false),
                    quantity_pcs = table.Column<int>(type: "integer", nullable: false),
                    grade = table.Column<string>(type: "text", nullable: false),
                    buyer_target_price_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    buyer_currency = table.Column<string>(type: "text", nullable: false),
                    buyer_target_price_original = table.Column<decimal>(type: "numeric", nullable: true),
                    buyer_exchange_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    seller_target_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    spread_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    total_spread_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    destination_country = table.Column<string>(type: "text", nullable: false),
                    destination_port = table.Column<string>(type: "text", nullable: true),
                    delivery_timeline_days = table.Column<int>(type: "integer", nullable: true),
                    preferred_dispatch_date = table.Column<DateOnly>(type: "date", nullable: true),
                    preferred_shipment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    shipping_mode = table.Column<string>(type: "text", nullable: true),
                    specific_brands = table.Column<string>(type: "text", nullable: true),
                    estimated_weight_kg = table.Column<decimal>(type: "numeric", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    requirement_type = table.Column<string>(type: "text", nullable: true),
                    public_display = table.Column<bool>(type: "boolean", nullable: false),
                    buyer_notes = table.Column<string>(type: "text", nullable: true),
                    admin_notes = table.Column<string>(type: "text", nullable: true),
                    additional_notes = table.Column<string>(type: "text", nullable: true),
                    min_seller_tier = table.Column<string>(type: "text", nullable: false),
                    restricted_to_tags = table.Column<string[]>(type: "text[]", nullable: true),
                    target_currency = table.Column<string>(type: "text", nullable: true),
                    target_price_per_piece = table.Column<decimal>(type: "numeric", nullable: true),
                    target_price_per_pc = table.Column<decimal>(type: "numeric", nullable: true),
                    target_price_currency = table.Column<string>(type: "text", nullable: true),
                    target_total_buyer_currency = table.Column<decimal>(type: "numeric", nullable: true),
                    target_total_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    exchange_rate_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    matched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requirements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seller_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    company_name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    used_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seller_invites", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seller_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    available_quantity_pcs = table.Column<int>(type: "integer", nullable: false),
                    proposed_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    current_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    fee_per_pc_applied_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    seller_notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    negotiation_state = table.Column<string>(type: "text", nullable: false),
                    last_actor = table.Column<string>(type: "text", nullable: true),
                    round_count = table.Column<int>(type: "integer", nullable: false),
                    last_action_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_quantity_pcs = table.Column<int>(type: "integer", nullable: true),
                    final_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    backed_out_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    backout_reason = table.Column<string>(type: "text", nullable: true),
                    buyer_counter_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    buyer_counter_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    buyer_counter_note = table.Column<string>(type: "text", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seller_responses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sellers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "text", nullable: true),
                    location_city = table.Column<string>(type: "text", nullable: false),
                    location_country = table.Column<string>(type: "text", nullable: false),
                    years_in_business = table.Column<int>(type: "integer", nullable: true),
                    social_media = table.Column<string>(type: "jsonb", nullable: true),
                    categories_supplied = table.Column<string[]>(type: "text[]", nullable: false),
                    monthly_volume_capacity_pcs = table.Column<int>(type: "integer", nullable: true),
                    reference_contact = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    whatsapp = table.Column<string>(type: "text", nullable: true),
                    tier = table.Column<string>(type: "text", nullable: false),
                    tier_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tier_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tier_notes = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    manual_tags = table.Column<string[]>(type: "text[]", nullable: true),
                    seller_code = table.Column<string>(type: "text", nullable: true),
                    public_alias = table.Column<string>(type: "text", nullable: false),
                    kyc_verified = table.Column<bool>(type: "boolean", nullable: false),
                    kyc_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    kyc_verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    kyc_notes = table.Column<string>(type: "text", nullable: true),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    invited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    invite_token = table.Column<string>(type: "text", nullable: true),
                    invite_accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_pre_loaded = table.Column<bool>(type: "boolean", nullable: false),
                    last_active_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_orders_fulfilled = table.Column<int>(type: "integer", nullable: false),
                    total_pcs_supplied = table.Column<long>(type: "bigint", nullable: false),
                    total_paid_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    dispute_count = table.Column<int>(type: "integer", nullable: false),
                    backout_count = table.Column<int>(type: "integer", nullable: false),
                    on_time_dispatch_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    avg_grade_accuracy = table.Column<decimal>(type: "numeric", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sellers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_rates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    destination_country = table.Column<string>(type: "text", nullable: false),
                    destination_port = table.Column<string>(type: "text", nullable: true),
                    container_size = table.Column<string>(type: "text", nullable: true),
                    rate_usd_per_kg = table.Column<decimal>(type: "numeric", nullable: true),
                    flat_rate_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    transit_days = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    zone_name = table.Column<string>(type: "text", nullable: true),
                    countries = table.Column<string[]>(type: "text[]", nullable: true),
                    currency = table.Column<string>(type: "text", nullable: false),
                    rate_per_kg = table.Column<decimal>(type: "numeric", nullable: true),
                    delivery_days_min = table.Column<int>(type: "integer", nullable: true),
                    delivery_days_max = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_entries",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stockout_entries",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stockout_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "translations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_translations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wash_received",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_received", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wash_sent",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_sent", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agencies_agency_code",
                table: "agencies",
                column: "agency_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deals_deal_number",
                table: "deals",
                column: "deal_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_disputes_dispute_number",
                table: "disputes",
                column: "dispute_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_influencers_influencer_code",
                table: "influencers",
                column: "influencer_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_influencers_referral_code",
                table: "influencers",
                column: "referral_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipient_id_is_read_created_at",
                table: "notifications",
                columns: new[] { "recipient_id", "is_read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_requirements_requirement_number",
                table: "requirements",
                column: "requirement_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seller_invites_token",
                table: "seller_invites",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sellers_public_alias",
                table: "sellers",
                column: "public_alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_translations_key_language",
                table: "translations",
                columns: new[] { "key", "language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agencies");

            migrationBuilder.DropTable(
                name: "app_config");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "blocked_ips");

            migrationBuilder.DropTable(
                name: "buyers");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "commissions");

            migrationBuilder.DropTable(
                name: "deal_allocations");

            migrationBuilder.DropTable(
                name: "deals");

            migrationBuilder.DropTable(
                name: "disputes");

            migrationBuilder.DropTable(
                name: "exchange_rates");

            migrationBuilder.DropTable(
                name: "influencer_commissions");

            migrationBuilder.DropTable(
                name: "influencers");

            migrationBuilder.DropTable(
                name: "message_threads");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "offer_rounds");

            migrationBuilder.DropTable(
                name: "ops_chat");

            migrationBuilder.DropTable(
                name: "ops_presence");

            migrationBuilder.DropTable(
                name: "ops_tasks");

            migrationBuilder.DropTable(
                name: "partner_applications");

            migrationBuilder.DropTable(
                name: "platform_fee_config");

            migrationBuilder.DropTable(
                name: "platform_fee_overrides");

            migrationBuilder.DropTable(
                name: "platform_stats");

            migrationBuilder.DropTable(
                name: "profiles");

            migrationBuilder.DropTable(
                name: "requirement_seller_offers");

            migrationBuilder.DropTable(
                name: "requirements");

            migrationBuilder.DropTable(
                name: "seller_invites");

            migrationBuilder.DropTable(
                name: "seller_responses");

            migrationBuilder.DropTable(
                name: "sellers");

            migrationBuilder.DropTable(
                name: "shipping_rates");

            migrationBuilder.DropTable(
                name: "stock_entries");

            migrationBuilder.DropTable(
                name: "stockout_entries");

            migrationBuilder.DropTable(
                name: "translations");

            migrationBuilder.DropTable(
                name: "wash_received");

            migrationBuilder.DropTable(
                name: "wash_sent");
        }
    }
}
