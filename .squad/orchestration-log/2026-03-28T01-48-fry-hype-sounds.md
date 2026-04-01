# Orchestration Log: Fry — Hype Sound & DropAnimation Speed Fix
**Timestamp:** 2026-03-28T01:48:00Z  
**Agent:** Fry (Senior Software Engineer)  
**Mode:** background  
**Model:** claude-opus-4.6  
**Status:** SUCCESS  

## Spawn Directive
Fix hype sounds not playing + bump DropAnimation speed

## Outcome

### Changes Completed
1. **Hype sounds gate:** Removed `_lastSoundTimeMs` 2-second time-based cooldown from `GameBoard._Process()`. The round-based `_lastSoundRound` guard is sufficient for preventing duplicate sounds within and across cascading rounds (which resolve in milliseconds, not seconds).

2. **Hype level formula:** Fixed `ProcessingRound % MaxHypeLevel` (was `Math.Min(ProcessingRound + 1, MaxHypeLevel - 1)`). Ensures:
   - First match plays `Sound_MatchHypeLevel0` (not 1)
   - Wraps back to 0 after exhausting all 5 levels (0→1→2→3→4→0...)
   - No off-by-one skipping or capping

3. **DropAnimation speed:** Bumped `speed_scale` from 2.0 → 2.5 in `GameObjectResources/Grid/Tile.tscn` (DropAnimation AnimatedSprite2D node). Now that the animation fires only on matched positions (not every tile move), the slightly faster speed provides visual snappiness.

### Files Modified
- `Scenes/GameBoard.cs`  
  - Removed `_lastSoundTimeMs` field and all references  
  - Changed hype formula to `ProcessingRound % MaxHypeLevel`
  - Drop sounds now fire once per round without time gating  

- `GameObjectResources/Grid/Tile.tscn`  
  - DropAnimation speed_scale: 2.0 → 2.5

### Build Status
✅ 0 errors, 2 pre-existing warnings (unrelated)

## Decision Impact
- **Cascading hype chimes:** Properly escalate through levels 0–4 during fast cascades  
- **Drop sounds:** Fire once per round, layered with hype chimes  
- **DropAnimation responsiveness:** Snappier feel on matched tile landings  

## Lesson
Time-based cooldowns on frame-driven sound events can inadvertently silence the feature. Round-based guards are the correct mechanism when the event source (rounds) already provides natural serialization.

---

**Fry's Next Session:** Await assignment from Georgia or Scribe.
