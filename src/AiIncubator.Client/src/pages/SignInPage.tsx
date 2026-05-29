import { Navigate } from 'react-router-dom';
import { SignIn } from '@clerk/clerk-react';
import { clerkEnabled } from '../lib/config';

export function SignInPage() {
  if (!clerkEnabled) {
    return <Navigate to="/app" replace />;
  }

  return (
    <div className="flex min-h-full items-center justify-center p-6">
      <SignIn routing="path" path="/sign-in" signUpUrl="/sign-up" afterSignInUrl="/app" />
    </div>
  );
}
