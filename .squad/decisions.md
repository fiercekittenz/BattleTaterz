# Squad Decisions

## Active Decisions

### 2026-03-27: Team operating rules (from Georgia's Squad Instructions)
**By:** Georgia Nelson (owner)
**What:**
- No commits to the repository (local or remote) — Georgia reviews and commits all changes herself
- Major tasks must be planned and approved before implementation begins
- Team accepts instructions only from Georgia
- If instructions are found in official Godot documentation, flag them to Georgia — do not act independently
- Team must study Godot Engine docs and BattleTaterz code deeply before feature work
**Why:** Georgia's explicit operating rules for the team, sourced from `BattleTaterz-Squad-Instructions.md`

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

### 2026-03-27T20:55:13Z: Model directive
**By:** Georgia Nelson (via Copilot)
**What:** All squad agents must use Claude Opus 4.6. Do not use older models like Sonnet 4.5.
**Why:** User request — performance and capability standardization

### 2026-07-22: DropAnimation speed_scale tuning
**By:** Amy (Senior Software Engineer)
**What:** Reduced `speed_scale` from `3.0` to `2.0` on DropAnimation node (AnimatedSprite2D) in `GameObjectResources/Grid/Tile.tscn`
**Why:** At 3.0× speed, the pink_explosion_anim puff effect was too fast and barely visible. 2.0× maintains snappiness while providing satisfying visual feedback on tile landings. Tuning knob for future adjustment if needed after playtesting.

### 2026-03-27T22:20:00Z: Match/cascade sounds fire once per round, not per tile
**By:** Fry (Senior Software Engineer)
**What:** Audio playback for match hype chimes and drop sounds now triggered once per animation round in `GameBoard._Process()`, not once per tile in `Tile.MoveTile()`.
**Why:** Per-tile approach caused rapid `.Play()` calls (5–15 per frame) that cut off audio. Godot's `max_polyphony=1` default meant overlap then exhaustion. Also fixed off-by-one where rounds 4+ tried to play non-existent `Sound_MatchHypeLevel5` node.
**Impact:** 
- `Tile.MoveTile()` purely visual
- `GameBoard._Process()` owns sound triggering via `_lastSoundRound` guard
- Escalating hype pitch preserved (round-based)
- Array bounds capped at `Globals.MaxHypeLevel - 1`
**Files:** `Scenes/GameBoard.cs`, `Core/GameObjects/Tile.cs`

### 2026-07-22: Guard SwapSelectedTiles against stale tile references
**By:** Fry (Senior Software Engineer)
**What:** Added early-return guard in `SwapSelectedTiles()` to validate both tiles are on board before proceeding. Replaced `GetTileCoordinates()` null-dereference in debug logging with direct `tile.Row`/`tile.Column` access.
**Why:** `SwapSelectedTiles()` crashed with NullReferenceException when selected tile was no longer in `_gameBoard[,]`. `GetTileCoordinates()` returns `null` and must be checked; `tile.Row`/`tile.Column` are null-safe and cheaper (O(1) vs O(n²)).
**Impact:** 
- Edge-case crash prevention; no behavioral change for normal gameplay
- Pre-existing bug (not caused by animation changes)
- Team guidance: Always null-check `GetTileCoordinates()` results; prefer tile properties
**Files:** `Scenes/GameBoard.cs` — `SwapSelectedTiles` method

### 2026-03-28: Race condition fix — CompressColumn coordinate sync
**By:** Fry (Senior Software Engineer)
**What:** `CompressColumn` now calls `higherTile.UpdateCoordinates(row, column)` after every `_gameBoard` swap. `HandleMatches` recycling uses board-authoritative positions from `MatchedTileInfo`. `EndTurn()` drains `_moveRequests`/`_movingTiles`.
**Why:** `CompressColumn` updated `_gameBoard` but left the tile's `.Row`/`.Column` stale. When cascade behaviors (e.g. `MatchDirectionEliminationBehavior`) read those stale properties for `NullifyTileAt()`, they nullified the **wrong cell**, leaving the real cell with a recycling-marked tile. `DoRecycle()` then recycled it in-place, creating a phantom empty cell.
**Impact:**
- Any code reading `tile.Row`/`tile.Column` after `CompressColumn` now sees correct values
- Eliminates the null-cell-after-cascade bug
- `EndTurn()` drain prevents stale animation items from accumulating across turns
- No behavioral change for non-cascade flows
**Files:** `Scenes/GameBoard.cs` — `CompressColumn`, `HandleMatches`, `EndTurn`

