# Fry — Session History

Fry is a Senior Engineer specializing in game systems, animation pipelines, and physics.

## 2026-03-27: Drop Animation Polish Session
**Session ID:** 2026-03-27T2125-drop-animation-polish

Implemented all 5 animation polish todos:
- ✅ Batch dequeue optimization (HashSet<Tile> prevents duplicate Animated requests per frame)
- ✅ Stagger delay (configurable cascading drop offset)
- ✅ Event handler leak fix (`_dropAnimationHandlerConnected` flag)
- ✅ Distance-based duration (scales animation time by tile fall distance)
- ✅ Gap smoothing (seamless static-to-animated transition)

Files: `Scenes/GameBoard.cs`, `Core/GameObjects/Tile.cs`, `Core/Gameplay/TileAnimationRequest.cs`
Outcome: Build clean, all tests pass. Handoff to Amy for VFX tuning.

## Project Context

- **Owner:** Georgia Nelson
- **Project:** BattleTaterz — a Match-3 puzzle game (think Candy Crush / Puzzle Quest) with a potato/tater theme
- **Stack:** Godot Engine 4.6.1 (.NET), C#, 2D graphics, shaders
- **Board:** 9×9 grid, 6 gem types, cascading matches with recursive fill, special tile behaviors
- **Key Directories:** Core/ (game logic), Scenes/ (Godot scenes), GameObjectResources/, Assets/
- **Special Tiles:** Double Points (complete), Match Direction Elimination / Chomp Tater (needs polish)
- **Key Rule:** No commits — Georgia reviews and commits all changes herself
- **Created:** 2026-03-27

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **Animation pipeline:** `_moveRequests` (BlockingCollection, FIFO) → `_Process()` drains per-round → classifies ready vs deferred → pre-populates `_movingTiles` → processes. `HandleTileMoveAnimationFinished` uses both collections' counts to determine round completion, so `_movingTiles` must be populated BEFORE processing to avoid premature round advancement.
- **Input guardrail chain:** Two guards must hold: (1) `Tile_OnTileMouseEvent` checks `_movingTiles.Count() > 0` and `State == Playable`, (2) state transitions in `SwapSelectedTiles`/`EndTurn` toggle `SetProcessInput`. Never modify `GameBoardState` enum, state assignments, `SetProcessInput()`, or `EndTurn()`.
- **CompressColumn** is recursive — a tile cascading N rows generates N separate Animated requests. The batch dequeue uses `HashSet<Tile>` to allow only one Animated request per tile per frame; extras are deferred.
- **Godot 4 tween sequencing:** `TweenInterval` before `SetParallel(true)` creates a delay; parallel tweeners chain to the end of the preceding sequential step.
- **AnimatedSprite2D.AnimationFinished** handler leak: Use a boolean flag (`_dropAnimationHandlerConnected`) to connect handler once per tile lifetime, avoiding per-MoveTile lambda accumulation.
- **Key files:** `Scenes/GameBoard.cs` (_Process, HandleTileMoveAnimationFinished, CompressColumn, PullTile, RequestTileAnimate), `Core/GameObjects/Tile.cs` (MoveTile, PrepareForDrop), `Core/Gameplay/TileAnimationRequest.cs`.

