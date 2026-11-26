import { api } from './client'
import { upload } from './studio'
import type { NameVisibility, PublicUser, TeachingLink, User } from './types'

export const profileApi = {
  update: (displayName: string, nameVisibility: NameVisibility) =>
    api<User>('/profile', { method: 'PATCH', body: { displayName, nameVisibility } }),
  uploadAvatar: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return upload<User>('/profile/avatar', form)
  },
  removeAvatar: () => api<User>('/profile/avatar', { method: 'DELETE' }),
  frameAvatar: (framing: { focusX: number; focusY: number; zoom: number }) =>
    api<User>('/profile/avatar/framing', { method: 'PUT', body: framing }),
  changePassword: (currentPassword: string, newPassword: string) =>
    api<void>('/profile/password', { method: 'POST', body: { currentPassword, newPassword } }),
  requestEmailChange: (newEmail: string, password: string) =>
    api<{ message: string }>('/profile/email', { method: 'POST', body: { newEmail, password } }),
  confirmEmailChange: (code: string) => api<User>('/profile/email/confirm', { method: 'POST', body: { code } }),
  deleteAccount: (password: string) => api<void>('/profile', { method: 'DELETE', body: { password } }),
  teachers: () => api<TeachingLink[]>('/teaching/teachers'),
  students: () => api<TeachingLink[]>('/teaching/students'),
  invite: (emailOrUsername: string) => api<TeachingLink>('/teaching/invitations', { method: 'POST', body: { emailOrUsername } }),
  acceptLink: (id: string) => api<TeachingLink>(`/teaching/links/${id}/accept`, { method: 'POST' }),
  removeLink: (id: string) => api<void>(`/teaching/links/${id}`, { method: 'DELETE' }),
  follows: () => api<PublicUser[]>('/follows'),
  unfollow: (username: string) => api<void>(`/authors/${encodeURIComponent(username)}/follow`, { method: 'DELETE' }),
}
