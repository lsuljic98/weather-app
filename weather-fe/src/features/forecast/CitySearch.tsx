import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import type { City, CityRef } from '../../api/weather'
import { useCitySearch } from '../../hooks/useCitySearch'

interface CitySearchProps {
  onSelect: (ref: CityRef) => void
}

export function CitySearch({ onSelect }: CitySearchProps) {
  const [input, setInput] = useState('')
  const [open, setOpen] = useState(false)
  const [highlighted, setHighlighted] = useState(0)
  const listId = useId()
  const root = useRef<HTMLDivElement>(null)
  const { data: cities = [], isFetching, enabled } = useCitySearch(input)

  const showList = open && enabled && (cities.length > 0 || !isFetching)

  useEffect(() => {
    const close = (e: MouseEvent) => {
      if (!root.current?.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', close)
    return () => document.removeEventListener('mousedown', close)
  }, [])

  const commit = (ref: CityRef, label: string) => {
    onSelect(ref)
    setInput(label)
    setOpen(false)
  }

  const pick = (c: City) => commit({ city: c.name, countryCode: c.country }, labelFor(c))

  const submitTyped = () => {
    const city = input.trim()
    if (city) commit({ city }, city)
  }

  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault()
        setOpen(true)
        setHighlighted((i) => Math.min(i + 1, cities.length - 1))
        break
      case 'ArrowUp':
        e.preventDefault()
        setHighlighted((i) => Math.max(i - 1, 0))
        break
      case 'Enter': {
        e.preventDefault()
        const chosen = showList ? cities[highlighted] : undefined
        if (chosen) pick(chosen)
        else submitTyped()
        break
      }
      case 'Escape':
        setOpen(false)
        break
    }
  }

  return (
    <div ref={root} className="relative">
      <label htmlFor={`${listId}-input`} className="sr-only">
        City
      </label>
      <input
        id={`${listId}-input`}
        type="search"
        role="combobox"
        aria-expanded={showList}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={showList && cities[highlighted] ? `${listId}-${highlighted}` : undefined}
        placeholder="Search for a city…"
        autoComplete="off"
        value={input}
        onChange={(e) => {
          setInput(e.target.value)
          setHighlighted(0)
          setOpen(true)
        }}
        onFocus={() => setOpen(true)}
        onKeyDown={onKeyDown}
        className="w-full rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm outline-none transition focus:ring-2 focus:ring-sky-500 dark:border-slate-700 dark:bg-slate-900"
      />

      {showList && (
        <ul
          id={listId}
          role="listbox"
          className="absolute z-10 mt-1 w-full overflow-hidden rounded-lg border border-slate-200 bg-white text-sm shadow-lg dark:border-slate-700 dark:bg-slate-900"
        >
          {cities.length === 0 ? (
            <li className="px-4 py-2 text-slate-500">No matches — press Enter to search anyway.</li>
          ) : (
            cities.map((c, i) => (
              <li
                key={`${c.name}-${c.state ?? ''}-${c.country}-${c.latitude}`}
                id={`${listId}-${i}`}
                role="option"
                aria-selected={i === highlighted}
                onMouseEnter={() => setHighlighted(i)}
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => pick(c)}
                className={`cursor-pointer px-4 py-2 ${
                  i === highlighted ? 'bg-sky-50 dark:bg-slate-800' : ''
                }`}
              >
                {c.name}
                <span className="ml-1 text-slate-500">
                  {c.state ? `${c.state}, ` : ''}
                  {c.country}
                </span>
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  )
}

const labelFor = (c: City) => `${c.name}, ${c.country}`
