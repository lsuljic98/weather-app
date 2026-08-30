import type { ReactNode } from 'react'

interface WeatherIconProps {
  /** OpenWeather icon code, e.g. "01d", "10n". */
  code: string
  size?: number
  /** Accessible name; omit when the text next to the icon already says it. */
  label?: string
  className?: string
}

// Inline replacements for OpenWeather's PNGs, whose "clear night" is a flat grey
// disc. Codes: 01 clear, 02 few clouds, 03 scattered, 04 broken, 09 shower rain,
// 10 rain, 11 thunderstorm, 13 snow, 50 mist; suffix d/n = day/night.
export function WeatherIcon({ code, size = 24, label, className = '' }: WeatherIconProps) {
  const night = code.endsWith('n')

  return (
    <svg
      viewBox="0 0 24 24"
      width={size}
      height={size}
      fill="none"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      role={label ? 'img' : undefined}
      aria-hidden={label ? undefined : true}
      className={`shrink-0 [--wi-cloud:#f8fafc] dark:[--wi-cloud:#cbd5e1] ${className}`}
    >
      {label && <title>{label}</title>}
      {glyphFor(code.slice(0, 2), night)}
    </svg>
  )
}

function glyphFor(kind: string, night: boolean): ReactNode {
  const orb = night ? <Moon /> : <Sun />
  const smallOrb = <g transform="translate(0.5 0.5) scale(0.55)">{orb}</g>

  switch (kind) {
    case '01':
      return orb
    case '02':
      return (
        <>
          {smallOrb}
          <Cloud transform="translate(2 3)" />
        </>
      )
    case '03':
      return <Cloud />
    case '04':
      return (
        <>
          <Cloud transform="translate(4 -3) scale(0.8)" faded />
          <Cloud transform="translate(-1 2)" />
        </>
      )
    case '09':
      return (
        <>
          <RainCloud />
          <Rain />
        </>
      )
    case '10':
      return (
        <>
          {smallOrb}
          <RainCloud />
          <Rain />
        </>
      )
    case '11':
      return (
        <>
          <RainCloud />
          <path d="M13 11l-4 6h6l-4 6" stroke="#f59e0b" fill="#fcd34d" />
        </>
      )
    case '13':
      return (
        <>
          <RainCloud />
          <g stroke="#38bdf8" strokeWidth={2}>
            <path d="M8 16h.01M8 20h.01M12 18h.01M12 22h.01M16 16h.01M16 20h.01" />
          </g>
        </>
      )
    case '50':
      return <path d="M3 8h13M6 12h15M4 16h12M9 20h9" stroke="#94a3b8" />
    default:
      return <Cloud />
  }
}

const Sun = () => (
  <g stroke="#f59e0b">
    <circle cx="12" cy="12" r="5" fill="#fcd34d" />
    <path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42" />
  </g>
)

const Moon = () => <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" stroke="#94a3b8" fill="#e2e8f0" />

const Cloud = ({ transform, faded }: { transform?: string; faded?: boolean }) => (
  <path
    d="M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z"
    transform={transform}
    stroke="#94a3b8"
    fill="var(--wi-cloud)"
    opacity={faded ? 0.7 : 1}
  />
)

// Open at the bottom so rain, snow and lightning can hang from it.
const RainCloud = () => (
  <path d="M20 16.58A5 5 0 0 0 18 7h-1.26A8 8 0 1 0 4 15.25" stroke="#94a3b8" fill="var(--wi-cloud)" />
)

const Rain = () => <path d="M16 13v6M8 13v6M12 15v6" stroke="#0ea5e9" />
