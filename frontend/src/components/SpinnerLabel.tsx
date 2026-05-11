import type { ReactNode } from 'react'

interface SpinnerLabelProps {
  loading: boolean
  children: ReactNode
}

/**
 * Wraps a button label so the button keeps its natural width while loading.
 * The text stays in the layout (visibility:hidden) and the spinner overlays
 * it absolutely-centered.
 */
export default function SpinnerLabel({ loading, children }: SpinnerLabelProps) {
  return (
    <span style={{ position: 'relative', display: 'inline-block' }}>
      <span style={{ visibility: loading ? 'hidden' : 'visible' }}>{children}</span>
      {loading && (
        <span
          className="spin"
          aria-hidden
          style={{
            position: 'absolute',
            inset: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          ◌
        </span>
      )}
    </span>
  )
}
