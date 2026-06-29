import { describe, it, expect } from 'vitest'
import { formatDueDate, dueStatus } from '../date'

describe('formatDueDate', () => {
  it('formats a yyyy-MM-dd date without shifting across timezones', () => {
    // Built from local calendar parts, so it stays the same day in every timezone — a naive
    // `new Date("2026-06-30")` parses as UTC midnight and renders Jun 29 west of UTC.
    expect(formatDueDate('2026-06-30')).toBe('Jun 30, 2026')
    expect(formatDueDate('2026-01-01')).toBe('Jan 1, 2026')
  })

  it('returns the raw value when it cannot be parsed', () => {
    expect(formatDueDate('not-a-date')).toBe('not-a-date')
  })
})

describe('dueStatus', () => {
  const today = new Date(2026, 5, 28) // Jun 28, 2026 (local)

  it('classifies a past date as overdue', () => {
    expect(dueStatus('2026-06-25', today)).toBe('overdue')
  })

  it('classifies the same calendar day as due today', () => {
    expect(dueStatus('2026-06-28', today)).toBe('today')
  })

  it('classifies a future date as upcoming', () => {
    expect(dueStatus('2026-07-02', today)).toBe('upcoming')
  })
})
