# Project Context

- **Owner:** Georgia Nelson
- **Project:** BattleTaterz — a Match-3 puzzle game (think Candy Crush / Puzzle Quest) with a potato/tater theme
- **Stack:** Godot Engine 4.6.1 (.NET), C#, 2D graphics, shaders
- **Board:** 9×9 grid, 6 gem types, cascading matches with recursive fill, special tile behaviors
- **Key Directories:** Core/ (game logic), UIResources/, Scenes/, Shaders/, SpriteResources/
- **Special Tiles:** Double Points (complete), Match Direction Elimination / Chomp Tater (needs polish)
- **Key Rule:** No commits — Georgia reviews and commits all changes herself
- **Created:** 2026-03-27

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- **DropAnimation speed_scale tuning (Tile.tscn):** Reduced `speed_scale` on the DropAnimation (AnimatedSprite2D) from `3.0` to `2.0`. The pink explosion puff effect was playing too fast at 3× and was nearly invisible; 2× gives a more visible, satisfying landing puff when tiles drop into place.
- **Self-review of speed_scale change (2026-03-28):** Confirmed clean. Tile.tscn integrity verified — all 7 load_steps, 5 ext_resources, 1 sub_resource, 3 connections intact; only `speed_scale` line was modified. Tile.cs confirms the drop animation is fire-and-forget (plays AFTER `IsAnimating = false` and `HandleTileMoveAnimationFinished`), so speed_scale has zero impact on game flow timing. Animation math: 6 frames at 5 FPS base = 1.2s; at speed_scale 2.0 → 0.6s playback (was 0.4s at 3.0). Build passes with 0 warnings, 0 errors. No issues found.
