export function Placeholder({ title }: { title: string }) {
  return (
    <div>
      <div className="section-header">
        <div className="section-tag">Coming next</div>
        <h2>{title}</h2>
        <p>This screen is being moved to the new system and will appear here shortly.</p>
      </div>
    </div>
  )
}
