import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import './index.css'
import { AuthProvider } from './auth/AuthContext'
import { Layout } from './components/Layout'
import { RequireAuth } from './components/RequireAuth'
import { ForgotPasswordPage, LoginPage, RegisterPage } from './pages/AuthPages'
import { HomePage } from './pages/HomePage'
import { AdminPage } from './pages/AdminPage'
import { AuthorPage } from './pages/AuthorPage'
import { DictionaryPage } from './pages/DictionaryPage'
import { WelcomePage } from './pages/WelcomePage'
import { LibraryPage } from './pages/LibraryPage'
import { EditorPage } from './pages/EditorPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { PlayerPage } from './pages/PlayerPage'
import { ProfilePage } from './pages/ProfilePage'
import { StoryIntroPage } from './pages/StoryIntroPage'
import { StudioPage } from './pages/StudioPage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<HomePage />} />
            <Route path="library" element={<LibraryPage />} />
            <Route path="stories/:storyId" element={<StoryIntroPage />} />
            <Route path="authors/:username" element={<AuthorPage />} />
            <Route path="login" element={<LoginPage />} />
            <Route path="register" element={<RegisterPage />} />
            <Route path="forgot-password" element={<ForgotPasswordPage />} />
            <Route
              path="admin"
              element={
                <RequireAuth adminOnly>
                  <AdminPage />
                </RequireAuth>
              }
            />
            <Route
              path="words"
              element={
                <RequireAuth>
                  <DictionaryPage />
                </RequireAuth>
              }
            />
            <Route
              path="welcome"
              element={
                <RequireAuth>
                  <WelcomePage />
                </RequireAuth>
              }
            />
            <Route
              path="profile"
              element={
                <RequireAuth>
                  <ProfilePage />
                </RequireAuth>
              }
            />
            <Route
              path="studio"
              element={
                <RequireAuth authorOnly>
                  <StudioPage />
                </RequireAuth>
              }
            />
            <Route
              path="studio/stories/:storyId"
              element={
                <RequireAuth authorOnly>
                  <EditorPage />
                </RequireAuth>
              }
            />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
          <Route
            path="play/:storyId"
            element={
              <RequireAuth>
                <PlayerPage />
              </RequireAuth>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
)
