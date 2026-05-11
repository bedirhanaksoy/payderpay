import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { Field, Input } from '../../components/Field'
import SpinnerLabel from '../../components/SpinnerLabel'
import { authProvider } from '../../shared/auth/provider'
import { session } from '../../shared/auth/session'
import { errorMessage } from '../../shared/errors/problem-details'
import { registerSchema, type RegisterFormValues } from './schemas'

export default function RegisterPage() {
  const navigate = useNavigate()
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      initialMainAccountBalance: 0,
    },
  })

  async function onSubmit(values: RegisterFormValues) {
    setSubmitError(null)
    setIsSubmitting(true)

    try {
      const user = await authProvider.register(values)
      session.save(user)
      navigate('/dashboard', { replace: true })
    } catch (error) {
      setSubmitError(errorMessage(error, 'auth_register'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="auth-layout">
      <div className="auth-card">
        <div className="auth-logo">PayderPay</div>
        <div className="auth-tagline">Create your account</div>

        {submitError && <div className="alert alert-error mb-4">{submitError}</div>}

        <form className="form" onSubmit={handleSubmit(onSubmit)}>
          <Field label="Full name" error={errors.fullName?.message}>
            <Input placeholder="Jane Doe" error={!!errors.fullName} {...register('fullName')} />
          </Field>

          <Field label="Email" error={errors.email?.message}>
            <Input type="email" placeholder="you@example.com" error={!!errors.email} {...register('email')} />
          </Field>

          <Field label="Phone number" error={errors.phoneNumber?.message}>
            <Input placeholder="+90 5xx xxx xx xx" error={!!errors.phoneNumber} {...register('phoneNumber')} />
          </Field>

          <Field label="Password" error={errors.password?.message}>
            <Input type="password" placeholder="••••••••" error={!!errors.password} {...register('password')} />
          </Field>

          <Field label="Initial main account balance" error={errors.initialMainAccountBalance?.message}>
            <Input type="number" step="0.01" min="0" error={!!errors.initialMainAccountBalance} {...register('initialMainAccountBalance', { valueAsNumber: true })} />
          </Field>

          <button
            type="submit"
            className="btn btn-primary w-full"
            style={{ justifyContent: 'center', marginTop: '0.5rem' }}
            disabled={isSubmitting}
          >
            <SpinnerLabel loading={isSubmitting}>Create account</SpinnerLabel>
          </button>
        </form>

        <div className="auth-footer">
          Already registered? <Link to="/auth/login">Sign in</Link>
        </div>
      </div>
    </div>
  )
}
