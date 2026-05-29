# Rule: Code Formatting
# Auto-loaded when Claude reads/writes code files

## General
- Always format code to be human-readable
- When you encounter messy or inconsistently formatted code, fix it as you go
- Follow the project's existing style. If none exists, use language conventions
- Never change logic when reformatting — only whitespace, alignment, grouping

## Language-Specific Defaults

### C#
- PascalCase for public members, _camelCase for private fields
- Braces on new line for types, same line for methods
- One blank line between methods, two between types
- 120 character line limit

### TypeScript/JavaScript
- 2-space indentation
- Semicolons required
- Single quotes for strings
- Trailing commas
- 100 character line limit

### Python
- 4-space indentation (never tabs)
- snake_case for functions and variables
- PascalCase for classes
- 88 character line limit (PEP 8)
- Blank line between functions, two between classes

### Go
- Tabs for indentation
- gofmt conventions
- No unused imports or variables
- PascalCase for exported, camelCase for unexported

## When to Format
- After writing new code
- After editing existing code (leave it better than you found it)
- Use /format command to batch-format the whole project
