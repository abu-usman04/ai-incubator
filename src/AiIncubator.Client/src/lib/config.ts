export const API_URL = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

export const CLERK_PUBLISHABLE_KEY = import.meta.env.VITE_CLERK_PUBLISHABLE_KEY;

export const clerkEnabled = Boolean(CLERK_PUBLISHABLE_KEY);

/** When no API URL is configured the client serves local fixtures so the UI is demoable offline. */
export const mockMode = API_URL.length === 0;
