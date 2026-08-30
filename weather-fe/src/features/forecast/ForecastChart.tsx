import { Bar, ComposedChart, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { ForecastPoint } from '../../api/weather'
import { ChartTooltip } from '../../components/ChartTooltip'
import { fullLabel, localHour, shortDay } from '../../lib/format'
import { formatMetric, METRIC_LABELS, metricValue, type Metric } from './filters'

interface ForecastChartProps {
  points: ForecastPoint[]
  metric: Metric
}

export function ForecastChart({ points, metric }: ForecastChartProps) {
  const data = points.map((p) => ({ ...p, value: metricValue(p, metric) }))
  const percent = metric === 'humidity' || metric === 'precipitation'

  return (
    <div className="chart h-64">
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
                <ChartTooltip title={fullLabel(p.localTime)}>
                  <p className="capitalize text-slate-500">{p.description}</p>
                  <p className="mt-1">
                    {METRIC_LABELS[metric]}: <span className="font-medium">{formatMetric(p.value, metric)}</span>
                  </p>
                </ChartTooltip>
              ) : null
            }}
          />
          {metric === 'precipitation' ? (
            <Bar dataKey="value" fill="var(--chart-series)" radius={[4, 4, 0, 0]} isAnimationActive={false} />
          ) : (
            <Line
              type="monotone"
              dataKey="value"
              stroke="var(--chart-series)"
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
