import { Logo } from '@/components/marketing/Logo'
import { FacebookIcon, InstagramIcon, XIcon } from '@/components/marketing/SocialIcons'

const COLUMNS = [
  { title: 'Explore', links: [{ label: 'About', href: '#about' }, { label: 'Categories', href: '#categories' }, { label: 'Suppliers', href: '#vendors' }, { label: 'The App', href: '#app' }] },
  { title: 'Portals', links: [{ label: 'Buyer Portal', href: '/login' }, { label: 'Seller Portal', href: '/login' }] },
  { title: 'Partners & Agencies', links: [{ label: 'Partner Program', href: '/login' }, { label: 'Agency Portal', href: '/login' }] },
  { title: 'Company', links: [{ label: 'Contact', href: '#contact' }, { label: 'Buyer Protection', href: '#' }, { label: 'Shipping', href: '#' }, { label: 'Privacy', href: '#' }] },
  { title: 'Connect', links: [{ label: 'Instagram', href: 'https://instagram.com/thrivts' }, { label: 'Facebook', href: 'https://facebook.com/thrivts' }, { label: 'X / Twitter', href: 'https://x.com/thrivts' }] },
]

export function MarketingFooter() {
  return (
    <footer className="relative overflow-hidden bg-[var(--color-ink)] pb-9 pt-20 text-[var(--color-cream)]">
      <div
        aria-hidden
        className="mb-14 select-none text-center text-[clamp(5rem,21vw,18rem)] font-black lowercase leading-[0.78] tracking-tight"
      >
        <Logo className="text-transparent [-webkit-text-stroke:1px_rgba(252,251,247,.16)]" sparkClassName="text-[rgba(174,184,177,.5)] [-webkit-text-stroke:0]" />
      </div>

      <div className="w-[min(1240px,90vw)] mx-auto">
        <div className="grid grid-cols-2 gap-10 border-b border-white/12 pb-12 md:grid-cols-[2.2fr_1fr_1fr_1fr_1fr]">
          <div>
            <a href="#top">
              <Logo className="text-3xl text-[var(--color-cream)]" />
            </a>
            <p className="mt-4.5 max-w-[32ch] text-[0.98rem] leading-relaxed text-[rgba(252,251,247,.66)]">
              Connecting vintage buyers with verified suppliers worldwide. Built by wholesale
              professionals with over 10 years of industry experience.
            </p>
          </div>
          {COLUMNS.map((col) => (
            <div key={col.title}>
              <h5 className="mb-5 text-xs uppercase tracking-[0.2em] text-[var(--color-sage-soft)]">{col.title}</h5>
              {col.links.map((link) => (
                <a
                  key={link.label}
                  href={link.href}
                  className="block py-1.5 text-sm text-[rgba(252,251,247,.74)] transition-all duration-300 hover:translate-x-1.5 hover:text-[var(--color-cream)]"
                >
                  {link.label}
                </a>
              ))}
            </div>
          ))}
        </div>

        <div className="flex flex-wrap items-center justify-between gap-4.5 pt-7">
          <small className="text-xs uppercase tracking-wide text-[rgba(252,251,247,.5)]">
            © {new Date().getFullYear()} Thrivts. All rights reserved.
          </small>
          <div className="flex gap-2.5">
            {[
              { icon: FacebookIcon, href: 'https://facebook.com/thrivts', label: 'Facebook' },
              { icon: InstagramIcon, href: 'https://instagram.com/thrivts', label: 'Instagram' },
              { icon: XIcon, href: 'https://x.com/thrivts', label: 'X' },
            ].map((s) => (
              <a
                key={s.label}
                href={s.href}
                aria-label={s.label}
                className="flex size-10.5 items-center justify-center rounded-full border border-white/18 transition-all duration-500 hover:-translate-y-1 hover:border-[var(--color-sage)] hover:bg-[var(--color-sage)]"
              >
                <s.icon size={16} />
              </a>
            ))}
          </div>
        </div>
      </div>
    </footer>
  )
}
