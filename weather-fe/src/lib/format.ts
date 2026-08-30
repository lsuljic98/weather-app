// The forecast API's localTime carries the city's own UTC offset, so its date and
// hour are read straight off the string rather than through the browser timezone.
export const localDate = (iso: string) => iso.slice(0, 10)
export const localHour = (iso: string) => Number(iso.slice(11, 13))

const atNoon = (date: string) => new Date(`${date}T12:00:00`)

/** "Sat 30 Aug" for a "YYYY-MM-DD" date. */
export function weekday(date: string) {
  return atNoon(date).toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
}

/** "Sat" for an offset-carrying ISO string. */
export function shortDay(iso: string) {
  return atNoon(localDate(iso)).toLocaleDateString(undefined, { weekday: 'short' })
}

/** "Sat 30 Aug 15:00" for an offset-carrying ISO string. */
export function fullLabel(iso: string) {
  return `${weekday(localDate(iso))} ${iso.slice(11, 16)}`
}

/** Instant → local "30 Aug 2026, 15:04". */
export function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** Instant → local "15:04". */
export function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
}
