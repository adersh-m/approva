export default function ErrorMessage({ message }) {
  if (!message) return null
  return (
    <div style={{
      background: '#fee2e2',
      border: '1px solid #fca5a5',
      borderRadius: 'var(--radius-sm)',
      color: 'var(--color-danger)',
      padding: 'var(--space-sm) var(--space-md)',
      fontSize: 'var(--font-sm)',
      marginBottom: 'var(--space-md)',
    }}>
      {message}
    </div>
  )
}
