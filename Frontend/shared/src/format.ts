const KARACHI = 'Asia/Karachi'
const nf = new Intl.NumberFormat('en-US')

/** Whole numbers with thousands separators: 12,450. */
export const num = (n: number | null | undefined) => nf.format(Math.round(n ?? 0))

/** Money is always Pakistani rupees, rounded to the rupee: "Rs 12,450". */
export const rs = (n: number | null | undefined) => `Rs ${num(n)}`

/** Today's business date in Karachi (yyyy-MM-dd) — never the device's UTC date. */
export function todayKarachi(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: KARACHI, year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date())
}

/** Adds days to a yyyy-MM-dd date. */
export function addDays(date: string, days: number): string {
  const d = new Date(`${date}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() + days)
  return d.toISOString().slice(0, 10)
}

/** "Mon 3 Nov" for a yyyy-MM-dd date. */
export function dayLabel(date: string): string {
  return new Date(`${date}T00:00:00Z`).toLocaleDateString('en-GB', { timeZone: 'UTC', weekday: 'short', day: 'numeric', month: 'short' })
}

/** "3 Nov 2026" for a yyyy-MM-dd date. */
export function dateLabel(date: string | null | undefined): string {
  if (!date) return '—'
  return new Date(`${date}T00:00:00Z`).toLocaleDateString('en-GB', { timeZone: 'UTC', day: 'numeric', month: 'short', year: 'numeric' })
}

/** "3 Nov, 14:05" in Karachi time for an instant. */
export function timeLabel(instant: string | null | undefined): string {
  if (!instant) return '—'
  return new Date(instant).toLocaleString('en-GB', { timeZone: KARACHI, day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: false })
}

/** "5 min ago", "3 h ago", "2 d ago". */
export function ago(instant: string | null | undefined): string {
  if (!instant) return '—'
  const minutes = Math.max(0, Math.round((Date.now() - new Date(instant).getTime()) / 60_000))
  if (minutes < 1) return 'just now'
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.round(minutes / 60)
  return hours < 24 ? `${hours} h ago` : `${Math.round(hours / 24)} d ago`
}

/** Hours until an instant (negative when overdue). */
export const hoursUntil = (instant: string) => (new Date(instant).getTime() - Date.now()) / 3_600_000

/** First letter for avatars. */
export const initial = (name: string) => name.trim().charAt(0).toUpperCase() || '?'
