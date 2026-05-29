# Rule: Coding Style
# Auto-loaded when Claude writes or modifies any source code

## General
- One class/interface per file
- Files: 200-400 lines typical, 800 absolute max
- Methods: under 30 lines. If longer, extract.
- Parameters: max 4. If more, use an options object.
- No console.log, print, or debug statements in production code
- No commented-out code — delete it (git remembers)

## Naming
- Names should describe behavior, not implementation
- Avoid abbreviations: `UserRepository` not `UserRepo`, `IsAuthenticated` not `IsAuth`
- Boolean properties: `Is/Has/Can` prefix (`IsActive`, `HasPermission`)
- Event handlers: `On/Handle` prefix (`OnUserCreated`, `HandlePaymentFailed`)

## Structure
- Dependencies point inward (core has no dependencies)
- Public API is thin, logic lives in services
- Configuration is separate from logic
- Error types are defined close to where they're thrown
