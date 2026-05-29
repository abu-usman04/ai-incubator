import { Routes, Route } from 'react-router-dom';
import { AppShell } from './components/app/AppShell';
import { RequireAuth } from './auth/RequireAuth';
import { Landing } from './pages/Landing';
import { Dashboard } from './pages/Dashboard';
import { Documents } from './pages/Documents';
import { Chat } from './pages/Chat';
import { SignInPage } from './pages/SignInPage';
import { SignUpPage } from './pages/SignUpPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/sign-in/*" element={<SignInPage />} />
      <Route path="/sign-up/*" element={<SignUpPage />} />
      <Route
        path="/app"
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route index element={<Dashboard />} />
        <Route path="documents" element={<Documents />} />
        <Route path="chat" element={<Chat />} />
      </Route>
    </Routes>
  );
}
