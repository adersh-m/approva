import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { getDashboard } from '../api/expenses'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorMessage from '../components/ErrorMessage'
import './DashboardPage.css'

const PERIODS = [
  { label: 'Last 7 days', days: 7 },
  { label: 'Last 30 days', days: 30 },
  { label: 'Last 90 days', days: 90 },
]

function toISO(date) {
  return date.toISOString()
}

function formatAmount(n) {
  return n?.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00'
}

export default function DashboardPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [periodDays, setPeriodDays] = useState(30)
  const [summary, setSummary] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!user) { navigate('/login'); return }
    if (user.role === 'Employee') { navigate('/expenses'); return }
  }, [user, navigate])

  const fetchDashboard = useCallback(() => {
    if (!user || user.role === 'Employee') return
    setLoading(true)
    setError('')
    const end = new Date()
    const start = new Date()
    start.setDate(start.getDate() - periodDays)
    getDashboard({ periodStart: toISO(start), periodEnd: toISO(end) })
      .then(res => setSummary(res.data))
      .catch(err => setError(err.message))
      .finally(() => setLoading(false))
  }, [user, periodDays])

  useEffect(() => { fetchDashboard() }, [fetchDashboard])

  if (!user || user.role === 'Employee') return null

  return (
    <div className="container dashboard-page">
      <div className="page-header">
        <h1>Dashboard</h1>
      </div>

      <div className="period-selector">
        {PERIODS.map(p => (
          <button
            key={p.days}
            className={`period-btn${periodDays === p.days ? ' active' : ''}`}
            onClick={() => setPeriodDays(p.days)}
          >
            {p.label}
          </button>
        ))}
      </div>

      <ErrorMessage message={error} />

      {loading ? (
        <LoadingSpinner />
      ) : summary ? (
        <div className="summary-grid">
          <SummaryCard label="Total Expenses" count={summary.totalExpenses} amount={summary.totalAmount} />
          <SummaryCard label="Pending" count={summary.pendingCount} amount={summary.pendingAmount} />
          <SummaryCard label="Approved" count={summary.approvedCount} amount={summary.approvedAmount} />
          <SummaryCard label="Rejected" count={summary.rejectedCount} amount={summary.rejectedAmount} />
          <SummaryCard label="Reimbursed" count={summary.reimbursedCount} amount={summary.reimbursedAmount} />
        </div>
      ) : null}
    </div>
  )
}

function SummaryCard({ label, count, amount }) {
  return (
    <div className="card summary-card">
      <span className="summary-card-label">{label}</span>
      <span className="summary-card-count">{count ?? 0}</span>
      <span className="summary-card-amount">${formatAmount(amount)}</span>
    </div>
  )
}