### 2026-03-27: Self-Review of Animation Polish (all 8 checkpoints clean)
Reviewed all 5 animation polish changes across GameBoard.cs, Tile.cs, and TileAnimationRequest.cs. Findings:
1. **Batch dequeue (GameBoard _Process lines 109-198):** Confirmed clean. Empty currentRound + populated otherRound is handled by the else branch (line 189-193). Deferred requests are re-added correctly. No risk of lost requests.
2. **Event handler leak fix (Tile.cs lines 298-302, 353):** Confirmed clean. `_dropAnimationHandlerConnected` is intentionally NOT reset in `Recycle()` — the handler stays connected to the same `_dropAnimation` node across pool reuse cycles. Resetting would re-introduce the leak.
3. **Stagger delay (CompressColumn line 1056, PullTile line 1166):** Confirmed clean. CompressColumn range 0.03–0.19s, PullTile range 0.03–0.43s. No negative or excessive values possible. Column offset creates a left-to-right cascade; row offset in PullTile creates top-to-bottom cascade for fresh tiles. Note: CompressColumn's `(row - aboveRow)` is always 1, so the row term is a fixed 0.03s — distance-based DURATION in MoveTile handles the actual fall-speed scaling.
4. **Distance-based duration (Tile.cs lines 251-252):** Confirmed clean. Distance=0 yields minimum 0.15s (safe). Max board distance ~8 tiles yields 0.47s (under 0.55 cap). Fresh tiles from above can reach the cap, which is fine.
5. **TweenInterval placement (Tile.cs lines 257-261):** Confirmed clean. `TweenInterval` runs BEFORE `SetParallel(true)`, so stagger delay completes before movement/fade begin. Matches Godot 4 tween sequential-then-parallel semantics.
6. **Input guardrails (_movingTiles pre-population, lines 155-158):** Confirmed clean. All ready requests added to `_movingTiles` BEFORE the processing foreach loop. Synchronous completions (Static/PrepareForDrop) see correct total count. TryTake balances exactly: one add per request, one remove per HandleTileMoveAnimationFinished call.
7. **RequestTileAnimate signature (line 629):** Confirmed clean. Three call sites verified — Recycling (line 975) uses default 0f, CompressColumn (1057) and PullTile (1167) pass explicit stagger. No callers broken.
8. **Build:** 0 warnings, 0 errors.

### 2026-03-27: Stagger delay formula fixes for bottom→up cascade
- **CompressColumn (line 1056):** Changed `(row - aboveRow) * 0.03f` → `(startingRow - aboveRow) * 0.04f`. The old formula always yielded 1×0.03 because `aboveRow = row - 1`. Using `startingRow` (the hole row) gives increasing delay for tiles further from the gap — bottom tile drops first, upper tiles follow.
- **PullTile (line 1166):** Changed `(row + 1) * 0.03f` → `(Globals.TileCount - row) * 0.04f`. The old formula gave bottom rows (high index) larger delays, making top tiles animate first — backwards. Inverting via `TileCount - row` ensures bottom slots fill first.
- **Outcome:** Both changes produce a natural gravity cascade (bottom→up). Build clean, no new warnings.

### 2026-03-27T22:11: NullReferenceException fix in SwapSelectedTiles (pre-existing bug)
- **Root cause:** `GetTileCoordinates()` returns `null` when a tile is no longer present in `_gameBoard[,]`. The debug logging at line 431 dereferenced the result (`.Item1`) without a null check, causing the NRE. This is a **pre-existing bug**, not caused by the animation polish changes.
- **How it triggers:** If a selected tile is removed from the board between player clicks (e.g., via a special behavior or edge-case timing), `GetTileCoordinates` can't find it by reference scan. The caller (`Tile_OnTileMouseEvent`) already null-checked `GetTileCoordinates` in its own debug logging, but `SwapSelectedTiles` did not.
- **Fix applied:** (1) Added an early-return guard at the top of `SwapSelectedTiles` (lines 426–437) that validates both tiles are still on the board (valid Row/Column, reference-matches `_gameBoard` slot). If not, aborts the swap, re-enables input, and clears selection. (2) Replaced the `GetTileCoordinates` scan in debug logging with direct `primary.Row`/`primary.Column` access — cheaper and null-safe since the guard already validated coordinates.
- **Lesson:** `GetTileCoordinates()` is an O(n²) board scan that can return null. Prefer using tile's own `.Row`/`.Column` properties when the tile is known-valid, and always guard when using `GetTileCoordinates`.
- **Build:** 0 errors, 0 warnings.

