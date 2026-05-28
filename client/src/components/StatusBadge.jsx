export default function StatusBadge({ status }) {
  const cls = `badge badge-${status?.toLowerCase()}`
  return <span className={cls}>{status}</span>
}