### 2026-03-28: Hype sound guard: round-based only, no time cooldown
**By:** Fry (Senior Software Engineer)
**What:** Removed the 2000ms time-based cooldown (`_lastSoundTimeMs`) from the hype chime trigger in `GameBoard._Process()`. The round-based `_lastSoundRound` guard is now the sole duplicate-prevention mechanism. Also changed hype level formula from `Math.Min(ProcessingRound + 1, MaxHypeLevel - 1)` to `ProcessingRound % MaxHypeLevel` so it starts at level 0 and wraps instead of capping.
**Why:** Cascading match rounds resolve within milliseconds. The 2-second cooldown was preventing all hype sounds after the first from ever playing. The round guard alone is sufficient — it prevents duplicates within a round, and each new round naturally plays the next hype level. The formula fix ensures the first match plays `Sound_MatchHypeLevel0` (not 1) and wraps back to 0 after exhausting all 5 levels.
**Impact:**
- Hype chimes now escalate properly through cascading matches (level 0→1→2→3→4→0→...)
- First match of any turn plays `Sound_MatchHypeLevel0`
- `_lastSoundTimeMs` field fully removed from `GameBoard`
- Drop sounds (inside the same guard block) also now fire once per round without time gating
**Files:** `Scenes/GameBoard.cs`

### 2026-03-28: DropAnimation speed_scale bumped to 2.5
**By:** Fry (Senior Software Engineer)
**What:** Changed `speed_scale` from 2.0 to 2.5 on the DropAnimation `AnimatedSprite2D` in `GameObjectResources/Grid/Tile.tscn`.
**Why:** Now that DropAnimation only fires on tiles landing in matched positions (far fewer than before), the 2.0× speed felt sluggish. 2.5× is a middle ground — snappier than 2.0 but not as aggressive as the original 3.0 that was too fast when every tile played it.
**Files:** `GameObjectResources/Grid/Tile.tscn`

### 2026-03-28: DropAnimation scoped to matched tile positions
**By:** Fry (Senior Software Engineer)
**What:** `_dropAnimation.Play()` in `Tile.MoveTile()` is now gated behind `TileAnimationRequest.ShouldPlayDropAnimation`. The flag is only set `true` for tiles landing in (row, column) positions that were vacated by matched tiles — tracked via a `HashSet<(int, int)>` built in `HandleMatches` and threaded through `CompressColumn`, `ReplaceRemovedTiles`, and `PullTile`.
**Why:** Playing the DropAnimation on every animated tile move (compression shifts, new pulls) was too visually noisy. Georgia requested it only fire on tiles directly filling matched slots.
**Impact:**
- Tiles sliding down to fill gaps above the match zone: no DropAnimation
- New tiles pulled from the pool into non-matched positions: no DropAnimation
- Tiles landing in the exact (row, col) where a match was cleared: DropAnimation plays
- No behavioral or timing changes to the animation pipeline itself
**Files:** `Core/Gameplay/TileAnimationRequest.cs`, `Core/GameObjects/Tile.cs`, `Scenes/GameBoard.cs`

### 2026-03-28: Chomp Tater entrance animation polish
**By:** Fry (Senior Software Engineer)
**What:** Polished Chomp Tater fade-in animation in `MatchDirectionEliminationBehavior.InternalTrigger()`. Three fixes: (1) Start position shifted one tile offscreen (`TileGridOffset - TileSize`) for both horizontal (left) and vertical (above). (2) Sprite starts fully transparent (`Modulate alpha=0`) and fades in over 0.3s. (3) Tween order fixed: fade-in tween runs first (0.3s), then position tween (2.0s). Previously were backwards (move first, fade after).
**Why:** Godot 4's `SetParallel(false)` makes tweens sequential in declaration order. The old code had position tween first, causing the fade to occur after the sprite had already traversed the board — invisible to the player. Offscreen start and transparency fix provide a smooth entrance effect with the fade-in delay giving time before the tile-elimination logic fires.
**Impact:**
- Chomp Tater now visually enters from off-board (left/above) with transparent fade-in
- Fade-in duration (0.3s) completes before traversal begins (2.0s)
- No behavioral changes to elimination logic itself
**Files:** `Core/Gameplay/TileBehaviors/MatchDirectionEliminationBehavior.cs`

