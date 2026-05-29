# /format — Pretty-format messy code
Analyze and reformat all messy or inconsistently formatted code in the project.

## Instructions

1. **Scan** the project for files with formatting issues:
   - Mixed indentation (tabs vs spaces)
   - Inconsistent brace style
   - Lines exceeding 120 characters
   - Missing blank lines between logical sections
   - Poorly aligned parameters or assignments

2. **Auto-detect** the project's primary language and apply appropriate formatting:
   - C#: Apply C# conventions (PascalCase, braces on new line for types, same line for methods)
   - JavaScript/TypeScript: Apply standard conventions (2-space indent, semicolons, single quotes)
   - Python: Apply PEP 8 (4-space indent, snake_case)
   - Go: Apply gofmt conventions (tabs, no unused imports)
   - Rust: Apply rustfmt conventions

3. **Do NOT change logic** — only fix formatting, spacing, and alignment.

4. **Group changes** by file and apply them in batches of 5-10 files max.

5. **After formatting**, run the project's build command to verify nothing broke.

6. **Report** what was changed:
   - Files reformatted (count)
   - Common issues found
   - Any files skipped and why
