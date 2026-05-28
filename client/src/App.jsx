import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import Navbar from './components/Navbar'
import LoginPage from './pages/LoginPage'
import ExpenseListPage from './pages/ExpenseListPage'
import SubmitExpensePage from './pages/SubmitExpensePage'
import DashboardPage from './pages/DashboardPage'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Navbar />
        <Routes>
          <Route path="/" element={<Navigate to="/expenses" replace />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/expenses" element={<ExpenseListPage />} />
          <Route path="/submit" element={<SubmitExpensePage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
