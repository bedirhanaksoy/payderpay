import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { session } from '../shared/auth/session'
import { authProvider } from '../shared/auth/provider'
import iconAccounts from '../assets/banking/nav-accounts.svg'
import iconTransactions from '../assets/banking/nav-transactions.svg'
import iconTransfers from '../assets/banking/nav-transfers.svg'
import iconProfile from '../assets/banking/nav-profile.svg'
import iconLogout from '../assets/banking/nav-logout.svg'

const navItems = [
  { to: '/dashboard', icon: iconAccounts, label: 'Dashboard' },
  { to: '/subscriptions', icon: iconTransfers, label: 'Subscriptions' },
  { to: '/billing', icon: iconTransactions, label: 'Debt & Payments' },
  { to: '/payments', icon: iconTransactions, label: 'Payment History' },
]

export default function Layout() {
  const navigate = useNavigate()
  const user = session.getUser()

  async function handleLogout() {
    try {
      await authProvider.logout()
    } catch {
      // best-effort logout
    }
    session.clear()
    navigate('/auth/login', { replace: true })
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-logo">
          <div className="sidebar-logo-brand">
            <div>
              <div className="sidebar-logo-mark">PayderPay</div>
              <div className="sidebar-logo-sub">Subscription Banking</div>
            </div>
          </div>
        </div>

        <nav className="sidebar-nav">
          {navItems.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `sidebar-item${isActive ? ' active' : ''}`}
            >
              <span className="sidebar-item-icon" aria-hidden>
                <img src={item.icon} alt="" />
              </span>
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <NavLink to="/profile" className={({ isActive }) => `sidebar-item${isActive ? ' active' : ''}`}>
            <span className="sidebar-item-icon" aria-hidden>
              <img src={iconProfile} alt="" />
            </span>
            Profile
          </NavLink>

          {user && (
            <div className="sidebar-user-block">
              <div className="sidebar-avatar" aria-hidden>
                {(user.fullName?.[0] ?? 'U').toUpperCase()}
              </div>
              <div className="sidebar-user-meta">
                <div className="sidebar-user-name">{user.fullName}</div>
                <div className="sidebar-user-email">{user.email}</div>
              </div>
            </div>
          )}

          <button className="sidebar-logout-btn" onClick={handleLogout}>
            <span className="sidebar-item-icon" aria-hidden>
              <img src={iconLogout} alt="" />
            </span>
            Sign out
          </button>
        </div>
      </aside>

      <main className="page-content">
        <Outlet />
      </main>
    </div>
  )
}
