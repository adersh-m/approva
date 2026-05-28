import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { submitExpense } from '../api/expenses'
import { getCategories, getDepartments } from '../api/reference'
import ErrorMessage from '../components/ErrorMessage'
import LoadingSpinner from '../components/LoadingSpinner'
import './SubmitExpensePage.css'

export default function SubmitExpensePage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const idempotencyKey = useRef(crypto.randomUUID())

  const [categories, setCategories] = useState([])
  const [departments, setDepartments] = useState([])
  const [loadingRef, setLoadingRef] = useState(true)

  const [form, setForm] = useState({
    title: '',
    amount: '',
    currencyCode: 'USD',
    receiptUrl: '',
    categoryId: '',
    departmentId: '',
  })
  const [errors, setErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)
  const [apiError, setApiError] = useState('')

  useEffect(() => {
    if (!user) { navigate('/login'); return }
    Promise.all([getCategories(), getDepartments()])
      .then(([cats, depts]) => {
        setCategories(cats.data ?? [])
        setDepartments(depts.data ?? [])
      })
      .catch(err => setApiError(err.message))
      .finally(() => setLoadingRef(false))
  }, [user, navigate])

  function validate() {
    const errs = {}
    if (!form.title.trim()) errs.title = 'Title is required'
    if (!form.amount || parseFloat(form.amount) <= 0) errs.amount = 'Amount must be greater than 0'
    if (!form.categoryId) errs.categoryId = 'Category is required'
    if (!form.departmentId) errs.departmentId = 'Department is required'
    return errs
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setApiError('')
    const errs = validate()
    if (Object.keys(errs).length > 0) { setErrors(errs); return }
    setErrors({})
    setSubmitting(true)
    try {
      await submitExpense(
        {
          title: form.title,
          amount: parseFloat(form.amount),
          currencyCode: form.currencyCode || 'USD',
          receiptUrl: form.receiptUrl || undefined,
          categoryId: form.categoryId,
          departmentId: form.departmentId,
        },
        idempotencyKey.current,
      )
      navigate('/expenses')
    } catch (err) {
      setApiError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  function set(field) {
    return e => setForm(f => ({ ...f, [field]: e.target.value }))
  }

  if (!user) return null
  if (loadingRef) return <LoadingSpinner />

  return (
    <div className="submit-page">
      <div className="page-header">
        <h1>Submit Expense</h1>
      </div>

      <div className="card">
        <ErrorMessage message={apiError} />
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label">Title</label>
            <input
              className={`form-input${errors.title ? ' error' : ''}`}
              value={form.title}
              onChange={set('title')}
              maxLength={200}
            />
            {errors.title && <span className="form-error">{errors.title}</span>}
          </div>

          <div className="form-group">
            <label className="form-label">Amount</label>
            <input
              type="number"
              step="0.01"
              min="0.01"
              className={`form-input${errors.amount ? ' error' : ''}`}
              value={form.amount}
              onChange={set('amount')}
            />
            {errors.amount && <span className="form-error">{errors.amount}</span>}
          </div>

          <div className="form-group">
            <label className="form-label">Currency Code</label>
            <input
              className="form-input"
              value={form.currencyCode}
              onChange={set('currencyCode')}
              maxLength={3}
            />
          </div>

          <div className="form-group">
            <label className="form-label">Receipt URL (optional)</label>
            <input
              type="url"
              className="form-input"
              value={form.receiptUrl}
              onChange={set('receiptUrl')}
            />
          </div>

          <div className="form-group">
            <label className="form-label">Category</label>
            <select
              className={`form-input${errors.categoryId ? ' error' : ''}`}
              value={form.categoryId}
              onChange={set('categoryId')}
            >
              <option value="">Select category</option>
              {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
            {errors.categoryId && <span className="form-error">{errors.categoryId}</span>}
          </div>

          <div className="form-group">
            <label className="form-label">Department</label>
            <select
              className={`form-input${errors.departmentId ? ' error' : ''}`}
              value={form.departmentId}
              onChange={set('departmentId')}
            >
              <option value="">Select department</option>
              {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
            {errors.departmentId && <span className="form-error">{errors.departmentId}</span>}
          </div>

          <div style={{ display: 'flex', gap: 'var(--space-md)', justifyContent: 'flex-end' }}>
            <button type="button" className="btn btn-secondary" onClick={() => navigate('/expenses')}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? 'Submitting…' : 'Submit'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