### Audio overlap/exhaustion fix — sounds now fire once per round, not per tile
- **Root cause:** `Tile.MoveTile()` played the hype chime and a random drop sound on every individual tile animation. When the batch dequeue in `_Process()` dispatched 5–15 tiles in the same frame, they all hammered the same `AudioStreamPlayer` node (e.g. `Sound_MatchHypeLevel1`). Godot's default `max_polyphony=1` means each `.Play()` restarts/cuts off the previous. After rapid-fire restarts the player becomes unresponsive — explaining the "chime stops entirely" symptom.
- **Secondary bug (off-by-one):** The hype level cap used `Globals.MaxHypeLevel` (5), but the highest scene node is `Sound_MatchHypeLevel4`. Rounds 4+ tried to play `Sound_MatchHypeLevel5` which doesn't exist — `?.Play()` silently no-ops.
- **Fix applied:** (1) Removed all sound code from `Tile.MoveTile()`. (2) Added `_lastSoundRound` field to `GameBoard` (reset at all three `ProcessingRound = 0` sites). (3) In `_Process()`, after batch classification, plays hype chime once per round when first Animated batch starts (capped at `MaxHypeLevel - 1`). (4) Drop sound also fires once per round (~1/3 probability).
- **Lesson:** In a batched animation pipeline, sounds belong at the batch dispatch level (GameBoard._Process), not at the individual item level (Tile.MoveTile). One AudioStreamPlayer per sound means one `.Play()` per event.
- **Files changed:** `Scenes/GameBoard.cs`, `Core/GameObjects/Tile.cs`
- **Build:** 0 errors, 0 warnings (same 2 pre-existing warnings).
- **Session:** 2026-03-27T22:20 (Background spawn, Status: SUCCESS)
- **Decision documented:** `.squad/decisions.md` merged from inbox

### Race condition fix: null cell after cascading match
- **Root cause:** `CompressColumn` updated `_gameBoard[row, column]` immediately when shifting a tile down, but never called `higherTile.UpdateCoordinates(row, column)`. The tile's own `.Row`/`.Column` properties stayed at the pre-compression position. When a cascade round triggered `MatchDirectionEliminationBehavior`, the behavior read the stale `.Row`/`.Column` to call `NullifyTileAt()` — nullifying the **wrong cell**. The intended cell kept a recycling-marked tile; `DoRecycle()` in `EndTurn()` then recycled that tile in-place (hidden, UNKNOWN gem, IsAvailable=true) while `_gameBoard` still referenced it. Result: a phantom empty cell and potential duplicate pool references on subsequent Yoink.
- **Fix applied (3 changes):**
  1. Added `higherTile.UpdateCoordinates(row, column)` in `CompressColumn` right after the `_gameBoard` swap (line ~1115). Keeps tile.Row/Column in sync with the board as tiles cascade step-by-step.
  2. Changed `HandleMatches` recycling request to use `tile.Row`/`tile.Column` (from `MatchedTileInfo`, always board-correct) instead of `tile.TileRef.Row`/`tile.TileRef.Column` (potentially stale tile properties).
  3. Added drain loops for `_moveRequests` and `_movingTiles` in `EndTurn()` to prevent stale animation items from bleeding into future turns.
- **Why it manifested "after the first cascading match":** The stale coordinates only diverge when `CompressColumn` runs, which only happens when tiles are cleared. In a cascade, tiles cleared in round N are compressed, but their `.Row`/`.Column` aren't updated. If round N+1's match includes an elimination behavior reading those stale coordinates, the wrong cell is nullified. First cascade = first opportunity for the mismatch.
- **Files changed:** `Scenes/GameBoard.cs`
- **Build:** 0 errors, 0 warnings (same 2 pre-existing warnings).

## 2026-03-28T00:02: Race condition fix & time-based sound cooldown
**Session ID:** 2026-03-28T00-02-race-condition-fix

### Completed
- ✅ Fixed `CompressColumn` coordinate sync (line ~1115)
- ✅ Hardened `HandleMatches` recycling to use board-authoritative positions
- ✅ Added `_moveRequests`/`_movingTiles` drain in `EndTurn()`
- ✅ Implemented time-based chime cooldown (~2s spacing via `_lastSoundTimeMs`)
- ✅ Reset `_lastSoundTimeMs` at turn boundaries

**Outcome:** Build clean, 0 errors. Phantom empty cell bug eliminated. Chimes now properly spaced during cascades.

**Decisions merged:**
- 2026-03-28: Race condition fix — CompressColumn coordinate sync
- 2026-03-28: Time-based cooldown for match hype chimes (~2s spacing)

### DropAnimation scoped to matched positions only
- **Change:** DropAnimation (bounce/landing VFX) now only plays on tiles that land in positions vacated by matched tiles, not on every tile that completes an animated move.
- **Implementation:** Added `ShouldPlayDropAnimation` bool to `TileAnimationRequest` (default false). `HandleMatches` collects matched (row, col) into a `HashSet<(int, int)>`, passed through `CompressColumn` and `ReplaceRemovedTiles`/`PullTile`. The flag is set to `true` only when the tile's destination matches a matched position. `Tile.MoveTile()` gates `_dropAnimation.Play()` on this flag.
- **Files changed:** `TileAnimationRequest.cs`, `Tile.cs`, `GameBoard.cs`
- **Build:** 0 errors, 2 pre-existing warnings.

