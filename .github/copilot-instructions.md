# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- When a user reports a fix/correction, verify the reported code issue directly in the workspace (e.g., property getter/setter bodies) before proposing larger refactors.

## Debugging Instructions
- When diagnosing MAUI/Android crashes that appear without code changes, clean or delete the `bin/obj` directories as a first-line step. Stale generated Android resources/themes can cause runtime crashes.
- For .NET MAUI XAML code-behind 'Cannot resolve symbol' issues in ReSharper (e.g., InitializeComponent, named controls), invalidate or clear ReSharper caches if the solution still builds/runs.

## C#/.NET Style Rules
- Always use braces `{}` for all `if`, `else`, `for`, `while`, and similar statements, even if the body is a single line.
- Prefer explicit access modifiers (`public`, `private`, etc.) for all classes and members.
- Use PascalCase for class, method, and property names; use camelCase for local variables and parameters.
