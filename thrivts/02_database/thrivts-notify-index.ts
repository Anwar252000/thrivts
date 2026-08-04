// supabase/functions/thrivts-notify/index.ts
// ---------------------------------------------------------------------------
// Thrivts email notifications via Resend  —  v2 (bidding model)
//
// Driven by a SINGLE Supabase Database Webhook on the `notifications` table:
//   Table:  notifications
//   Events: Insert
//   Method: POST, header  x-notify-secret: <NOTIFY_SECRET>
//   URL:    this function
//
// Because push_notification() writes a notifications row for EVERY event that
// matters (bid placed, counter received, bid accepted/declined, account
// approved, etc.), one webhook here emails all of them. New event types added
// in future are emailed automatically with no extra wiring.
//
// THE MOAT: notifications.title / body are written server-side and already
// respect anonymity (buyers see "Seller_7fx3", never a real name). This
// function only forwards that text — it never joins to identity tables — so it
// cannot leak a counterparty's identity.
//
// Secrets (unchanged from v1 — nothing to reset):
//   RESEND_API_KEY   (required)
//   RESEND_FROM      "Thrivts <notifications@thrivts.com>"  (domain verified)
//   NOTIFY_SECRET    must match the webhook header
//   BUYER_URL   SELLER_URL   ADMIN_URL   (portal links; optional, have defaults)
// SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY are injected automatically.
// ---------------------------------------------------------------------------
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const RESEND_API_KEY = Deno.env.get("RESEND_API_KEY") ?? "";
const RESEND_FROM    = Deno.env.get("RESEND_FROM") ?? "Thrivts <notifications@thrivts.com>";
const NOTIFY_SECRET  = Deno.env.get("NOTIFY_SECRET") ?? "";
const BUYER_URL      = Deno.env.get("BUYER_URL")  ?? "https://buyers.thrivts.com";
const SELLER_URL     = Deno.env.get("SELLER_URL") ?? "https://sellers.thrivts.com";
const ADMIN_URL      = Deno.env.get("ADMIN_URL")  ?? "https://vhq-backstage-7k2.thrivts.com";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SERVICE_KEY  = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
const db = createClient(SUPABASE_URL, SERVICE_KEY);

const SAGE = "#73837A";

// ---- Resend ---------------------------------------------------------------
async function sendEmail(to: string | string[], subject: string, html: string) {
  const recipients = (Array.isArray(to) ? to : [to]).filter(Boolean);
  if (!recipients.length) { console.log("skip: no recipient for:", subject); return; }
  if (!RESEND_API_KEY)   { console.error("RESEND_API_KEY not set"); return; }
  const res = await fetch("https://api.resend.com/emails", {
    method: "POST",
    headers: { "Authorization": `Bearer ${RESEND_API_KEY}`, "Content-Type": "application/json" },
    body: JSON.stringify({ from: RESEND_FROM, to: recipients, subject, html }),
  });
  if (!res.ok) console.error("Resend error", res.status, await res.text());
  else console.log("sent:", subject, "->", recipients.join(", "));
}

// ---- Recipient lookup (service role, bypasses RLS) ------------------------
async function emailFor(userId?: string | null): Promise<string | null> {
  if (!userId) return null;
  const { data } = await db.from("profiles").select("email").eq("id", userId).maybeSingle();
  return data?.email ?? null;
}

// ---- Which portal does this audience live in? -----------------------------
function portalUrl(audience?: string | null): string {
  if (audience === "seller") return SELLER_URL;
  if (audience === "admin")  return ADMIN_URL;
  return BUYER_URL;
}

// ---- Email template (unchanged brand from v1) -----------------------------
function wrap(title: string, bodyHtml: string, ctaText?: string, ctaUrl?: string) {
  const cta = ctaText && ctaUrl
    ? `<tr><td style="padding:8px 0 4px;">
         <a href="${ctaUrl}" style="display:inline-block;background:${SAGE};color:#fff;text-decoration:none;padding:11px 20px;border-radius:8px;font-weight:600;font-size:14px;">${ctaText}</a>
       </td></tr>` : "";
  return `<!doctype html><html><body style="margin:0;background:#f4f2ec;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#2b2b28;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f2ec;padding:24px 0;"><tr><td align="center">
      <table role="presentation" width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:14px;overflow:hidden;border:1px solid #e6e3da;">
        <tr><td style="background:${SAGE};padding:18px 24px;"><span style="color:#fff;font-size:18px;font-weight:700;letter-spacing:.02em;">Thrivts</span></td></tr>
        <tr><td style="padding:24px;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
            <tr><td style="font-size:17px;font-weight:700;padding-bottom:10px;">${escapeHtml(title)}</td></tr>
            <tr><td style="font-size:14px;line-height:1.55;color:#43433d;">${bodyHtml}</td></tr>
            ${cta}
          </table>
        </td></tr>
        <tr><td style="padding:16px 24px;border-top:1px solid #eee;font-size:12px;color:#9a988f;">
          Thrivts - B2B vintage wholesale. You're receiving this because you have an active account.
        </td></tr>
      </table>
    </td></tr></table>
  </body></html>`;
}
function escapeHtml(s: string) {
  return String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]!));
}

// A friendly call-to-action label per notification kind.
function ctaFor(kind?: string | null): string {
  switch (kind) {
    case "bid_placed":       return "View bids";
    case "counter":          return "View offer";
    case "counter_accepted": return "Review & confirm";
    case "counter_declined": return "View requirement";
    case "accepted":         return "View deal";
    case "req_live":         return "View requirement";
    case "account_approved": return "Open Thrivts";
    default:                 return "Open Thrivts";
  }
}

// ---- HTTP entry -----------------------------------------------------------
Deno.serve(async (req) => {
  if (req.method === "GET") return new Response("thrivts-notify v2 ok", { status: 200 });
  if (NOTIFY_SECRET && req.headers.get("x-notify-secret") !== NOTIFY_SECRET) {
    return new Response("unauthorized", { status: 401 });
  }

  let payload: any;
  try { payload = await req.json(); } catch { return new Response("bad json", { status: 400 }); }

  const { type, table, record } = payload ?? {};

  try {
    if (table === "notifications" && type === "INSERT" && record) {
      // recipient column is recipient_id in this schema; fall back to user_id
      // for safety in case an older row uses it.
      const recipientId = record.recipient_id ?? record.user_id;
      const to = await emailFor(recipientId);

      if (to) {
        const bodyHtml = record.body
          ? escapeHtml(record.body).replace(/\n/g, "<br>")
          : "You have a new update on Thrivts.";
        await sendEmail(
          to,
          `Thrivts - ${record.title ?? "New notification"}`,
          wrap(
            record.title ?? "New notification",
            bodyHtml,
            ctaFor(record.kind),
            portalUrl(record.audience),
          ),
        );
      } else {
        console.log("no email on profile for recipient", recipientId);
      }
    }
  } catch (e) {
    console.error("handler error", e);
    // Return 200 regardless so the webhook never retry-storms. Errors logged.
  }

  return new Response(JSON.stringify({ ok: true }), {
    status: 200, headers: { "Content-Type": "application/json" },
  });
});
