import { GrainOverlay } from '@/components/marketing/GrainOverlay'
import { Cursor } from '@/components/marketing/Cursor'
import { Loader } from '@/components/marketing/Loader'
import { MarketingNav } from '@/components/marketing/MarketingNav'
import { Ticker } from '@/components/marketing/Ticker'
import { Hero } from './landing/sections/Hero'
import { IntroLine } from './landing/sections/IntroLine'
import { TrustStats } from './landing/sections/TrustStats'
import { About } from './landing/sections/About'
import { StatsDark } from './landing/sections/StatsDark'
import { Categories } from './landing/sections/Categories'
import { Vendors } from './landing/sections/Vendors'
import { Benefits } from './landing/sections/Benefits'
import { Testimonials } from './landing/sections/Testimonials'
import { AppTeaser } from './landing/sections/AppTeaser'
import { Contact } from './landing/sections/Contact'
import { MarketingFooter } from './landing/sections/MarketingFooter'

const TICKER_ITEMS = [
  'Source',
  'Grade',
  'Grow Your Vintage Business',
  'Verified Suppliers',
  'Transparent Grading',
  'Global Network',
]

export function Landing() {
  return (
    <div className="selection:bg-[var(--color-sage)] selection:text-[var(--color-cream)]">
      <Loader />
      <GrainOverlay />
      <Cursor />
      <MarketingNav />

      <Hero />
      <IntroLine />
      <TrustStats />
      <Ticker items={TICKER_ITEMS} />
      <About />
      <StatsDark />
      <Categories />
      <Vendors />
      <Benefits />
      <Testimonials />
      <AppTeaser />
      <Contact />
      <MarketingFooter />
    </div>
  )
}