## 2026-03-28T01:34: DropAnimation scope finalization
**Session ID:** 2026-03-28T01-34-drop-anim-scope
**Agent:** Fry (background spawn)

✅ **Outcome:** SUCCESS

Completed scoping of DropAnimation (bounce/landing) to only play on tiles landing in matched positions. Previously fired on every tile completing an animated move (compression shifts, new pool pulls), creating visual noise.

**Implementation:**
- `TileAnimationRequest.cs`: Added `ShouldPlayDropAnimation` bool property (default false)
- `Tile.cs`: Gated `_dropAnimation.Play()` in `MoveTile()` on the flag
- `GameBoard.cs`: Built `HashSet<(int, int)>` of matched positions in `HandleMatches`, threaded through `CompressColumn`, `ReplaceRemovedTiles`, `PullTile`

**Decision documented:** `.squad/decisions.md` — "2026-03-28: DropAnimation scoped to matched tile positions"

### 2026-03-28T01:48: Hype sound fix & DropAnimation speed bump
**Session ID:** 2026-03-28T01-48-hype-sound-fix
**Agent:** Fry (background spawn, Opus 4.6)

- **Hype sounds broken by two bugs:** (1) The 2000ms time cooldown (`_lastSoundTimeMs`) blocked all hype sounds after the first — cascading rounds resolve in milliseconds, not seconds. Removed the time cooldown entirely; the round-based `_lastSoundRound` guard is sufficient. (2) The hype level formula `Math.Min(ProcessingRound + 1, MaxHypeLevel - 1)` skipped level 0 and capped instead of wrapping. Fixed to `ProcessingRound % MaxHypeLevel` — starts at level 0, wraps after level 4.
- **DropAnimation speed:** Bumped `speed_scale` from 2.0 to 2.5 in Tile.tscn. Now that the animation is scoped to matched positions only, the slightly faster speed gives a snappier feel without visual noise.
- **Lesson:** Time-based cooldowns on frame-driven sound events can silently kill the feature. Round-based guards are the right tool when rounds already serialize the events.
- **Files changed:** `Scenes/GameBoard.cs`, `GameObjectResources/Grid/Tile.tscn`
- **Build:** 0 errors, 2 pre-existing warnings.
- **Outcome:** SUCCESS — Cascading hype levels now play continuously; drop sounds resume firing; DropAnimation snappier.

### Chomp Tater animation polish — fade-in then move
- **Change:** Polished the ChompTater entrance animation in `MatchDirectionEliminationBehavior.InternalTrigger()`. Three fixes: (1) Start position moved one tile before the grid edge (`TileGridOffset - TileSize`) so she enters from offscreen — left for horizontal, above for vertical. (2) ChompTater now starts fully transparent (`Modulate alpha = 0`) and fades in over 0.3s before moving. (3) Tween order fixed — fade-in tween runs first (0.3s), then position tween (2.0s). Previously the sequential tweens were backwards (move first, fade after).
- **Lesson:** Godot 4's `SetParallel(false)` makes tweens sequential in declaration order. The old code had position first, opacity second — so the fade happened after the sprite had already traversed the board. Order matters.
- **Files changed:** `Core/Gameplay/TileBehaviors/MatchDirectionEliminationBehavior.cs`
- **Build:** 0 errors, 2 pre-existing warnings.

