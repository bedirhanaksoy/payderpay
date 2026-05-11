import axios from 'axios'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').trim()

export const apiClient = axios.create({
  baseURL: baseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
})
