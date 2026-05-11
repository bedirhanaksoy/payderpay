import { Link } from 'react-router-dom'

const features = [
  {
    icon: '◈',
    title: 'Subscription Hub',
    text: 'Keep electricity, water, internet and GSM subscriptions in one place.',
  },
  {
    icon: '⌁',
    title: 'Debt Query',
    text: 'Check period-based debt details from provider integrations before payment.',
  },
  {
    icon: '↕',
    title: 'Main Account Balance',
    text: 'Pay directly from your main account with automatic insufficient-balance checks.',
  },
  {
    icon: '✓',
    title: 'Payment Tracking',
    text: 'Review customer and subscription payment histories with success/failure states.',
  },
]

export default function LandingPage() {
  return (
    <div className="landing">
      <nav className="landing-nav">
        <span className="landing-nav-logo">PayderPay</span>
        <div className="landing-nav-actions">
          <Link to="/auth/login" className="btn btn-secondary btn-sm">Sign in</Link>
          <Link to="/auth/register" className="btn btn-primary btn-sm">Get started</Link>
        </div>
      </nav>

      <section className="landing-hero">
        <h1 className="landing-headline">Subscription payments,<br />without blind spots.</h1>
        <p className="landing-sub">
          A clean banking interface to manage subscribers, query debts, pay from main account,
          and monitor every payment period with precise status visibility.
        </p>
        <div className="landing-actions">
          <Link to="/auth/register" className="btn btn-primary">Create account</Link>
          <Link to="/auth/login" className="btn btn-secondary">Sign in</Link>
        </div>
      </section>

      <section className="landing-features">
        {features.map(item => (
          <div key={item.title} className="landing-feature">
            <div className="landing-feature-icon">{item.icon}</div>
            <div className="landing-feature-title">{item.title}</div>
            <div className="landing-feature-text">{item.text}</div>
          </div>
        ))}
      </section>
    </div>
  )
}
