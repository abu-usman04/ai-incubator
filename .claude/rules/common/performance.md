# Rule: Performance
# Auto-loaded when Claude writes or modifies data-access or heavy logic

## Watch For
- N+1 queries: loading related data in a loop
- Select N+1: fetching entire rows when only a few columns are needed
- Missing pagination on list endpoints
- Unbounded collections: no limit on query results
- Synchronous I/O on hot paths
- Large object allocations in tight loops
- Missing async/await on I/O operations

## Rules
- All I/O operations must be async
- All list endpoints must have pagination
- All database queries must use parameterized calls
- Use streaming for large payloads
- Cache expensive computations when appropriate
- Profile before optimizing — measure, don't guess
