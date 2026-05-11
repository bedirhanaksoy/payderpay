import { z } from 'zod'

export const loginSchema = z.object({
  email: z.string().email('Enter a valid email.'),
  password: z.string().min(6, 'Password must be at least 6 characters.'),
})

export const registerSchema = z.object({
  fullName: z.string().min(2, 'Full name is required.').max(200),
  email: z.string().email('Enter a valid email.'),
  phoneNumber: z.string().min(8, 'Phone number is required.').max(30),
  password: z.string().min(6, 'Password must be at least 6 characters.'),
  initialMainAccountBalance: z.number().min(0, 'Initial balance cannot be negative.').max(9999999999),
})

export type LoginFormValues = z.infer<typeof loginSchema>
export type RegisterFormValues = z.infer<typeof registerSchema>
