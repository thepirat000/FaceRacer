# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction

## Debugging Instructions
- When diagnosing MAUI/Android crashes that appear without code changes, clean or delete the `bin/obj` directories as a first-line step. Stale generated Android resources/themes can cause runtime crashes.
- For .NET MAUI XAML code-behind 'Cannot resolve symbol' issues in ReSharper (e.g., InitializeComponent, named controls), invalidate or clear ReSharper caches if the solution still builds/runs.