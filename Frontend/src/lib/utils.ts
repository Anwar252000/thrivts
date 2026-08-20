import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

const usdFormatter = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
const numberFormatter = new Intl.NumberFormat('en-US')

export function formatUsd(value: number): string {
  return usdFormatter.format(value)
}

export function formatNumber(value: number): string {
  return numberFormatter.format(value)
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  return new Date(value).toLocaleDateString('en-US', { day: 'numeric', month: 'short', year: 'numeric' })
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—'
  return new Date(value).toLocaleString('en-US', { day: 'numeric', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit' })
}

/** Downloads `rows` as a CSV file — only flat (non-object) fields are included, and any value
 * starting with =+-@ is quote-prefixed to defuse spreadsheet formula injection on open. */
export function exportToCsv<T extends object>(filename: string, rows: T[]): void {
  if (rows.length === 0) return

  const first = rows[0] as Record<string, unknown>
  const headers = Object.keys(first).filter((k) => typeof first[k] !== 'object' || first[k] === null)
  const csv = [
    headers.join(','),
    ...rows.map((row) =>
      headers
        .map((h) => {
          const v = (row as Record<string, unknown>)[h]
          if (v === null || v === undefined) return ''
          let s = String(v).replace(/"/g, '""')
          if (/^[=+\-@]/.test(s)) s = "'" + s
          return /[",\n]/.test(s) ? `"${s}"` : s
        })
        .join(','),
    ),
  ].join('\n')

  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `thrivts_${filename}_${new Date().toISOString().slice(0, 10)}.csv`
  a.click()
  URL.revokeObjectURL(url)
}