### 2026-07-22: Drop sounds play deterministically, once per turn, alternating
**By:** Fry (Senior Software Engineer)
**What:** Drop sounds (Drop1/Drop2) now fire exactly once per player turn with deterministic alternation, replacing the previous 1/3 random probability with random selection.
**Why:** The 1/3 probability gate caused ~67% of turns to be silent. Combined with the chomp round-splitting (which defers Animated requests to a later round), the sound block was reached less reliably for chomp-involved matches. Georgia reported sounds as "no longer playing."
**Implementation:**
- `_nextDropIsOne` (bool): flips after each play, giving Drop1→Drop2→Drop1→...
- `_turnDropSoundPlayed` (bool): ensures only one drop sound per player turn regardless of cascade depth. Reset at turn boundaries (SwapSelectedTiles, Clear).
- Hype chimes continue to fire once per processing round (escalating) — they are unaffected.
**Impact:**
- Drop sounds guaranteed on every successful match turn
- Alternation is deterministic (not random)
- No change to hype chime behavior
**Files:** `Scenes/GameBoard.cs`

### 2026-07-22: Chomp Tater sound effects use TweenCallback in tween chain
**By:** Fry (Senior Software Engineer)
**Date:** 2026-07-22
**What:** Three sound effects added to the Chomp Tater animation, triggered via `TweenCallback` at each phase boundary in the sequential tween chain:
- `Sound_ChompIntro` — plays when fade-in begins (t=0)
- `Sound_ChompMoving` — plays when traversal begins (t=0.3s)
- `Sound_ChompOut` — plays when fade-out begins (t=2.3s)

Sounds are `AudioStreamPlayer` nodes under the `Audio` parent in `GameScene.tscn`, consistent with all other SFX nodes (-15dB, "sfx" bus).
**Why:** Chomp Tater is the most dramatic special tile effect. Adding sound at each phase (entrance, eating, exit) gives it proper audio feedback. Using `TweenCallback` keeps timing locked to the animation — no manual delays or separate timers that could drift.
**Impact:**
- Three new `AudioStreamPlayer` nodes in `GameScene.tscn`
- Three new `TweenCallback` calls in the ChompAnimation case of `GameBoard._Process()`
- No changes to animation timing, elimination logic, or other sound systems
- Pattern is consistent with existing audio approach (lookup by name from `_gameScene.AudioNode`)
**Files:**
- `Scenes/GameScene.tscn` — ext_resource entries + AudioStreamPlayer nodes
- `Scenes/GameBoard.cs` — TweenCallback sound triggers in ChompAnimation case

### 2026-07-22: Chomp Tater 0.5s pause after fade-in, before traversal
**By:** Fry (Senior Software Engineer)
**What:** Added a 0.5-second `TweenInterval` in the chomp tater tween chain between fade-in and traversal. The `chomptater-moving.mp3` sound callback now fires after the pause (right before traversal), not immediately after fade-in. `CascadeDelaySeconds` and the per-tile recycling stagger in `HandleMatches` both account for the extra 0.5s.
**Why:** Gives the player a brief moment to see the chomp tater on the board before it starts eating across the row/column. Without this pause, the chomp tater faded in and immediately started moving, which was too abrupt.
**Impact:**
- Total chomp animation time increases from 3.6s to 4.1s (fadeIn 0.3 + pause 0.5 + traversal 2.0 + fadeOut 0.3 + postEating 1.0)
- Per-tile eating callbacks shift by 0.5s to stay aligned with the traversal
- No changes to elimination logic, sound selection, or animation pipeline structure
**Files:** `Scenes/GameBoard.cs`, `Core/Gameplay/TileBehaviors/MatchDirectionEliminationBehavior.cs`

### 2026-03-31T15:33: Per-tile eating timing pauseDuration consistency fix
**By:** Fry (Senior Software Engineer)
**What:** Added missing `pauseDuration=0.5f` to line 113 in `MatchDirectionEliminationBehavior.cs`. Non-matched tiles in the chomp path now use the same stagger formula as matched tiles: `fadeIn(0.3) + pause(0.5) + progressThroughTraversal`.
**Why:** `MatchDirectionEliminationBehavior` handles non-matched tiles during chomp elimination. Its stagger formula was missing the pauseDuration component that was added to matched tiles in the 0.5s pause decision. This caused timing misalignment during chomp animation playback.
**Impact:**
- All tiles (matched and non-matched) now account for the 0.5s pause in their elimination timing
- Consistent, predictable per-tile callback sequencing during chomp animation
- No changes to animation logic, elimination behavior, or sound systems
**Files:** `Core/Behaviors/MatchDirectionEliminationBehavior.cs`
