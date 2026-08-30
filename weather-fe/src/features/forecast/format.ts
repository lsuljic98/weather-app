import { localDate } from './filters'

const atNoon = (date: string) => new Date(`${date}T12:00:00`)

export function weekday(date: string) {
  return atNoon(date).toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
}

export function shortDay(iso: string) {
  return atNoon(localDate(iso)).toLocaleDateString(undefined, { weekday: 'short' })
}

/** "Sat 30 Aug 15:00" from the API's offset-carrying ISO string. */
export function fullLabel(iso: string) {
  return `${weekday(localDate(iso))} ${iso.slice(11, 16)}`
}
