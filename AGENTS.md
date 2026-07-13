# AGENTS.md

This directory is the Unity / Git project inside an external Harness.

- Project repository: `E:\UnityProgram\FPS\FPSResearch`
- Harness root: `E:\UnityProgram\FPS`
- Shared long-lived facts: `E:\UnityProgram\FPS\Shared`
- Per-run evidence: `E:\UnityProgram\FPS\.harness\runs`

Do not treat this folder as a standalone agent workspace. Before making code,
asset, Git, or verification changes, go to the Harness root and follow:

1. `E:\UnityProgram\FPS\AGENTS.md`
2. `E:\UnityProgram\FPS\Shared\index.md`
3. The Harness work rules linked from `E:\UnityProgram\FPS\Shared\index.md`
4. `E:\UnityProgram\FPS\session-handoff.md`, if it exists

## Required Start Point

When an agent starts from this project folder, first run:

```powershell
Set-Location E:\UnityProgram\FPS
Get-Location
.\resolve-feature-list.ps1 -AsObject
.\reset-feature-list.ps1 -EscapeBackslashes -Confirm:$false
git -C E:\UnityProgram\FPS\FPSResearch status --short --branch
git -C E:\UnityProgram\FPS\FPSResearch log --oneline -5
./init.sh status
```

Run `./init.sh baseline` before coding unless the user explicitly requests a
read-only inspection.

## Boundary Rules

- Keep Harness files out of this repository except for this small pointer file.
- Do not copy task lists, run evidence, handoff notes, or Shared documents into
  `FPSResearch`.
- Use `git -C E:\UnityProgram\FPS\FPSResearch ...` for all Git commands, or
  explicitly confirm the current directory is the project repository first.
- Do not run Git commands that modify repository state from the Harness root.
- Do not overwrite, revert, delete, or tidy unrelated user changes.
- Only commit when the user explicitly asks for a commit.

## Project Coding Rules

For hand-written C# touched by the current task:

- Public types, methods, properties, fields, events, and constants use
  PascalCase.
- Private members and local delegates use camelCase without an underscore
  prefix.
- Interfaces use an `I` prefix and PascalCase.
- Enums and enum members use PascalCase.
- Local variables and parameters use camelCase.
- File names should match their primary type.
- Do not manually edit generated Input System or Luban code.
- Do not perform unrelated naming cleanup outside the current task.

## Completion Reminder

At the end of a task, return to the Harness rules for evidence, task status,
`session-handoff.md`, `git diff --check`, and final verification. The source of
truth for task state is the feature list resolved from the Harness root, not
this file.
