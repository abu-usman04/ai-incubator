# Rule: Git Workflow
# Auto-loaded when Claude performs git operations

## Commit Messages
Format: `<type>(<scope>): <description>`

Types: feat, fix, refactor, test, docs, chore, perf, security

Examples:
- `feat(auth): add JWT refresh token endpoint`
- `fix(orders): handle null reference in order total calculation`
- `test(payments): add integration tests for Stripe webhook handler`

## Rules
- Commit only what belongs together (one logical change per commit)
- Never commit secrets, credentials, or build artifacts
- Never force push to shared branches
- Write meaningful messages — no "fix stuff" or "WIP"
- Stage only relevant files, never `git add .`

## Branch Strategy
- `main` is always deployable
- Feature branches from `main`, merge back via PR
- Hotfix branches from `main`, merge back immediately
- Delete branches after merge
