---
paths:
  - "src/web/**/*.ts"
  - "src/web/**/*.tsx"
  - "src/web/**/*.js"
  - "src/web/**/*.jsx"
---
# Frontend Conventions

Rules specific to files under `src/web/` (the Next.js web app).

> **Extends** [`coding-style.md`](./coding-style.md). That file owns all
> language-level TS/JS rules (types, `any`, error handling, logging,
> immutability, Zod). This file only defines web-app concerns: state, components,
> styling, icons, routing, and shared utilities.

## State Management

- **Zustand** for client / UI state — stores in `src/stores/`
  (`auth.ts`, `canvas.ts`, `workspace.ts`).
- **TanStack Query v5** for server / API state — via `useMutation` /
  `useQuery` hooks in `src/hooks/`.
- **Never mix the two layers.** UI state → Zustand. Remote data → TanStack
  Query. Don't cache API responses in Zustand.
- **Subscribe with selectors, not destructuring.** Destructuring the whole
  store re-renders the component on every unrelated field change. Select only
  what the component reads:

  ```typescript
  // WRONG: component re-renders when any canvas field changes
  const { addScreen, removeScreen } = useCanvasStore()

  // CORRECT: scoped subscription
  const addScreen = useCanvasStore((s) => s.addScreen)
  const removeScreen = useCanvasStore((s) => s.removeScreen)
  ```

- **Action naming:** `set<Field>()` for plain setters, verb-based for domain
  actions — `addScreen`, `removeScreen`, `toggleChatPanel`.
- **Persist middleware** only on the auth store. Use `partialize` to
  whitelist exactly what gets written to storage — never persist the full
  store.

## Components

- **Named exports only:** `export function MyComponent(...)`. No
  `export default`.
- **Props interface** declared directly above the component, suffix `Props`:
  `interface MyComponentProps { ... }`.
- **File naming:** PascalCase matching the component — `ExportModal.tsx`,
  `ScreenCard.tsx`, `ChatPanel.tsx`.
- **Directory layout:** `src/components/<domain>/` — group by feature
  (`generation/`, `workspace/`, `ui/`), not by type.
- **`"use client"`** at the top of any file that uses hooks, event handlers,
  or browser APIs. Omit it from pure server components so they stay on the
  server boundary.

## Styling & Animation

- **Tailwind CSS** only. Always compose conditional classes through `cn()`
  from `@/lib/utils` — don't call `clsx` or `twMerge` directly in components.
- **Framer Motion** for animations. Use `motion.*` elements and wrap
  enter/exit transitions in `<AnimatePresence>`.
- **Animation presets** live in `@/lib/constants` — `SPRING`, `FADE_IN_UP`,
  `FADE_IN_SCALE`, `STAGGER_CHILDREN`. Prefer these over inline transition
  configs so timing stays consistent across the app.

## Icons

- Use **`lucide-react`** for every UI icon. Do not hand-roll `<svg>` for
  icons in React components.
- Import by name: `import { Plus, Check, Sparkles } from 'lucide-react'`.
- Size with the `size` prop (`size={16}`) or Tailwind (`className="w-4 h-4"`).
  Never hard-code `<svg width/height>`.
- Color via Tailwind `text-*` classes (icons inherit `currentColor`). Do not
  set `fill` / `stroke` attributes.
- **Inline `<svg>` is only permitted for:** (a) data visualization (snap
  guides, grid patterns, charts); (b) programmatically built SVG *data*
  (e.g. sketch export); (c) generated HTML fixtures. Everything else uses
  `lucide-react`.
- If lucide doesn't have a close match, substitute the closest lucide icon
  plus a text label. Do not pull in a second icon library or paste raw SVG
  paths.

## Routing & API

- **App Router only** — `page.tsx` / `layout.tsx` / `loading.tsx` under
  `src/app/`. No `pages/` directory.
- API calls go through the **Next.js proxy** (`/api/*` rewrites to the
  backend). Never hard-code `localhost:3001` or any absolute backend URL.
- Shared types come from **`@threadline/types`** — never redeclare backend
  types locally.

## Utilities

Available from `@/lib/utils`:

- `cn()` — conditional Tailwind class merging.
- `formatCurrency()`, `formatRelativeTime()`, `slugify()`, `truncate()` —
  formatting helpers.
- `scoreColor()`, `scoreLabel()` — score-to-visual mapping.

Available from `@/lib/api`:

- `api` — singleton HTTP client with automatic token refresh. All API calls
  go through this; don't call `fetch` directly from components.
