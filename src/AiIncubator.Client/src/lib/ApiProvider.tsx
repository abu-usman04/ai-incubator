import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { useAuth } from '@clerk/clerk-react';
import { clerkEnabled, mockMode } from './config';
import { createHttpClient } from './api';
import { createMockClient } from './mock';
import type { ApiClient } from './types';

const ApiContext = createContext<ApiClient | null>(null);

export function useApi(): ApiClient {
  const client = useContext(ApiContext);
  if (!client) {
    throw new Error('useApi must be used within an ApiProvider');
  }
  return client;
}

function buildClient(getToken: () => Promise<string | null>): ApiClient {
  return mockMode ? createMockClient() : createHttpClient(getToken);
}

function ClerkBoundProvider({ children }: { children: ReactNode }) {
  const { getToken } = useAuth();
  const client = useMemo(() => buildClient(() => getToken()), [getToken]);
  return <ApiContext.Provider value={client}>{children}</ApiContext.Provider>;
}

function StaticProvider({ children }: { children: ReactNode }) {
  const client = useMemo(() => buildClient(async () => null), []);
  return <ApiContext.Provider value={client}>{children}</ApiContext.Provider>;
}

export function ApiProvider({ children }: { children: ReactNode }) {
  return clerkEnabled ? (
    <ClerkBoundProvider>{children}</ClerkBoundProvider>
  ) : (
    <StaticProvider>{children}</StaticProvider>
  );
}