### Drop sound fix — guaranteed alternating playback per turn
- **Root cause:** Drop sounds (Drop1/Drop2) were gated behind a 1/3 random probability (`Globals.RNGesus.Next(0, 3) == 0`) and selected randomly between Drop1/Drop2. This meant ~67% of player turns had no drop sound at all, and when they did play, there was no alternation pattern. Combined with the chomp round-splitting (which defers Animated requests to a later round), the sound block was reached even less reliably for chomp matches.
- **Fix applied:** (1) Removed the 1/3 probability gate entirely — drop sounds now play every turn. (2) Replaced random Drop1/Drop2 selection with a deterministic alternator (`_nextDropIsOne` bool that flips each turn). (3) Added `_turnDropSoundPlayed` flag so the drop sound fires exactly once per player turn regardless of how many processing rounds occur during cascades. Flag resets at all three turn-boundary sites (SwapSelectedTiles ×2, Clear).
- **Lesson:** Per-round sound guards work for escalating hype chimes (where you want one per round), but drop sounds are a per-turn event. Separate the two with distinct flags. Random probability on audio cues makes them feel broken — if the design says "play every turn," use a deterministic trigger.
- **Files changed:** `Scenes/GameBoard.cs`
- **Build:** 0 errors, 2 pre-existing warnings.

## 2026-03-29T00:37: Drop sounds fix (deterministic alternation)
**Session ID:** 2026-03-29T00-37-drop-sounds
**Agent:** Fry (background spawn, Opus 4.6)

✅ **Outcome:** SUCCESS

Fixed drop sound playback regression caused by 1/3 random probability gate (~67% silent turns) combined with cascade round-splitting behavior. Replaced with deterministic per-turn trigger using `_nextDropIsOne` alternator and `_turnDropSoundPlayed` flag.

**Implementation:**
- `_nextDropIsOne`: Flips after each drop sound, alternating Drop1→Drop2→Drop1→...
- `_turnDropSoundPlayed`: Prevents multiple drops during cascade rounds, resets at turn boundaries (SwapSelectedTiles ×2, Clear)
- Drop sounds now fire exactly once per player turn, deterministically

**Decision documented:** `.squad/decisions.md` — "2026-07-22: Drop sounds play deterministically, once per turn, alternating"

**Orchestration logged:** `.squad/orchestration-log/2026-03-29T00-37-fry.md`

### Chomp Tater sound effects — three sounds wired to tween chain
- **Change:** Added three sound effects to the Chomp Tater animation: `chomptater-intro.mp3` (plays at fade-in start), `chomptater-moving.mp3` (plays at traversal start), `chomptater-out.mp3` (plays at fade-out start).
- **Implementation:** (1) Added three `AudioStreamPlayer` nodes (`Sound_ChompIntro`, `Sound_ChompMoving`, `Sound_ChompOut`) to `GameScene.tscn` under the Audio parent, following the existing pattern (volume -15dB, bus "sfx"). (2) In `GameBoard._Process()` ChompAnimation case, inserted `TweenCallback` calls before each tween phase to play the corresponding sound via `_gameScene.AudioNode.GetNode<AudioStreamPlayer>()`. The sequential tween chain guarantees correct timing: intro fires at t=0 (fade-in), moving at t=0.3s (traversal start), out at t=2.3s (fade-out start).
- **Lesson:** Godot's sequential tween callbacks execute at the precise boundary between phases — inserting a callback before a TweenProperty means it fires right as that phase begins. No manual timers needed.
- **Files changed:** `Scenes/GameScene.tscn`, `Scenes/GameBoard.cs`
- **Build:** 0 errors, 2 pre-existing warnings.

## 2026-03-31T15:07: Chomp Tater 0.5s pause after fade-in
**Session ID:** 2026-03-31T15-07-chomp-pause
**Agent:** Fry (background spawn, Opus 4.6)

✅ **Outcome:** SUCCESS

Added a 0.5-second pause after the Chomp Tater fade-in animation, before traversal begins. This gives the player a brief moment to see the Chomp Tater on-board before it starts eating across the row/column.

**Implementation:**
- `Scenes/GameBoard.cs`: Added `TweenInterval(0.5f)` in the tween chain between fade-in and traversal; updated `CascadeDelaySeconds` from 3.6s to 4.1s to account for the pause
- `Core/Gameplay/TileBehaviors/MatchDirectionEliminationBehavior.cs`: Repositioned `chomptater-moving.mp3` sound callback to fire after the pause (t=0.8s instead of t=0.3s)
- Per-tile recycling stagger in `HandleMatches` shifted by 0.5s to stay aligned with the new timeline

**Decision documented:** `.squad/decisions.md` — "2026-07-22: Chomp Tater 0.5s pause after fade-in, before traversal"

**Orchestration logged:** `.squad/orchestration-log/2026-03-31T15-07-fry.md`