import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import './Navbar.css'

export default function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  if (!user) return null

  return (
    <nav className="navbar">
      <div className="container navbar-inner">
        <NavLink to="/expenses" className="navbar-brand">Approva</NavLink>

        <div className="navbar-nav">
          <NavLink to="/expenses" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            Expenses
          </NavLink>
          {user.role !== 'Employee' && (
            <NavLink to="/dashboard" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Dashboard
            </NavLink>
          )}
        </div>

        <div className="navbar-right">
          <div className="navbar-user">
            <span className="navbar-user-name">{user.name}</span>
            <span className="badge badge-submitted">{user.role}</span>
          </div>
          <button className="btn btn-secondary" onClick={handleLogout}>Logout</button>
        </div>
      </div>
    </nav>
  )
}
