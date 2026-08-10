import { useEffect, useRef, type ReactNode } from 'react'
import { animate, useInView, useMotionValue, useTransform, motion } from 'framer-motion'

interface CountUpProps {
  to: number
  suffix?: ReactNode
  className?: string
}

/** Animates 0 → `to` once the number scrolls into view — matches the live site's countUp(). */
export function CountUp({ to, suffix = '', className }: CountUpProps) {
  const ref = useRef<HTMLSpanElement>(null)
  const inView = useInView(ref, { once: true, amount: 0.5 })
  const count = useMotionValue(0)
  const rounded = useTransform(count, (v) => Math.round(v).toLocaleString())

  useEffect(() => {
    if (!inView) return
    const controls = animate(count, to, { duration: 1.8, ease: [0.19, 1, 0.22, 1] })
    return controls.stop
  }, [inView, to, count])

  return (
    <span ref={ref} className={className}>
      <motion.span>{rounded}</motion.span>
      {suffix}
    </span>
  )
}
