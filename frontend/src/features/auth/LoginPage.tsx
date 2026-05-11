import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { Field, Input } from '../../components/Field'
import SpinnerLabel from '../../components/SpinnerLabel'
import { authProvider } from '../../shared/auth/provider'
import { session } from '../../shared/auth/session'
import { loginSchema, type LoginFormValues } from './schemas'
import { errorMessage } from '../../shared/errors/problem-details'

export default function LoginPage() {
  const navigate = useNavigate()
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  })

  async function onSubmit(values: LoginFormValues) {
    setSubmitError(null)
    setIsSubmitting(true)

    try {
      const user = await authProvider.login(values)
      session.save(user)
      navigate('/dashboard', { replace: true })
    } catch (error) {
      setSubmitError(errorMessage(error, 'auth_login'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="auth-layout">
      <div className="auth-card">
        <div className="auth-logo">PayderPay</div>
        <div className="auth-tagline">Sign in to your account</div>

        {submitError && <div className="alert alert-error mb-4">{submitError}</div>}

        <form className="form" onSubmit={handleSubmit(onSubmit)}>
          <Field label="Email" error={errors.email?.message}>
            <Input
              type="email"
              autoComplete="email"
              placeholder="you@example.com"
              error={!!errors.email}
              {...register('email')}
            />
          </Field>

          <Field label="Password" error={errors.password?.message}>
            <Input
              type="password"
              autoComplete="current-password"
              placeholder="••••••••"
              error={!!errors.password}
              {...register('password')}
            />
          </Field>

          <button
            type="submit"
            className="btn btn-primary w-full"
            style={{ justifyContent: 'center', marginTop: '0.5rem' }}
            disabled={isSubmitting}
          >
            <SpinnerLabel loading={isSubmitting}>Sign in</SpinnerLabel>
          </button>
        </form>

        <div className="auth-footer">
          Don&apos;t have an account? <Link to="/auth/register">Create one</Link>
        </div>
      </div>
    </div>
  )
}
