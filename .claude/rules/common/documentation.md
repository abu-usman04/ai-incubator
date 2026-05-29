# Documentation Guide

Auto-loaded when creating or editing documentation.

---

## Principles
- Concise, scannable, no fluff
- Each section <100 words (except code examples)
- Prefer tables and code blocks over prose
- Use present tense ("Creates", not "Created")
- No emojis

---

## README Structure

- Overview  
  2–3 sentences: what it does, who it's for

- Quick Start  
  Exact commands to run the project

- Architecture  
  High-level structure (bullets or diagram)

- API / Usage  
  Key interfaces, endpoints, commands

- Development  
  Build, test, lint commands

- Configuration  
  Env variables, config files

- Contributing (if needed)  
  PR rules, branch strategy

---

## When to Update Docs

- New feature or endpoint
- Architecture change
- New config or env variable
- New dependency requiring setup
- Build/test commands change

---

## Constraints

- Update ONLY affected sections
- Preserve existing structure and style
- Keep changes minimal and targeted

---

## Forbidden

- Do NOT regenerate entire README
- Do NOT add filler text ("Welcome to...", etc.)
- Do NOT leave placeholders (TODO, FIXME)