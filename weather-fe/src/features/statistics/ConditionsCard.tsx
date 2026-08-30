import { Bar, BarChart, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { useConditionDistribution } from '../../hooks/useStatistics'
import { StatCard } from './StatCard'

const ROW_HEIGHT = 32

export function ConditionsCard() {
  const { data = [], isPending, isError } = useConditionDistribution()
  const total = data.reduce((sum, c) => sum + c.count, 0)

  return (
    <StatCard
      title="Conditions at search time"
      isPending={isPending}
      isError={isError}
      isEmpty={data.length === 0}
      emptyMessage="Nothing to chart yet."
    >
      {/* Single series, sorted by the server, so one hue and direct labels are enough. */}
      <div
        className="text-slate-600 [--bar:var(--color-sky-600)] dark:text-slate-300 dark:[--bar:var(--color-sky-400)]"
        style={{ height: data.length * ROW_HEIGHT + 8 }}
      >
        <ResponsiveContainer>
          <BarChart data={data} layout="vertical" margin={{ top: 0, right: 40, bottom: 0, left: 0 }} barSize={16}>
            <XAxis type="number" hide allowDecimals={false} />
            <YAxis
              type="category"
              dataKey="condition"
              width={96}
              axisLine={false}
              tickLine={false}
              tick={{ fill: 'currentColor', fontSize: 13 }}
            />
            <Tooltip
              cursor={{ fill: 'currentColor', fillOpacity: 0.06 }}
              content={({ active, payload }) =>
                active && payload?.[0] ? (
                  <div className="rounded-md border border-slate-200 bg-white px-3 py-2 text-xs shadow-md dark:border-slate-700 dark:bg-slate-900">
                    <p className="font-medium">{String(payload[0].payload.condition)}</p>
                    <p className="text-slate-500">
                      {String(payload[0].value)} of {total} ({Math.round((Number(payload[0].value) / total) * 100)}%)
                    </p>
                  </div>
                ) : null
              }
            />
            <Bar dataKey="count" fill="var(--bar)" radius={[0, 4, 4, 0]} isAnimationActive={false}>
              <LabelList dataKey="count" position="right" fill="currentColor" fontSize={13} />
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </StatCard>
  )
}
