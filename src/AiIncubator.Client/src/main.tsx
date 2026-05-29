import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { ClerkProvider } from '@clerk/clerk-react';
import App from './App';
import { ApiProvider } from './lib/ApiProvider';
import { CLERK_PUBLISHABLE_KEY, clerkEnabled } from './lib/config';
import './index.css';

const tree = (
  <BrowserRouter>
    <ApiProvider>
      <App />
    </ApiProvider>
  </BrowserRouter>
);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {clerkEnabled ? (
      <ClerkProvider publishableKey={CLERK_PUBLISHABLE_KEY!}>{tree}</ClerkProvider>
    ) : (
      tree
    )}
  </StrictMode>,
);
