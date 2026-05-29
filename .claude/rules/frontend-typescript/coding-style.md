---
paths:
  - "**/*.ts"
  - "**/*.tsx"
  - "**/*.js"
  - "**/*.jsx"
---
# TypeScript / JavaScript Coding Style

Broad language-level rules for every `.ts` / `.tsx` / `.js` / `.jsx` file in
the repo.

> **See also:** Files under `src/web/` additionally follow
> [`frontend-conventions.md`](./frontend-conventions.md) (state management,
> component layout, styling, Next.js routing).

Examples below reuse this canonical type:

```typescript
interface User {
  id: string
  firstName: string
  lastName: string
  email: string
}
```

## Types

### Public APIs

- Annotate parameters and return types on **exported** functions, shared
  utilities, and public class methods.
- Let inference handle **local** variables — redundant annotations add noise
  without adding safety.
- Extract repeated inline object shapes into a named `type` or `interface`.

```typescript
// WRONG: exported function without explicit types
export function formatUser(user) {
  return `${user.firstName} ${user.lastName}`
}

// CORRECT: explicit on the public boundary
export function formatUser(user: User): string {
  return `${user.firstName} ${user.lastName}`
}
```

### `interface` vs. `type`

- `interface` for object shapes that may be extended or implemented.
- `type` for unions, intersections, tuples, mapped types, and utility types.
- Prefer **string-literal unions over `enum`** unless `enum` is required for
  interop (e.g. a protobuf-generated client expects numeric enums).

```typescript
type UserRole = 'admin' | 'member'
type UserWithRole = User & { role: UserRole }
```

### Avoid `any`

- No `any` in application code.
- Use `unknown` for untrusted / external input and narrow it before use.
- Use **generics** when a return type depends on the caller.

```typescript
// WRONG: any silently erases safety
function getErrorMessage(error: any) {
  return error.message
}

// CORRECT: unknown forces a narrowing step
function getErrorMessage(error: unknown): string {
  if (error instanceof Error) return error.message
  return 'Unexpected error'
}
```

### React props

- Declare props with a **named** `interface` or `type` above the component.
- Type callback props explicitly (`(id: string) => void`).
- Do not use `React.FC` — it adds an implicit `children` and interferes with
  generic components. Type the function directly.

```typescript
interface UserCardProps {
  user: User
  onSelect: (id: string) => void
}

function UserCard({ user, onSelect }: UserCardProps) {
  return <button onClick={() => onSelect(user.id)}>{user.email}</button>
}
```

### JavaScript files

In `.js` / `.jsx` files where a TypeScript migration isn't practical, use
JSDoc for types on exported functions. Keep JSDoc aligned with runtime
behaviour — stale JSDoc is worse than no JSDoc.

```javascript
/**
 * @param {{ firstName: string, lastName: string }} user
 * @returns {string}
 */
export function formatUser(user) {
  return `${user.firstName} ${user.lastName}`
}
```

## Immutability

Treat inputs as read-only. Produce new objects instead of mutating in place —
this plays well with React state, Zustand, and structural sharing.

```typescript
// WRONG: mutates the caller's object
function updateUser(user: User, name: string): User {
  user.firstName = name
  return user
}

// CORRECT: returns a new object
function updateUser(user: Readonly<User>, firstName: string): User {
  return { ...user, firstName }
}
```

## Error handling

Use `async` / `await` with `try` / `catch`, and narrow `unknown` errors before
surfacing them.

```typescript
async function loadUser(userId: string): Promise<User> {
  try {
    return await api.getUser(userId)
  } catch (error: unknown) {
    logger.error('loadUser failed', { userId, error })
    throw new Error(getErrorMessage(error))
  }
}
```

## Input validation

Use **Zod** for schema validation at trust boundaries (API responses, form
input, env vars). Infer the TS type from the schema so the two can't drift.

```typescript
import { z } from 'zod'

const userSchema = z.object({
  email: z.string().email(),
  age: z.number().int().min(0).max(150),
})

type UserInput = z.infer<typeof userSchema>

const validated: UserInput = userSchema.parse(input)
```

## Logging

- **No `console.log` in committed code.** Use the project's configured logger.
- `console.warn` / `console.error` are allowed only as a last-resort fallback
  before the logger is initialised (e.g. in bootstrap code).
- This rule is enforced by a pre-commit hook; check the repo's hook config for
  the exact location.
