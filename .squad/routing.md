# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Art direction, sprites, animation | Leela | Sprite feedback, VFX concepts, animation timing, visual polish |
| Planning, task breakdown, priorities | Hermes | Feature decomposition, milestone planning, progress tracking |
| Architecture, engine decisions | Farnsworth | Node hierarchy, signal design, scene structure, performance |
| Code review | Farnsworth | Review code changes, check Godot .NET patterns, approve technical approach |
| Gameplay systems, core logic | Fry | Match-3 mechanics, cascading, scoring, special tile logic |
| UI, VFX code, shaders, polish | Amy | UI scenes, shader integration, visual effects, animation code |
| General C# implementation | Fry + Amy | Split by system — Fry takes gameplay logic, Amy takes visual/UI |
| Bug fixes | Fry or Amy | Route based on whether bug is logic (Fry) or visual/UI (Amy) |
| Scope & priorities | Hermes | What to build next, trade-offs, sequencing |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
