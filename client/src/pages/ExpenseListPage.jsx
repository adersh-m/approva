import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { getExpenses, approveExpense, rejectExpense } from '../api/expenses'
import { getDepartments } from '../api/reference'
import StatusBadge from '../components/StatusBadge'
import Pagination from '../components/Pagination'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorMessage from '../components/ErrorMessage'
import './ExpenseListPage.css'

const STATUS_OPTIONS = ['Submitted', 'Approved', 'Rejected', 'Reimbursed', 'Draft']

function generateUUID() {
  return crypto.randomUUID()
}

function formatDate(dateStr) {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleDateString()
}

export default function ExpenseListPage() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const [expenses, setExpenses] = useState([])
  const [totalPages, setTotalPages] = useState(1)
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [departments, setDepartments] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!user) { navigate('/login'); return }
  }, [user, navigate])

  useEffect(() => {
    if (user?.role === 'FinanceAdmin') {
      getDepartments()
        .then(res => setDepartments(res.data ?? []))
        .catch(() => {})
    }
  }, [user])

  const fetchExpenses = useCallback(() => {
    if (!user) return
    setLoading(true)
    setError('')
    getExpenses({ page, pageSize: 20, status: status || undefined, departmentId: departmentId || undefined })
      .then(res => {
        setExpenses(res.data?.items ?? [])
        const total = res.data?.totalCount ?? 0
        setTotalPages(Math.max(1, Math.ceil(total / 20)))
      })
      .catch(err => setError(err.message))
      .finally(() => setLoading(false))
  }, [user, page, status, departmentId])

  useEffect(() => { fetchExpenses() }, [fetchExpenses])

  async function handleApprove(id) {
    if (!confirm('Approve this expense?')) return
    try {
      await approveExpense(id, generateUUID())
      fetchExpenses()
    } catch (err) {
      setError(err.message)
    }
  }

  async function handleReject(id) {
    const reason = prompt('Rejection reason:')
    if (!reason) return
    try {
      await rejectExpense(id, reason, generateUUID())
      fetchExpenses()
    } catch (err) {
      setError(err.message)
    }
  }

  if (!user) return null

  return (
    <div className="container" style={{ paddingTop: 'var(--space-lg)' }}>
      <div className="page-header">
        <h1>Expenses</h1>
        {user.role === 'Employee' && (
          <button className="btn btn-primary" onClick={() => navigate('/submit')}>
            Submit Expense
          </button>
        )}
      </div>

      <div className="expense-filters">
        {(user.role === 'Manager' || user.role === 'FinanceAdmin') && (
          <select
            className="form-input"
            value={status}
            onChange={e => { setStatus(e.target.value); setPage(1) }}
          >
            <option value="">All Statuses</option>
            {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        )}
        {user.role === 'FinanceAdmin' && (
          <select
            className="form-input"
            value={departmentId}
            onChange={e => { setDepartmentId(e.target.value); setPage(1) }}
          >
            <option value="">All Departments</option>
            {departments.map(d => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
        )}
      </div>

      <ErrorMessage message={error} />

      {loading ? (
        <LoadingSpinner />
      ) : (
        <div className="card" style={{ padding: 0 }}>
          <div className="expense-table-wrap">
            <table className="expense-table">
              <thead>
                <tr>
                  <th>Title</th>
                  <th>Amount</th>
                  <th>Category</th>
                  <th>Department</th>
                  <th>Status</th>
                  <th>Submitted</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {expenses.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="empty-state">No expenses found.</td>
                  </tr>
                ) : expenses.map(exp => (
                  <tr key={exp.id}>
                    <td>{exp.title}</td>
                    <td>{exp.amount.toFixed(2)} {exp.currencyCode}</td>
                    <td>{exp.categoryName}</td>
                    <td>{exp.departmentName}</td>
                    <td><StatusBadge status={exp.status} /></td>
                    <td>{formatDate(exp.submittedAt)}</td>
                    <td>
                      <div className="expense-actions">
                        {user.role === 'Manager' && exp.status === 'Submitted' && (
                          <>
                            <button className="btn btn-primary" onClick={() => handleApprove(exp.id)}>
                              Approve
                            </button>
                            <button className="btn btn-danger" onClick={() => handleReject(exp.id)}>
                              Reject
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
    </div>
  )
}
