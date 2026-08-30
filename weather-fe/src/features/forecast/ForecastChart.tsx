import { Bar, ComposedChart, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { ForecastPoint } from '../../api/weather'
import { formatMetric, localHour, METRIC_LABELS, metricValue, type Metric } from './filters'
import { fullLabel, shortDay } from './format'

interface ForecastChartProps {
  points: ForecastPoint[]
  metric: Metric
}

export function ForecastChart({ points, metric }: ForecastChartProps) {
  const data = points.map((p) => ({ ...p, value: metricValue(p, metric) }))
  const percent = metric === 'humidity' || metric === 'precipitation'

  return (
    <div className="h-64 text-slate-600 [--series:var(--color-sky-600)] dark:text-slate-300 dark:[--series:var(--color-sky-400)]">
      <ResponsiveContainer>
        <ComposedChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: -16 }}>
          <XAxis
            dataKey="localTime"
            tickFormatter={tickLabel}
            interval="preserveStartEnd"
            minTickGap={48}
            axisLine={false}
            tickLine={false}
            tick={{ fill: 'currentColor', fontSize: 12 }}
          />
          <YAxis
            domain={percent ? [0, 100] : ['auto', 'auto']}
            tickFormatter={(v: number) => formatMetric(v, metric)}
            axisLine={false}
            tickLine={false}
            width={56}
            tick={{ fill: 'currentColor', fontSize: 12 }}
          />
          <Tooltip
            cursor={{ stroke: 'currentColor', strokeOpacity: 0.2 }}
            content={({ active, payload }) => {
              const p = payload?.[0]?.payload as (typeof data)[number] | undefined
              return active && p ? (
                <div className="rounded-md border border-slate-200 bg-white px-3 py-2 text-xs shadow-md dark:border-slate-700 dark:bg-slate-900">
                  <p className="font-medium">{fullLabel(p.localTime)}</p>
                  <p className="capitalize text-slate-500">{p.description}</p>
                  <p className="mt-1">
                    {METRIC_LABELS[metric]}: <span className="font-medium">{formatMetric(p.value, metric)}</span>
                  </p>
                </div>
              ) : null
            }}
          />
          {metric === 'precipitation' ? (
            <Bar dataKey="value" fill="var(--series)" radius={[4, 4, 0, 0]} isAnimationActive={false} />
          ) : (
            <Line
              type="monotone"
              dataKey="value"
              stroke="var(--series)"
              strokeWidth={2}
              dot={false}
              activeDot={{ r: 4 }}
              isAnimationActive={false}
            />
          )}
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}

const tickLabel = (iso: string) => {
  const hour = localHour(iso)
  return hour === 0 || hour === 12 ? `${shortDay(iso)} ${String(hour).padStart(2, '0')}h` : `${hour}h`
}
