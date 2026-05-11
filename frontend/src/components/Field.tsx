import type { InputHTMLAttributes, SelectHTMLAttributes } from 'react'

interface FieldProps {
  label: string
  error?: string
  hint?: string
  children: React.ReactNode
}

export function Field({ label, error, hint, children }: FieldProps) {
  return (
    <div className="field">
      <label className="field-label">{label}</label>
      {children}
      {error && <span className="field-error">{error}</span>}
      {!error && hint && <span className="field-hint">{hint}</span>}
    </div>
  )
}

type InputProps = InputHTMLAttributes<HTMLInputElement> & { error?: boolean }

export function Input({ error, className = '', ...props }: InputProps) {
  return <input className={`field-input${error ? ' error' : ''} ${className}`} {...props} />
}

type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & { error?: boolean }

export function Select({ error, className = '', children, ...props }: SelectProps) {
  return (
    <select className={`field-select${error ? ' error' : ''} ${className}`} {...props}>
      {children}
    </select>
  )
}
