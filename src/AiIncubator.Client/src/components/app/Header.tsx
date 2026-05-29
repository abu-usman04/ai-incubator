import { SignedIn, UserButton } from '@clerk/clerk-react';
import { clerkEnabled, mockMode } from '../../lib/config';
import { Badge } from '../ui/Badge';

export function Header({ title }: { title: string }) {
  return (
    <header className="flex h-16 items-center justify-between border-b border-slate-200 bg-white px-6">
      <h1 className="text-lg font-semibold text-slate-800">{title}</h1>
      <div className="flex items-center gap-3">
        {mockMode && <Badge tone="warning">Mock mode</Badge>}
        {clerkEnabled ? (
          <SignedIn>
            <UserButton afterSignOutUrl="/" />
          </SignedIn>
        ) : (
          <Badge tone="info">Dev user</Badge>
        )}
      </div>
    </header>
  );
}
