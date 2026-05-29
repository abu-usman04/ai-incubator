import { Navigate } from 'react-router-dom';
import { SignUp } from '@clerk/clerk-react';
import { clerkEnabled } from '../lib/config';

export function SignUpPage() {
  if (!clerkEnabled) {
    return <Navigate to="/app" replace />;
  }

  return (
    <div className="flex min-h-full items-center justify-center p-6">
      <SignUp routing="path" path="/sign-up" signInUrl="/sign-in" afterSignUpUrl="/app" />
    </div>
  );
}
