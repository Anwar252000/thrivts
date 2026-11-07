using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Finance;
using Thrivts.Domain.Identity;
using Thrivts.Domain.Operations;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class DailySubmissionConfiguration : IEntityTypeConfiguration<DailySubmission>
{
    public void Configure(EntityTypeBuilder<DailySubmission> b)
    {
        b.ToTable("daily_submissions");
        b.HasIndex(x => new { x.UserId, x.Section, x.BusinessDate }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.BusinessDate });
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.ToTable("expenses");
        b.Property(x => x.Description).HasMaxLength(200).IsRequired();
        b.Property(x => x.ReceiptLink).HasMaxLength(500);
        b.HasIndex(x => x.BusinessDate);
    }
}

public class BlockerConfiguration : IEntityTypeConfiguration<Blocker>
{
    public void Configure(EntityTypeBuilder<Blocker> b)
    {
        b.ToTable("blockers");
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.Responsible).HasMaxLength(80);
        b.HasIndex(x => x.BusinessDate);
    }
}

public class DaySummaryConfiguration : IEntityTypeConfiguration<DaySummary>
{
    public void Configure(EntityTypeBuilder<DaySummary> b)
    {
        b.ToTable("day_summaries");
        b.Property(x => x.FocusTomorrow).HasMaxLength(500);
        b.Property(x => x.OwnerActions).HasMaxLength(2000);
        b.Property(x => x.TeamReport).HasMaxLength(2000);
        b.HasIndex(x => x.BusinessDate).IsUnique();
    }
}

public class TeamCheckConfiguration : IEntityTypeConfiguration<TeamCheck>
{
    public void Configure(EntityTypeBuilder<TeamCheck> b)
    {
        b.ToTable("team_checks");
        b.Property(x => x.PersonName).HasMaxLength(60).IsRequired();
        b.Property(x => x.RoleLabel).HasMaxLength(60).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasIndex(x => new { x.BusinessDate, x.PersonName }).IsUnique();
    }
}

public class ZoneCheckConfiguration : IEntityTypeConfiguration<ZoneCheck>
{
    public void Configure(EntityTypeBuilder<ZoneCheck> b)
    {
        b.ToTable("zone_checks");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasIndex(x => new { x.BusinessDate, x.Zone }).IsUnique();
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.ToTable("attendance_records");
        b.Property(x => x.LabourerName).HasMaxLength(40).IsRequired();
        b.Property(x => x.OvertimeHours).HasPrecision(5, 2);
        b.HasOne<Labourer>().WithMany().HasForeignKey(x => x.LabourerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BusinessDate, x.LabourerId }).IsUnique();
    }
}

public class EscalationConfiguration : IEntityTypeConfiguration<Escalation>
{
    public void Configure(EntityTypeBuilder<Escalation> b)
    {
        b.ToTable("escalations");
        b.Property(x => x.FromName).HasMaxLength(AppUser.MaxDisplayNameLength).IsRequired();
        b.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Department).HasMaxLength(40);
        b.Property(x => x.Reply).HasMaxLength(2000);
        b.Ignore(x => x.HasUnreadReply);
        b.HasIndex(x => new { x.IsResolved, x.CreatedAt });
        b.HasIndex(x => x.FromUserId);
    }
}

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> b)
    {
        b.ToTable("approvals");
        b.Property(x => x.FromName).HasMaxLength(AppUser.MaxDisplayNameLength).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.ValueText).HasMaxLength(60);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> b)
    {
        b.ToTable("goals");
        b.Property(x => x.Label).HasMaxLength(200).IsRequired();
        b.Property(x => x.Target).HasMaxLength(200);
        b.ToTable(t => t.HasCheckConstraint("ck_goals_progress", "progress BETWEEN 0 AND 100"));
    }
}

public class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> b)
    {
        b.ToTable("activity_events");
        b.Property(x => x.Type).HasMaxLength(40).IsRequired();
        b.Property(x => x.Text).HasMaxLength(1000).IsRequired();
        b.Property(x => x.UserName).HasMaxLength(AppUser.MaxDisplayNameLength);
        b.HasIndex(x => new { x.Area, x.OccurredAt, x.Id });
        b.HasIndex(x => new { x.OccurredAt, x.Id });
    }
}

public class VendorInvoiceConfiguration : IEntityTypeConfiguration<VendorInvoice>
{
    public void Configure(EntityTypeBuilder<VendorInvoice> b)
    {
        b.ToTable("vendor_invoices");
        b.Property(x => x.Number).HasMaxLength(20).IsRequired();
        b.Property(x => x.SupplierName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        b.Property(x => x.LotSerial).HasMaxLength(20);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.VendorInvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Rejections).WithOne().HasForeignKey(x => x.VendorInvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Rejections).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => x.SupplierId);
        b.HasIndex(x => x.LotId).IsUnique().HasFilter("lot_id IS NOT NULL AND is_deleted = false");
    }
}

public class VendorInvoiceLineConfiguration : IEntityTypeConfiguration<VendorInvoiceLine>
{
    public void Configure(EntityTypeBuilder<VendorInvoiceLine> b)
    {
        b.ToTable("vendor_invoice_lines");
        b.Property(x => x.ItemName).HasMaxLength(160).IsRequired();
        b.Property(x => x.BuyPrice).HasPrecision(18, 2);
        b.Property(x => x.LineTotal).HasPrecision(18, 2);
    }
}

public class InvoiceRejectionConfiguration : IEntityTypeConfiguration<InvoiceRejection>
{
    public void Configure(EntityTypeBuilder<InvoiceRejection> b)
    {
        b.ToTable("invoice_rejections");
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(60);
        b.Ignore(x => x.Returned);
    }
}

public class VendorPaymentConfiguration : IEntityTypeConfiguration<VendorPayment>
{
    public void Configure(EntityTypeBuilder<VendorPayment> b)
    {
        b.ToTable("vendor_payments");
        b.Property(x => x.SupplierName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.SupplierId);
        b.HasIndex(x => x.PaidOn);
    }
}

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> b)
    {
        b.ToTable("stored_files");
        b.Property(x => x.ContentType).HasMaxLength(60).IsRequired();
        b.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.Property(x => x.UserName).HasMaxLength(AppUser.MaxDisplayNameLength);
        b.Property(x => x.EntityType).HasMaxLength(60).IsRequired();
        b.Property(x => x.Changes).HasColumnType("jsonb");
        b.Property(x => x.CorrelationId).HasMaxLength(64);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => new { x.OccurredAt, x.Id });
        b.HasIndex(x => x.UserId);
    }
}

public class SerialCounterConfiguration : IEntityTypeConfiguration<SerialCounter>
{
    public void Configure(EntityTypeBuilder<SerialCounter> b)
    {
        b.ToTable("serial_counters");
        b.HasKey(x => x.Name);
        b.Property(x => x.Name).HasMaxLength(40);
    }
}

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> b)
    {
        b.ToTable("idempotency_records");
        b.HasKey(x => x.Key);
        b.Property(x => x.Key).HasMaxLength(100);
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.HasIndex(x => x.ExpiresAt);
    }
}
