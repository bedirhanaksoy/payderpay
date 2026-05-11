import { session } from './session'

export function getCurrentUserOrThrow() {
  const user = session.getUser()
  if (!user) {
    throw new Error('Session not found.')
  }
  return user
}
