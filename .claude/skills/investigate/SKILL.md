---
name: investigate
description: |
  Structured debugging and root-cause analysis. Reproduces the issue, isolates the
  layer, traces the data flow, identifies the root cause, and proposes a fix.
  Use when: something is broken, a bug report comes in, behavior is unexpected,
  "why does this happen", "this doesn't work", or error messages appear.
allowed-tools:
  - Bash
  - Read
  - Grep
  - Glob
  - Edit
  - Write
  - Agent
---

# Investigate

Systematic root-cause analysis. You are a senior debugger. No guessing, no shotgun fixes. Trace the problem to its source with evidence at every step.

**Principle:** Understand before you fix. A fix without understanding is a new bug waiting to happen.

---

## Phase 1: Triage

Classify the report before touching anything.

1. **Read the symptom.** What exactly is broken? Error message, wrong behavior, crash, performance?
2. **Read CLAUDE.md files** for stack context, architecture, and error handling patterns.
3. **Classify the layer:**

| Signal | Likely Layer | Start Here |
|--------|-------------|------------|
| HTTP status code, API error | Backend (controller/service) | `src/api/Controllers/`, `src/api/Services/` |
| TypeScript error, component crash | Frontend | `src/web/src/` |
| SQL error, missing data | Database/Repository | `src/api/Persistence/`, `packages/db/` |
| Build failure | Configuration | `tsconfig.json`, `.csproj`, `package.json` |
| Auth error (401/403) | Auth middleware | JWT config, `[Authorize]` attributes |
| CORS error | Configuration | `appsettings.json`, CORS middleware |

4. **Check recent changes:**
```bash
git log --oneline -10
git diff --name-only HEAD~5
```

If the bug correlates with a recent change, start there.

Output: "Triage: [layer] issue. Symptom: [what's broken]. Most likely location: [files]. Starting investigation at [specific point]."

---

## Phase 2: Reproduce

Before investigating, confirm the problem exists and is reproducible.

### Backend issues
```bash
cd "$CLAUDE_PROJECT_DIR/src/api" && dotnet build --nologo 2>&1 | tail -20
```

If it's a runtime error, check logs or attempt the failing request path in code.

### Frontend issues
```bash
cd "$CLAUDE_PROJECT_DIR/src/web" && bunx tsc --noEmit --incremental false 2>&1 | tail -30
```

### Database issues
Check the schema matches expectations:
```bash
cd "$CLAUDE_PROJECT_DIR" && bun run db:generate 2>&1
```

**If you cannot reproduce:** say so. Ask the user for exact reproduction steps. Do not guess at fixes for unreproducible issues.

Output: "Reproduced: [yes/no]. Evidence: [error output or behavior observed]."

---

## Phase 3: Isolate

Narrow from "something is broken" to "this specific thing is broken."

### Strategy: Binary search the call stack

1. **Identify the entry point** — the controller endpoint, page route, or component that triggers the bug.
2. **Trace inward** — follow the call chain: controller → service → repository → database (backend) or page → component → hook → API call (frontend).
3. **At each layer, verify:**
   - Does the input arrive correctly? (Read the function signature, check types)
   - Does the output leave correctly? (Check return types, error handling)
   - If input is right but output is wrong, the bug is in this layer.

### Common patterns to check

**Backend:**
- Missing DI registration (service not injected)
- Wrong SQL query (check `Persistence/` files for the query)
- Missing `[Authorize]` or wrong role check
- `AppException` not thrown for expected error cases
- Null reference from unhandled optional data

**Frontend:**
- API URL mismatch (check `NEXT_PUBLIC_API_URL`)
- Missing `"use client"` directive on interactive components
- TanStack Query key mismatch (stale cache)
- Zustand store not updating (check selectors)
- Type mismatch between API response and TypeScript interface

**Database:**
- Schema drift (Prisma schema vs actual DB)
- Missing migration (new column not pushed)
- Constraint violation (unique, foreign key)

Output: "Isolated to: [file:line]. The bug is [specific description]. Evidence: [what I found]."

---

## Phase 4: Root Cause

State the root cause clearly:

```
ROOT CAUSE: [one sentence]
LOCATION: [file:line]
MECHANISM: [how the bug manifests — what happens step by step]
EVIDENCE: [what proves this is the cause, not a symptom]
```

**Distinguish cause from symptom.** A null reference exception is a symptom. The missing null check or the upstream function returning null unexpectedly is the cause. Keep tracing until you reach the actual cause.

---

## Phase 5: Fix

Propose the fix with reasoning:

```
FIX: [what to change]
WHY: [why this fixes the root cause, not just the symptom]
RISK: [what could this fix break? what else uses this code path?]
```

Then implement:

1. **Make the minimal fix.** Don't refactor surrounding code. Don't add features. Fix the bug.
2. **Verify the fix:**
   - Backend: `cd "$CLAUDE_PROJECT_DIR/src/api" && dotnet build --nologo`
   - Frontend: `cd "$CLAUDE_PROJECT_DIR/src/web" && bunx tsc --noEmit --incremental false`
3. **Check for related instances.** Use Grep to find similar patterns that might have the same bug.

---

## Phase 6: Report

Summarize for the user:

```markdown
## Investigation: [Title]

**Symptom:** [what was reported]
**Root cause:** [one sentence]
**Location:** [file:line]
**Fix:** [what was changed]
**Verification:** [build/typecheck passes]
**Related:** [any similar patterns found that might need the same fix]
```

If the fix touches critical paths (auth, data handling, payments), note: "This change touches [area]. Consider running `/test` and dispatching `security-reviewer` before merging."

---

## Rules

- **Never guess.** Every claim needs evidence (file contents, error output, git history).
- **Never shotgun fix.** Don't change 5 things hoping one works. Change one thing, verify.
- **Trace, don't grep.** Start from the symptom and follow the call chain. Grepping for keywords misses context.
- **Minimal fix.** The fix should be as small as possible. No drive-by refactors.
- **If stuck after 3 attempts at the same approach, escalate.** Tell the user what you tried and what you think the issue might be. Don't loop.
