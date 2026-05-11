import { useEffect, useRef } from 'react'
import SpinnerLabel from './SpinnerLabel'

interface ModalProps {
  title: string
  onClose: () => void
  children: React.ReactNode
  footer?: React.ReactNode
}

export function Modal({ title, onClose, children, footer }: ModalProps) {
  const overlayRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  return (
    <div
      className="modal-overlay"
      ref={overlayRef}
      onClick={event => {
        if (event.target === overlayRef.current) {
          onClose()
        }
      }}
    >
      <div className="modal" role="dialog" aria-modal aria-labelledby="modal-title">
        <div className="modal-header">
          <span className="modal-title" id="modal-title">{title}</span>
          <button type="button" className="modal-close" onClick={onClose} aria-label="Close">✕</button>
        </div>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-footer">{footer}</div>}
      </div>
    </div>
  )
}

interface ConfirmModalProps {
  title: string
  message: string
  confirmLabel?: string
  onConfirm: () => void
  onClose: () => void
  danger?: boolean
  loading?: boolean
}

export function ConfirmModal({
  title,
  message,
  confirmLabel = 'Confirm',
  onConfirm,
  onClose,
  danger,
  loading,
}: ConfirmModalProps) {
  return (
    <Modal
      title={title}
      onClose={onClose}
      footer={(
        <>
          <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
            Cancel
          </button>
          <button
            type="button"
            className={`btn ${danger ? 'btn-danger' : 'btn-primary'}`}
            onClick={onConfirm}
            disabled={loading}
          >
            <SpinnerLabel loading={loading ?? false}>{confirmLabel}</SpinnerLabel>
          </button>
        </>
      )}
    >
      <p style={{ color: 'var(--text-muted)', fontSize: '0.9375rem' }}>{message}</p>
    </Modal>
  )
}
