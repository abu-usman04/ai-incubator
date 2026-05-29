import type { ReactNode } from 'react';
import { RedirectToSignIn, SignedIn, SignedOut } from '@clerk/clerk-react';
import { clerkEnabled } from '../lib/config';

/**
 * Guards protected routes. When Clerk is configured, unauthenticated users are redirected to
 * sign-in. When Clerk is disabled (local/mock mode), access is open so the UI can be explored.
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  if (!clerkEnabled) {
    return <>{children}</>;
  }

  return (
    <>
      <SignedIn>{children}</SignedIn>
      <SignedOut>
        <RedirectToSignIn />
      </SignedOut>
    </>
  );
}
