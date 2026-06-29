// Helpers for a task's due date. The API models it as a DateOnly calendar date (no time, no zone)
// and serializes it as "yyyy-MM-dd". Everything here treats it as a *local* calendar date so a
// task never slips a day across timezones — `new Date("2026-06-30")` parses as UTC midnight and
// renders as Jun 29 anywhere west of UTC, which is exactly the bug we avoid.

export type DueStatus = 'overdue' | 'today' | 'upcoming'

interface DateParts {
  year: number
  month: number
  day: number
}

function parse(value: string): DateParts | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value)
  if (!match) {
    return null
  }
  return { year: Number(match[1]), month: Number(match[2]), day: Number(match[3]) }
}

/** A Date at local midnight for the given calendar parts. */
function toLocalDate(parts: DateParts): Date {
  return new Date(parts.year, parts.month - 1, parts.day)
}

/** Formats a "yyyy-MM-dd" due date as e.g. "Jun 30, 2026"; returns the raw value if unparseable. */
export function formatDueDate(value: string): string {
  const parts = parse(value)
  if (!parts) {
    return value
  }
  return toLocalDate(parts).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

/**
 * Classifies a due date relative to today's local calendar date: overdue, due today, or upcoming.
 * `today` is injectable so the logic is deterministically testable.
 */
export function dueStatus(value: string, today: Date = new Date()): DueStatus {
  const parts = parse(value)
  if (!parts) {
    return 'upcoming'
  }
  const due = toLocalDate(parts).getTime()
  const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime()
  if (due < startOfToday) {
    return 'overdue'
  }
  if (due === startOfToday) {
    return 'today'
  }
  return 'upcoming'
}
