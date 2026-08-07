using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
                    company_name = table.Column<string>(type: "text", nullable: false),
                    referral_code = table.Column<string>(type: "text", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buyers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                    amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    release_due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    allocated_quantity_pcs = table.Column<int>(type: "integer", nullable: false),
                    price_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    gross_payout_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    fee_per_pc_applied_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    platform_fee_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    total_payout_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    payout_paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    total_quantity_pcs = table.Column<int>(type: "integer", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    total_invoice_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    total_invoice_currency = table.Column<string>(type: "text", nullable: false),
                    total_spread_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    total_spread_currency = table.Column<string>(type: "text", nullable: false),
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
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raised_by_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    resolution_notes = table.Column<string>(type: "text", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    rate_to_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    influencer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    full_name = table.Column<string>(type: "text", nullable: false),
                    referral_code = table.Column<string>(type: "text", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_threads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    audience = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    link_type = table.Column<string>(type: "text", nullable: true),
                    link_id = table.Column<Guid>(type: "uuid", nullable: true),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    requirement_seller_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    posted_by = table.Column<string>(type: "text", nullable: false),
                    round_number = table.Column<int>(type: "integer", nullable: false),
                    price_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_rounds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    approval_status = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_price_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
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
                    item_name = table.Column<string>(type: "text", nullable: false),
                    grade = table.Column<string>(type: "text", nullable: true),
                    quantity_pcs = table.Column<int>(type: "integer", nullable: false),
                    destination_country = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
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
                    email = table.Column<string>(type: "text", nullable: true),
                    invited_by_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    proposed_price_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    current_price_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    fee_per_pc_applied_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    seller_notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    negotiation_state = table.Column<string>(type: "text", nullable: false),
                    last_actor = table.Column<string>(type: "text", nullable: true),
                    round_count = table.Column<int>(type: "integer", nullable: false),
                    last_action_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    public_alias = table.Column<string>(type: "text", nullable: false),
                    company_name = table.Column<string>(type: "text", nullable: false),
                    tier = table.Column<string>(type: "text", nullable: true),
                    kyc_verified = table.Column<bool>(type: "boolean", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    whats_app = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    reference_contact = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string>(type: "text", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_country = table.Column<string>(type: "text", nullable: false),
                    cost_per_pc_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_rates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agencies_referral_code",
                table: "agencies",
                column: "referral_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deals_deal_number",
                table: "deals",
                column: "deal_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_influencers_referral_code",
                table: "influencers",
                column: "referral_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_read_at_created_at",
                table: "notifications",
                columns: new[] { "user_id", "read_at", "created_at" });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agencies");

            migrationBuilder.DropTable(
                name: "audit_log");

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
                name: "notifications");

            migrationBuilder.DropTable(
                name: "offer_rounds");

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
        }
    }
}
