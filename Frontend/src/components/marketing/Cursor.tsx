import { useEffect, useState } from 'react'
import { motion, useMotionValue, useSpring } from 'framer-motion'

/** Custom dot+ring cursor with a magnetic hover state — desktop/mouse only. */
export function Cursor() {
  const [enabled] = useState(() => typeof window !== 'undefined' && window.matchMedia('(hover: hover)').matches)
  const [hovering, setHovering] = useState(false)
  const x = useMotionValue(-100)
  const y = useMotionValue(-100)
  const ringX = useSpring(x, { damping: 25, stiffness: 300, mass: 0.5 })
  const ringY = useSpring(y, { damping: 25, stiffness: 300, mass: 0.5 })

  useEffect(() => {
    if (!enabled) return

    const onMove = (e: MouseEvent) => {
      x.set(e.clientX)
      y.set(e.clientY)
    }
    window.addEventListener('mousemove', onMove)

    const interactive = 'a, button, [data-cursor-hover]'
    const onOver = (e: MouseEvent) => {
      if ((e.target as Element)?.closest?.(interactive)) setHovering(true)
    }
    const onOut = (e: MouseEvent) => {
      if ((e.target as Element)?.closest?.(interactive)) setHovering(false)
    }
    document.addEventListener('mouseover', onOver)
    document.addEventListener('mouseout', onOut)

    return () => {
      window.removeEventListener('mousemove', onMove)
      document.removeEventListener('mouseover', onOver)
      document.removeEventListener('mouseout', onOut)
    }
  }, [enabled, x, y])

  if (!enabled) return null

  return (
    <>
      <motion.div
        className="pointer-events-none fixed left-0 top-0 z-[99999] rounded-full bg-[var(--color-sage-dark)]"
        style={{ x, y, translateX: '-50%', translateY: '-50%' }}
        animate={{ width: hovering ? 0 : 7, height: hovering ? 0 : 7 }}
        transition={{ duration: 0.3 }}
      />
      <motion.div
        className="pointer-events-none fixed left-0 top-0 z-[99999] rounded-full border"
        style={{ x: ringX, y: ringY, translateX: '-50%', translateY: '-50%' }}
        animate={{
          width: hovering ? 58 : 34,
          height: hovering ? 58 : 34,
          borderColor: hovering ? 'var(--color-sage)' : 'rgba(115,131,122,.5)',
          backgroundColor: hovering ? 'rgba(115,131,122,.06)' : 'transparent',
        }}
        transition={{ duration: 0.4, ease: [0.19, 1, 0.22, 1] }}
      />
    </>
  )
}
