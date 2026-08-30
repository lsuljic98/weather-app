import { ConditionsCard } from './ConditionsCard'
import { RecentSearchesCard } from './RecentSearchesCard'
import { TopCitiesCard } from './TopCitiesCard'

export function StatisticsPage() {
  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <TopCitiesCard />
      <RecentSearchesCard />
      <div className="lg:col-span-2">
        <ConditionsCard />
      </div>
    </div>
  )
}
