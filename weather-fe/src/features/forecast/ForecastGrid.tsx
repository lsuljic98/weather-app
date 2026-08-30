import {
  createColumnHelper,
  createSortedRowModel,
  rowSortingFeature,
  tableFeatures,
  useTable,
} from '@tanstack/react-table'
import type { ForecastPoint } from '../../api/weather'
import type { Metric } from './filters'
import { fullLabel } from './format'

const features = tableFeatures({
  rowSortingFeature,
  sortedRowModel: createSortedRowModel(),
})
const helper = createColumnHelper<typeof features, ForecastPoint>()

const columns = helper.columns([
  helper.accessor('localTime', {
    id: 'time',
    header: 'Time',
    cell: (info) => fullLabel(info.getValue()),
  }),
  helper.accessor('description', {
    id: 'condition',
    header: 'Conditions',
    cell: (info) => (
      <span className="flex items-center gap-1 capitalize">
        {info.row.original.icon && (
          <img
            src={`https://openweathermap.org/img/wn/${info.row.original.icon}.png`}
            alt=""
            width={28}
            height={28}
            className="-my-2"
          />
        )}
        {info.getValue()}
      </span>
    ),
  }),
  helper.accessor('temperatureC', {
    id: 'temperature',
    header: 'Temp',
    cell: (info) => `${Math.round(info.getValue())}°`,
  }),
  helper.accessor('humidity', {
    id: 'humidity',
    header: 'Humidity',
    cell: (info) => `${info.getValue()}%`,
  }),
  helper.accessor('windSpeed', {
    id: 'wind',
    header: 'Wind',
    cell: (info) => `${info.getValue().toFixed(1)} m/s`,
  }),
  helper.accessor('precipitationProbability', {
    id: 'precipitation',
    header: 'Rain',
    cell: (info) => `${Math.round(info.getValue() * 100)}%`,
  }),
])

interface ForecastGridProps {
  points: ForecastPoint[]
  metric: Metric
}

export function ForecastGrid({ points, metric }: ForecastGridProps) {
  const table = useTable({ features, columns, data: points })
  const numeric = (id: string) => id !== 'time' && id !== 'condition'

  return (
    <div className="overflow-x-auto rounded-xl border border-slate-200 dark:border-slate-800">
      <table className="w-full text-sm">
        <thead className="bg-slate-50 text-xs text-slate-500 dark:bg-slate-900">
          {table.getHeaderGroups().map((group) => (
            <tr key={group.id}>
              {group.headers.map((header) => {
                const sorted = header.column.getIsSorted()
                return (
                  <th
                    key={header.id}
                    scope="col"
                    aria-sort={sorted === 'asc' ? 'ascending' : sorted === 'desc' ? 'descending' : 'none'}
                    className={`px-3 py-2 font-medium ${numeric(header.column.id) ? 'text-right' : 'text-left'} ${
                      header.column.id === metric ? 'text-sky-600 dark:text-sky-400' : ''
                    }`}
                  >
                    <button
                      type="button"
                      onClick={header.column.getToggleSortingHandler()}
                      className="inline-flex items-center gap-1 hover:text-slate-900 dark:hover:text-slate-100"
                    >
                      <table.FlexRender header={header} />
                      <span aria-hidden="true" className="w-3">
                        {sorted === 'asc' ? '▲' : sorted === 'desc' ? '▼' : ''}
                      </span>
                    </button>
                  </th>
                )
              })}
            </tr>
          ))}
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
          {table.getRowModel().rows.map((row) => (
            <tr key={row.id}>
              {row.getAllCells().map((cell) => (
                <td
                  key={cell.id}
                  className={`px-3 py-2 whitespace-nowrap ${numeric(cell.column.id) ? 'text-right tabular-nums' : ''} ${
                    cell.column.id === metric ? 'bg-sky-50/60 font-medium dark:bg-slate-800/60' : ''
                  }`}
                >
                  <table.FlexRender cell={cell} />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
